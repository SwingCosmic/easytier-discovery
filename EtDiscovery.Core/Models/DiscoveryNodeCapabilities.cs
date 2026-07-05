namespace EtDiscovery.Core.Models;

/// <summary>
/// Final capability set resolved from one or more node roles.
/// </summary>
public sealed class DiscoveryNodeCapabilities
{
    public CatalogCapability Catalog { get; set; } = CatalogCapability.None;

    public ServicePublicationCapability Publication { get; set; } = ServicePublicationCapability.None;

    public DiscoveryAccessCapability DiscoveryAccess { get; set; } = DiscoveryAccessCapability.None;

    public bool CanManageCatalog => Catalog >= CatalogCapability.ManageCatalog;

    public bool CanObserveCatalog => Catalog >= CatalogCapability.ObserveCatalog;

    public bool CanPublishOwnedServices => Publication >= ServicePublicationCapability.PublishOwnedServices;

    public bool CanResolveServices => DiscoveryAccess >= DiscoveryAccessCapability.ResolveOnly;

    public bool CanSelectServices => DiscoveryAccess >= DiscoveryAccessCapability.ResolveSelectWatchAndReport;

    public bool CanWatchServices => DiscoveryAccess >= DiscoveryAccessCapability.ResolveSelectWatchAndReport;

    public bool CanReportCallFeedback => DiscoveryAccess >= DiscoveryAccessCapability.ResolveSelectWatchAndReport;

    public static DiscoveryNodeCapabilities ForRoles(IEnumerable<NodeRole> roles)
    {
        var roleSet = roles.Distinct().ToArray();

        var capabilities = new DiscoveryNodeCapabilities
        {
            Catalog = ResolveCatalogCapability(roleSet),
            Publication = ResolvePublicationCapability(roleSet),
            DiscoveryAccess = ResolveDiscoveryAccessCapability(roleSet),
        };

        return capabilities;
    }

    private static CatalogCapability ResolveCatalogCapability(IReadOnlyCollection<NodeRole> roles)
    {
        if (roles.Contains(NodeRole.Registry))
        {
            return CatalogCapability.ManageCatalog;
        }

        if (roles.Contains(NodeRole.Worker) || roles.Contains(NodeRole.Client))
        {
            return CatalogCapability.ObserveCatalog;
        }

        return CatalogCapability.None;
    }

    private static ServicePublicationCapability ResolvePublicationCapability(IReadOnlyCollection<NodeRole> roles)
    {
        if (roles.Contains(NodeRole.Worker))
        {
            return ServicePublicationCapability.PublishOwnedServices;
        }

        return ServicePublicationCapability.None;
    }

    private static DiscoveryAccessCapability ResolveDiscoveryAccessCapability(IReadOnlyCollection<NodeRole> roles)
    {
        if (roles.Contains(NodeRole.Registry) || roles.Contains(NodeRole.Worker) || roles.Contains(NodeRole.Client))
        {
            return DiscoveryAccessCapability.ResolveSelectWatchAndReport;
        }

        return DiscoveryAccessCapability.None;
    }
}
