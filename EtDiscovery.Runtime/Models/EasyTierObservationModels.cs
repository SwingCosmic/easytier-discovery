using System.Text.Json.Serialization;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Runtime.Models;

public sealed class EasyTierObservationSnapshot
{
    public required DateTimeOffset ObservedAt { get; init; }

    public required LocalNodeView LocalNode { get; init; }

    public required IReadOnlyList<ObservedPeer> Peers { get; init; }
}

public sealed class LocalNodeView
{
    public required string NodeId { get; init; }

    public required string NetworkName { get; init; }

    public string? VirtualIp { get; init; }

    public string? Hostname { get; init; }

    public uint? PeerId { get; init; }

    public uint? NodeTypeAppId { get; init; }

    public uint NodeTypeFlags { get; init; }

    public IReadOnlyList<NodeRole> Roles { get; init; } = [NodeRole.Worker];
}

public sealed class ObservedPeer
{
    public required string NodeId { get; init; }

    public required string NetworkName { get; init; }

    public string? VirtualIp { get; init; }

    public string? Hostname { get; init; }

    public uint? PeerId { get; init; }

    public bool IsLocal { get; init; }

    public bool Reachable { get; init; }

    public bool SameNetwork { get; init; }

    public bool InVirtualNetworkCidr { get; init; }

    public bool EligibleForDiscovery { get; init; }

    public string? ForeignNetworkName { get; init; }

    public string? Cost { get; init; }

    public uint? NodeTypeAppId { get; init; }

    public uint NodeTypeFlags { get; init; }

    public IReadOnlyList<NodeRole> Roles { get; init; } = [NodeRole.Worker];

    public bool IsRegistryCandidate =>
        EtDiscoveryNodeTypeFlags.IsRegistryCandidate(NodeTypeAppId, NodeTypeFlags);
}

public sealed class EasyTierPeerListItem
{
    [JsonPropertyName("cidr")]
    public string? Cidr { get; init; }

    [JsonPropertyName("ipv4")]
    public string? Ipv4 { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("cost")]
    public string? Cost { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public sealed class EasyTierNodeInfo
{
    [JsonPropertyName("peer_id")]
    public uint PeerId { get; init; }

    [JsonPropertyName("ipv4_addr")]
    public string? Ipv4Addr { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("config")]
    public string? Config { get; init; }

    [JsonPropertyName("node_type_flags")]
    public uint? NodeTypeFlags { get; init; }

    [JsonPropertyName("node_type_app_id")]
    public uint? NodeTypeAppId { get; init; }
}

/// <summary>
/// Subset of EasyTier verbose <c>peer list -v</c> / route payload needed for role discovery.
/// Intentionally omits nested address objects (e.g. ipv4_addr.address.addr as u32) that we
/// do not need; VIP still comes from non-verbose peer list projection.
/// </summary>
public sealed class EasyTierPeerRoutePair
{
    [JsonPropertyName("route")]
    public EasyTierRouteInfo? Route { get; init; }
}

public sealed class EasyTierRouteInfo
{
    [JsonPropertyName("peer_id")]
    public uint PeerId { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("node_type_flags")]
    public uint? NodeTypeFlags { get; init; }

    [JsonPropertyName("node_type_app_id")]
    public uint? NodeTypeAppId { get; init; }
}

public sealed class ForeignNetworkEntry
{
    [JsonPropertyName("peers")]
    public IReadOnlyList<ForeignNetworkPeer> Peers { get; init; } = [];
}

public sealed class ForeignNetworkPeer
{
    [JsonPropertyName("peer_id")]
    public uint PeerId { get; init; }
}

public sealed class RegistryEndpoint
{
    public required string Address { get; init; }

    public required string Source { get; init; }

    public string? NodeId { get; init; }

    public string? VirtualIp { get; init; }
}

public sealed class RegistryMetadataResponse
{
    public string Protocol { get; init; } = "etdiscovery";

    public string ProtocolVersion { get; init; } = "0.1";

    public required string NetworkName { get; init; }

    public required string NodeId { get; init; }

    public string? VirtualIp { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }

    public required RegistryEndpoints Endpoints { get; init; }

    public required RegistryCapabilities Capabilities { get; init; }
}

public sealed class RegistryEndpoints
{
    public required string Http { get; init; }
}

public sealed class RegistryCapabilities
{
    public bool ServiceRegistration { get; init; } = true;

    public bool ServiceResolve { get; init; } = true;
}

public sealed record ForeignPeerMembership(uint PeerId, string NetworkName);
