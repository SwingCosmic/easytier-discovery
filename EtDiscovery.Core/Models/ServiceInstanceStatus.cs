namespace EtDiscovery.Core.Models;

/// <summary>
/// External availability marker stored on a service instance.
/// </summary>
public enum ServiceInstanceStatus
{
    Active = 0,
    Draining = 1,
    Offline = 2,
}
