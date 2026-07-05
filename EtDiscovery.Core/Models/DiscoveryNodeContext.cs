namespace EtDiscovery.Core.Models;

/// <summary>
/// Identifies the local node, its role set and resolved discovery capabilities at runtime.
/// </summary>
public sealed class DiscoveryNodeContext
{
    public DiscoveryNodeContext(string localNodeId, IEnumerable<NodeRole> localRoles)
    {
        LocalNodeId = localNodeId;
        LocalRoles = localRoles
            .Distinct()
            .ToArray();
        Capabilities = DiscoveryNodeCapabilities.ForRoles(LocalRoles);
    }

    public string LocalNodeId { get; }

    /// <summary>
    /// One node may carry multiple logical roles at the same time.
    /// </summary>
    public IReadOnlyList<NodeRole> LocalRoles { get; }

    public DiscoveryNodeCapabilities Capabilities { get; set; }
}
