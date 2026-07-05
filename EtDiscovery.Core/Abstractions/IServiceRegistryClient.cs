using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Async API surface for service publish and lifecycle operations.
/// </summary>
public interface IServiceRegistryClient
{
    Task RegisterServiceAsync(RegisterServiceRequest request, CancellationToken cancellationToken = default);

    Task RenewAsync(string instanceId, long? leaseEpoch = null, CancellationToken cancellationToken = default);

    Task DeregisterAsync(string instanceId, CancellationToken cancellationToken = default);

    Task SetDrainingAsync(string instanceId, CancellationToken cancellationToken = default);
}
