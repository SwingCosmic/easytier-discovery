namespace EtDiscovery.Core.Models;

/// <summary>
/// In-memory snapshot used by the policy layer to rebuild discovery state.
/// </summary>
public sealed class DiscoverySnapshot
{
    public DiscoverySnapshot(
        IReadOnlyList<NodeSnapshot>? nodes = null,
        IReadOnlyList<ServiceInstance>? instances = null,
        IReadOnlyList<NodeProfile>? nodeProfiles = null,
        IReadOnlyList<LinkProfile>? linkProfiles = null)
    {
        Nodes = nodes ?? [];
        Instances = instances ?? [];
        NodeProfiles = nodeProfiles ?? [];
        LinkProfiles = linkProfiles ?? [];
    }

    /// <summary>
    /// Raw node reachability view.
    /// </summary>
    public IReadOnlyList<NodeSnapshot> Nodes { get; set; }

    /// <summary>
    /// Raw service instances visible to discovery.
    /// </summary>
    public IReadOnlyList<ServiceInstance> Instances { get; set; }

    public IReadOnlyList<NodeProfile> NodeProfiles { get; set; }

    public IReadOnlyList<LinkProfile> LinkProfiles { get; set; }

    public static DiscoverySnapshot Empty { get; } = new();
}
