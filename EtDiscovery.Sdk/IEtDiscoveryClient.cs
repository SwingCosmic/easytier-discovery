using EtDiscovery.Core.Models;

namespace EtDiscovery.Sdk;

/// <summary>
/// Business-facing client for the local EtDiscovery runtime (/runtime/v1).
/// </summary>
public interface IEtDiscoveryClient
{
    /// <summary>Select one healthy instance for a service (consumer).</summary>
    Task<SelectedInstance?> SelectOneAsync(
        string serviceName,
        string? protocol = null,
        CancellationToken cancellationToken = default);

    /// <summary>List candidate instances for a service (consumer).</summary>
    Task<IReadOnlyList<ServiceInstance>> ResolveAsync(
        string serviceName,
        string? protocol = null,
        CancellationToken cancellationToken = default);

    /// <summary>Register this process using configured ServiceName/Port (provider).</summary>
    Task RegisterAsync(CancellationToken cancellationToken = default);

    /// <summary>Send one heartbeat for the registered instance (provider).</summary>
    Task HeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>Deregister this process (provider).</summary>
    Task DeregisterAsync(CancellationToken cancellationToken = default);

    /// <summary>Instance id after a successful register, if any.</summary>
    string? RegisteredInstanceId { get; }
}
