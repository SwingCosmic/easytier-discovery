using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;
using EtDiscovery.Web.Models;
using System.Threading;

namespace EtDiscovery.Web.Services;

public sealed class DiscoveryCatalogService
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly DiscoveryEngine _engine;
    private readonly DiscoveryNodeContext _context;
    private CatalogSnapshotState _state = CatalogSnapshotState.Empty;

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
        return Volatile.Read(ref _state).LastRefreshAt;
    }

    public DiscoverySnapshot GetLastSnapshot()
    {
        return Volatile.Read(ref _state).Snapshot;
    }

    public void ApplySnapshot(DiscoverySnapshot snapshot)
    {
        if (!_context.Capabilities.CanManageCatalog)
        {
            throw new InvalidOperationException("The current node role does not allow catalog management.");
        }

        _engine.ApplySnapshot(snapshot);
        Volatile.Write(ref _state, new CatalogSnapshotState(snapshot, DateTimeOffset.UtcNow));
    }

    public IReadOnlyList<ServiceInstance> ResolveServices(string? serviceName = null)
    {
        EnsureRegistry();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return _engine.GetAllInstances();
        }

        var query = new ServiceQuery("default", serviceName, "http");
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

    private sealed record CatalogSnapshotState(DiscoverySnapshot Snapshot, DateTimeOffset? LastRefreshAt)
    {
        public static CatalogSnapshotState Empty { get; } = new(DiscoverySnapshot.Empty, null);
    }
}
