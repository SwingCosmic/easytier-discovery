using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Public async query surface for resolving and selecting service instances.
/// </summary>
public interface IDiscoveryResolver
{
    Task<IReadOnlyList<ServiceInstance>> ResolveAsync(ServiceQuery query, CancellationToken cancellationToken = default);

    Task<SelectedInstance?> SelectOneHealthyInstanceAsync(ServiceQuery query, CallContext? callContext = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectedInstance>> SelectManyHealthyInstancesAsync(ServiceQuery query, CallContext? callContext = null, int limit = 10, CancellationToken cancellationToken = default);

    IAsyncEnumerable<InstanceWatchEvent> WatchAsync(ServiceQuery query, CancellationToken cancellationToken = default);

    Task<NodeProfile?> GetNodeProfileAsync(string nodeId, CancellationToken cancellationToken = default);

    Task<CallModeRecommendation?> RecommendCallModeAsync(string instanceId, CallContext? callContext = null, CancellationToken cancellationToken = default);
}
