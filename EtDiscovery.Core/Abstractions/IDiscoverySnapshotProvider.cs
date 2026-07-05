using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Supplies raw discovery snapshots for the runtime refresh loop.
/// </summary>
public interface IDiscoverySnapshotProvider
{
    Task<DiscoverySnapshot> GetSnapshotAsync(DiscoveryNodeContext context, CancellationToken cancellationToken = default);
}
