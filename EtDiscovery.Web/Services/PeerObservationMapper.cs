using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class PeerObservationMapper
{
    public EasyTierObservationSnapshot Map(
        EtDiscoveryWebOptions options,
        EasyTierNodeInfo nodeInfo,
        IReadOnlyList<EasyTierPeerListItem> peers,
        IReadOnlyDictionary<string, ForeignNetworkEntry> foreignNetworks)
    {
        var foreignMembership = BuildForeignMembership(foreignNetworks);
        var localVirtualIp = TrimCidr(nodeInfo.Ipv4Addr) ?? options.ConfiguredVirtualIp;
        var localNodeId = BuildNodeId(nodeInfo.PeerId, nodeInfo.Hostname, localVirtualIp);

        var observedPeers = peers
            .Select(peer => MapPeer(options, peer, foreignMembership, localNodeId, localVirtualIp))
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
        string localNodeId,
        string? localVirtualIp)
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
        var roles = isLocal
            ? options.Roles.Select(RoleNameMapper.ToNodeRole).ToArray()
            : new[] { EtDiscovery.Core.Models.NodeRole.Worker };

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
            Roles = roles,
        };
    }

    private static string? TrimCidr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
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
}
