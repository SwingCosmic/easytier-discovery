namespace EtDiscovery.Core.Models;

/// <summary>
/// Reachability-oriented node state consumed by the current prototype policies.
/// </summary>
public sealed class NodeSnapshot
{
    public NodeSnapshot(string nodeId, bool reachable)
    {
        NodeId = nodeId;
        Reachable = reachable;
    }

    public string NodeId { get; }

    /// <summary>
    /// Current simplified reachability bit used by the prototype.
    /// </summary>
    public bool Reachable { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Roles currently associated with the node.
    /// </summary>
    public IReadOnlyList<NodeRole> Roles { get; set; } = [NodeRole.Worker];

    public string? VirtualIp { get; set; }

    public NetworkType NetworkType { get; set; } = NetworkType.Unknown;

    public string? NatType { get; set; }

    public IReadOnlyDictionary<string, string> Features { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<string> TopologyTags { get; set; } = [];

    public int? ResourceScore { get; set; }

    public int? StabilityScore { get; set; }
}
