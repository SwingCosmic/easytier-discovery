using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Picks one candidate instance from an already filtered candidate set.
/// </summary>
public interface IServiceSelectionPolicy
{
    ServiceInstance? Select(ServiceKey serviceKey, IReadOnlyList<ServiceInstance> candidates);
}
