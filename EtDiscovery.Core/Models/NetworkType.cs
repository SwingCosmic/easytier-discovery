namespace EtDiscovery.Core.Models;

/// <summary>
/// Rough classification of the node's current access network.
/// </summary>
public enum NetworkType
{
    Unknown = 0,
    Wired = 1,
    Wifi = 2,
    Cellular = 3,
}
