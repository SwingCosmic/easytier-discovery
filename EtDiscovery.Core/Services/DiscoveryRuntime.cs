using System.Runtime.CompilerServices;
using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Services;

/// <summary>
/// Role-aware runtime wrapper that drives snapshot refresh and exposes async APIs.
/// </summary>
public sealed class DiscoveryRuntime : IDiscoveryRuntime
{
    private readonly DiscoveryEngine _engine;
    private readonly IDiscoverySnapshotProvider? _snapshotProvider;
    private readonly DiscoveryRuntimeOptions _options;
    private readonly object _lifecycleSync = new();
    private CancellationTokenSource? _backgroundCts;
    private Task? _backgroundTask;

    public DiscoveryRuntime(
        DiscoveryNodeContext context,
        DiscoveryEngine engine,
        IDiscoverySnapshotProvider? snapshotProvider = null,
        DiscoveryRuntimeOptions? options = null)
    {
        Context = context;
        _engine = engine;
        _snapshotProvider = snapshotProvider;
        _options = options ?? new DiscoveryRuntimeOptions();
    }

    public DiscoveryNodeContext Context { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleSync)
        {
            if (_backgroundTask is not null || !_options.EnableBackgroundRefresh || _snapshotProvider is null)
            {
                return Task.CompletedTask;
            }

            _backgroundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _backgroundTask = Task.Run(() => RunRefreshLoopAsync(_backgroundCts.Token), _backgroundCts.Token);
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? backgroundTask;

        lock (_lifecycleSync)
        {
            _backgroundCts?.Cancel();
            backgroundTask = _backgroundTask;
            _backgroundTask = null;
            _backgroundCts = null;
        }

        if (backgroundTask is not null)
        {
            await backgroundTask.WaitAsync(cancellationToken);
        }
    }

    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanManageCatalog, "refresh discovery snapshots");

        if (_snapshotProvider is null)
        {
            throw new InvalidOperationException("No snapshot provider is configured for the runtime.");
        }

        var snapshot = await _snapshotProvider.GetSnapshotAsync(Context, cancellationToken);
        _engine.ApplySnapshot(snapshot);
    }

    public Task RegisterServiceAsync(RegisterServiceRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanPublishOwnedServices, "register services");
        return Task.CompletedTask;
    }

    public Task RenewAsync(string instanceId, long? leaseEpoch = null, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanPublishOwnedServices, "renew services");
        return Task.CompletedTask;
    }

    public Task DeregisterAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanPublishOwnedServices, "deregister services");
        return Task.CompletedTask;
    }

    public Task SetDrainingAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanPublishOwnedServices, "set draining");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ServiceInstance>> ResolveAsync(ServiceQuery query, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanResolveServices, "resolve services");
        return Task.FromResult(_engine.Resolve(query));
    }

    public Task<SelectedInstance?> SelectOneHealthyInstanceAsync(ServiceQuery query, CallContext? callContext = null, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanSelectServices, "select service instances");
        return Task.FromResult(_engine.SelectOne(query));
    }

    public Task<IReadOnlyList<SelectedInstance>> SelectManyHealthyInstancesAsync(ServiceQuery query, CallContext? callContext = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanSelectServices, "select service instances");
        return Task.FromResult(_engine.SelectMany(query, limit));
    }

    public async IAsyncEnumerable<InstanceWatchEvent> WatchAsync(ServiceQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanWatchServices, "watch services");
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        var instances = await ResolveAsync(query, cancellationToken);
        yield return new InstanceWatchEvent(WatchEventType.Snapshot, query, instances, DateTimeOffset.UtcNow);
    }

    public Task<NodeProfile?> GetNodeProfileAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanObserveCatalog, "read node profiles");
        return Task.FromResult(_engine.GetNodeProfile(nodeId));
    }

    public Task<CallModeRecommendation?> RecommendCallModeAsync(string instanceId, CallContext? callContext = null, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanSelectServices, "recommend call modes");
        return Task.FromResult(_engine.RecommendCallMode(instanceId));
    }

    public Task ReportCallResultAsync(ReportCallResultRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanReportCallFeedback, "report call feedback");
        return Task.CompletedTask;
    }

    public Task OpenCircuitAsync(string instanceId, string reason, CancellationToken cancellationToken = default)
    {
        EnsureCapability(Context.Capabilities.CanReportCallFeedback, "open circuits");
        return Task.CompletedTask;
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await RefreshOnceAsync(cancellationToken);
            await Task.Delay(_options.RefreshInterval, cancellationToken);
        }
    }

    private static void EnsureCapability(bool allowed, string action)
    {
        if (!allowed)
        {
            throw new InvalidOperationException($"The current node role does not allow this action: {action}.");
        }
    }
}
