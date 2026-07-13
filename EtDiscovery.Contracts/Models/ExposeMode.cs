namespace EtDiscovery.Core.Models;

/// <summary>
/// Declares how an instance expects to be exposed to callers.
/// </summary>
public enum ExposeMode
{
    Direct = 0,
    RelayPreferred = 1,
    RelayOnly = 2,
    ProxyPreferred = 3,
}
