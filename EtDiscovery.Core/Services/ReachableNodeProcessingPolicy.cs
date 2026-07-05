using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Services;

public sealed class ReachableNodeProcessingPolicy : INodeProcessingPolicy
{
    public ServiceCatalog BuildCatalog(DiscoverySnapshot snapshot)
    {
        var reachableNodes = snapshot.Nodes
            .Where(node => node.Reachable)
            .Select(node => node.NodeId)
            .ToHashSet(StringComparer.Ordinal);

        var services = snapshot.Instances
            .Where(instance =>
                instance.Status == ServiceInstanceStatus.Active &&
                reachableNodes.Contains(instance.NodeId))
            .GroupBy(instance => instance.ServiceKey)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ServiceInstance>)group
                    .OrderBy(instance => instance.InstanceId, StringComparer.Ordinal)
                    .ToArray());

        var nodeProfiles = (snapshot.NodeProfiles ?? [])
            .ToDictionary(profile => profile.NodeId, StringComparer.Ordinal);

        var linkProfiles = (snapshot.LinkProfiles ?? [])
            .ToDictionary(profile => profile.LinkId, StringComparer.Ordinal);

        return new ServiceCatalog(services, nodeProfiles, linkProfiles);
    }
}
