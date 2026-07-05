namespace EtDiscovery.Core.Models;

/// <summary>
/// Options for the background discovery refresh loop.
/// </summary>
public sealed class DiscoveryRuntimeOptions
{
    public bool EnableBackgroundRefresh { get; set; }

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(5);
}
