namespace EtDiscovery.Core.Models;

/// <summary>
/// Represents the recommended transport style for a selected endpoint.
/// </summary>
public enum CallMode
{
    Unknown = 0,
    Direct = 1,
    Relay = 2,
    Proxy = 3,
}
