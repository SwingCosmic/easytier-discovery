using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Rebuilds a service catalog from one raw discovery snapshot.
/// </summary>
public interface INodeProcessingPolicy
{
    ServiceCatalog BuildCatalog(DiscoverySnapshot snapshot);
}
