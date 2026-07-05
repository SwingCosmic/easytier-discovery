using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Long-running runtime wrapper that owns background refresh and role-aware capabilities.
/// </summary>
public interface IDiscoveryRuntime : IDiscoveryResolver, IServiceRegistryClient, ICallFeedbackSink
{
    DiscoveryNodeContext Context { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task RefreshOnceAsync(CancellationToken cancellationToken = default);
}
