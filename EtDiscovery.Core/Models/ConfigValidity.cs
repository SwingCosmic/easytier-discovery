namespace EtDiscovery.Core.Models;

/// <summary>
/// Indicates how authoritative the current config or ACL view is.
/// </summary>
public enum ConfigValidity
{
    Unknown = 0,
    OwnerAck = 1,
    HomeAAck = 2,
    RegionalAck = 3,
    TemporaryLocal = 4,
    StaleRemote = 5,
}
