namespace EtDiscovery.Core.Models;

/// <summary>
/// Mutually exclusive catalog responsibilities for the local node.
/// </summary>
public enum CatalogCapability
{
    None = 0,

    ObserveCatalog = 1,

    ManageCatalog = 2,
}
