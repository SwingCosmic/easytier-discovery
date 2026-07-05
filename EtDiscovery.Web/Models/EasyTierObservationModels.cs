using System.Text.Json.Serialization;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Web.Models;

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

    public IReadOnlyList<NodeRole> Roles { get; init; } = [NodeRole.Worker];
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

public sealed record ForeignPeerMembership(uint PeerId, string NetworkName);
