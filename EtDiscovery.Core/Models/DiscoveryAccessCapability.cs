namespace EtDiscovery.Core.Models;

/// <summary>
/// Mutually exclusive service-consumption capabilities for the local node.
/// </summary>
public enum DiscoveryAccessCapability
{
    None = 0,

    ResolveOnly = 1,

    ResolveSelectWatchAndReport = 2,
}
