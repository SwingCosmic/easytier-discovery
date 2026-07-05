namespace EtDiscovery.Core.Models;

/// <summary>
/// Simplified lifecycle state for a service instance.
/// </summary>
public enum HealthState
{
    Registering = 0,
    Healthy = 1,
    Degraded = 2,
    Suspect = 3,
    Unreachable = 4,
    Draining = 5,
    Dead = 6,
}
