using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class DiscoveryCatalogService
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly DiscoveryEngine _engine;
    private readonly DiscoveryNodeContext _context;
    private readonly object _sync = new();
    private DateTimeOffset? _lastRefreshAt;
    private DiscoverySnapshot _lastSnapshot = DiscoverySnapshot.Empty;

    public DiscoveryCatalogService(
        EtDiscoveryWebOptions options,
        DiscoveryEngine engine,
        DiscoveryNodeContext context)
    {
        _options = options;
        _engine = engine;
        _context = context;
    }

    public DateTimeOffset? GetLastRefreshAt()
    {
        lock (_sync)
        {
            return _lastRefreshAt;
        }
    }

    public DiscoverySnapshot GetLastSnapshot()
    {
        lock (_sync)
        {
            return _lastSnapshot;
        }
    }

    public void ApplySnapshot(DiscoverySnapshot snapshot)
    {
        if (!_context.Capabilities.CanManageCatalog)
        {
            throw new InvalidOperationException("The current node role does not allow catalog management.");
        }

        _engine.ApplySnapshot(snapshot);
        lock (_sync)
        {
            _lastSnapshot = snapshot;
            _lastRefreshAt = DateTimeOffset.UtcNow;
        }
    }

    public IReadOnlyList<ServiceInstance> ResolveServices(string? serviceName = null)
    {
        EnsureRegistry();

        var query = new ServiceQuery("default", serviceName ?? _options.RegistryWorkerServiceName!, "http");
        return _engine.Resolve(query);
    }

    public SelectedInstance? SelectOne(string serviceName)
    {
        EnsureRegistry();
        return _engine.SelectOne(new ServiceQuery("default", serviceName, "http"));
    }

    private void EnsureRegistry()
    {
        if (!_options.IsRegistry)
        {
            throw new InvalidOperationException("Discovery APIs require the registry role.");
        }
    }
}
