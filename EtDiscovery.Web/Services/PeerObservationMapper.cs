using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class PeerObservationMapper
{
    public EasyTierObservationSnapshot Map(
        EtDiscoveryWebOptions options,
        EasyTierNodeInfo nodeInfo,
        IReadOnlyList<EasyTierPeerListItem> peers,
        IReadOnlyList<EasyTierPeerRoutePair> routePairs,
        IReadOnlyDictionary<string, ForeignNetworkEntry> foreignNetworks)
    {
        var foreignMembership = BuildForeignMembership(foreignNetworks);
        var routeMetadataByPeerId = BuildRouteMetadataLookup(routePairs);
        var localVirtualIp = TrimCidr(nodeInfo.Ipv4Addr) ?? options.ConfiguredVirtualIp;
        var localNodeId = BuildNodeId(nodeInfo.PeerId, nodeInfo.Hostname, localVirtualIp);
        var localAppId = nodeInfo.NodeTypeAppId ?? EtDiscoveryNodeTypeFlags.AppId;
        var localFlags = nodeInfo.NodeTypeFlags ?? options.GetAdvertisedNodeTypeMetadata().Flags;
        var localRoles = options.Roles.Select(RoleNameMapper.ToNodeRole).ToArray();

        var observedPeers = peers
            .Select(peer => MapPeer(
                options,
                peer,
                foreignMembership,
                routeMetadataByPeerId,
                localNodeId,
                localVirtualIp,
                localAppId,
                localFlags,
                localRoles))
            .ToArray();

        return new EasyTierObservationSnapshot
        {
            ObservedAt = DateTimeOffset.UtcNow,
            LocalNode = new LocalNodeView
            {
                NodeId = localNodeId,
                Hostname = nodeInfo.Hostname,
                NetworkName = options.NetworkName,
                PeerId = nodeInfo.PeerId,
                VirtualIp = localVirtualIp,
                NodeTypeAppId = localAppId,
                NodeTypeFlags = localFlags,
                Roles = localRoles,
            },
            Peers = observedPeers,
        };
    }

    public static string BuildNodeId(uint? peerId, string? hostname, string? virtualIp)
    {
        if (peerId is not null)
        {
            return $"peer:{peerId.Value}";
        }

        if (!string.IsNullOrWhiteSpace(hostname))
        {
            return $"host:{hostname}";
        }

        return $"ip:{virtualIp}";
    }

    private ObservedPeer MapPeer(
        EtDiscoveryWebOptions options,
        EasyTierPeerListItem peer,
        IReadOnlyDictionary<uint, string> foreignMembership,
        IReadOnlyDictionary<uint, RouteMetadata> routeMetadataByPeerId,
        string localNodeId,
        string? localVirtualIp,
        uint localAppId,
        uint localFlags,
        IReadOnlyList<NodeRole> localRoles)
    {
        uint? peerId = uint.TryParse(peer.Id, out var parsedPeerId) ? parsedPeerId : null;
        var foreignNetworkName = peerId is not null && foreignMembership.TryGetValue(peerId.Value, out var networkName)
            ? networkName
            : null;
        var isLocal = string.Equals(peer.Cost, "Local", StringComparison.OrdinalIgnoreCase);
        var virtualIp = isLocal
            ? TrimCidr(peer.Cidr) ?? TrimCidr(peer.Ipv4) ?? localVirtualIp
            : TrimCidr(peer.Cidr) ?? TrimCidr(peer.Ipv4);
        var sameNetwork = isLocal || foreignNetworkName is null;
        var inVirtualNetworkCidr = options.VirtualNetworkCidr.Contains(virtualIp);

        uint? appId;
        uint flags;
        IReadOnlyList<NodeRole> roles;
        if (isLocal)
        {
            appId = localAppId;
            flags = localFlags;
            roles = localRoles;
        }
        else if (peerId is not null && routeMetadataByPeerId.TryGetValue(peerId.Value, out var metadata))
        {
            appId = metadata.AppId;
            flags = metadata.Flags;
            roles = EtDiscoveryNodeTypeFlags.DecodeRoles(appId, flags);
        }
        else
        {
            appId = null;
            flags = 0;
            roles = EtDiscoveryNodeTypeFlags.DecodeRoles(null, 0);
        }

        return new ObservedPeer
        {
            NodeId = isLocal ? localNodeId : BuildNodeId(peerId, peer.Hostname, virtualIp),
            NetworkName = sameNetwork ? options.NetworkName : foreignNetworkName ?? options.NetworkName,
            VirtualIp = virtualIp,
            Hostname = peer.Hostname,
            PeerId = peerId,
            IsLocal = isLocal,
            Reachable = true,
            SameNetwork = sameNetwork,
            InVirtualNetworkCidr = inVirtualNetworkCidr,
            EligibleForDiscovery = sameNetwork && inVirtualNetworkCidr,
            ForeignNetworkName = foreignNetworkName,
            Cost = peer.Cost,
            NodeTypeAppId = appId,
            NodeTypeFlags = flags,
            Roles = roles,
        };
    }

    private static string? TrimCidr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var slash = value.IndexOf('/');
        return slash >= 0 ? value[..slash] : value;
    }

    private static IReadOnlyDictionary<uint, string> BuildForeignMembership(IReadOnlyDictionary<string, ForeignNetworkEntry> foreignNetworks)
    {
        var result = new Dictionary<uint, string>();
        foreach (var (networkName, entry) in foreignNetworks)
        {
            foreach (var peer in entry.Peers)
            {
                result[peer.PeerId] = networkName;
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<uint, RouteMetadata> BuildRouteMetadataLookup(
        IReadOnlyList<EasyTierPeerRoutePair> routePairs)
    {
        var result = new Dictionary<uint, RouteMetadata>();
        foreach (var pair in routePairs)
        {
            var route = pair.Route;
            if (route is null || route.PeerId == 0)
            {
                continue;
            }

            result[route.PeerId] = new RouteMetadata(route.NodeTypeAppId, route.NodeTypeFlags ?? 0);
        }

        return result;
    }

    private sealed record RouteMetadata(uint? AppId, uint Flags);
}
