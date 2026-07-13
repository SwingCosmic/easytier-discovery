namespace EtDiscovery.Core.Models;

/// <summary>
/// Stable or semi-stable metadata about one node.
/// </summary>
public sealed class NodeProfile
{
    public NodeProfile(string nodeId)
    {
        NodeId = nodeId;
    }

    public string NodeId { get; }

    /// <summary>
    /// One node may contribute multiple logical roles.
    /// </summary>
    public IReadOnlyList<NodeRole> Roles { get; set; } = [NodeRole.Worker];

    public string? NetworkType { get; set; }

    public string? NatType { get; set; }

    public string? VirtualIp { get; set; }

    public IReadOnlyDictionary<string, string> FeatureFlags { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<string> TopologyTags { get; set; } = [];

    public int? ResourceScore { get; set; }

    public int? StabilityScore { get; set; }
}
