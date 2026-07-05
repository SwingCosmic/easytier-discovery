using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Services;

public sealed class RoundRobinServiceSelectionPolicy : IServiceSelectionPolicy
{
    private readonly object _sync = new();
    private readonly Dictionary<ServiceKey, int> _nextIndexes = [];

    public ServiceInstance? Select(ServiceKey serviceKey, IReadOnlyList<ServiceInstance> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        lock (_sync)
        {
            var nextIndex = _nextIndexes.GetValueOrDefault(serviceKey, 0);
            var selectedIndex = nextIndex % candidates.Count;
            _nextIndexes[serviceKey] = (selectedIndex + 1) % candidates.Count;
            return candidates[selectedIndex];
        }
    }
}
