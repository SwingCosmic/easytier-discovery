using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;
using System.Collections.Concurrent;

namespace EtDiscovery.Core.Services;

public sealed class RoundRobinServiceSelectionPolicy : IServiceSelectionPolicy
{
    private readonly ConcurrentDictionary<ServiceKey, int> _nextIndexes = [];

    public ServiceInstance? Select(ServiceKey serviceKey, IReadOnlyList<ServiceInstance> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var cursor = _nextIndexes.AddOrUpdate(
            serviceKey,
            _ => 1,
            (_, current) => current == int.MaxValue ? 1 : current + 1);
        var selectedIndex = (cursor - 1) % candidates.Count;
        return candidates[selectedIndex];
    }
}
