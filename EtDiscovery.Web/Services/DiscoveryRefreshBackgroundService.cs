using Microsoft.Extensions.Hosting;

namespace EtDiscovery.Web.Services;

public sealed class DiscoveryRefreshBackgroundService : BackgroundService
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly EasyTierObservationService _observationService;
    private readonly RegistrySnapshotBuilder _snapshotBuilder;
    private readonly DiscoveryCatalogService _catalogService;
    private readonly ILogger<DiscoveryRefreshBackgroundService> _logger;
    private HashSet<string> _lastCandidateNodeIds = [];
    private HashSet<string> _lastInstanceIds = [];

    public DiscoveryRefreshBackgroundService(
        EtDiscoveryWebOptions options,
        EasyTierObservationService observationService,
        RegistrySnapshotBuilder snapshotBuilder,
        DiscoveryCatalogService catalogService,
        ILogger<DiscoveryRefreshBackgroundService> logger)
    {
        _options = options;
        _observationService = observationService;
        _snapshotBuilder = snapshotBuilder;
        _catalogService = catalogService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsRegistry)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var observation = await _observationService.GetCurrentSnapshotAsync(stoppingToken);
                var snapshot = _snapshotBuilder.Build(observation);
                LogDiscoveryChanges(observation, snapshot);
                _catalogService.ApplySnapshot(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh discovery catalog from EasyTier.");
            }

            await Task.Delay(_options.RefreshInterval, stoppingToken);
        }
    }

    private void LogDiscoveryChanges(EtDiscovery.Web.Models.EasyTierObservationSnapshot observation, EtDiscovery.Core.Models.DiscoverySnapshot snapshot)
    {
        var candidateNodeIds = snapshot.Nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
        var instanceIds = snapshot.Instances.Select(instance => instance.InstanceId).ToHashSet(StringComparer.Ordinal);

        foreach (var nodeId in candidateNodeIds.Except(_lastCandidateNodeIds, StringComparer.Ordinal))
        {
            var peer = observation.Peers.FirstOrDefault(item => string.Equals(item.NodeId, nodeId, StringComparison.Ordinal));
            _logger.LogInformation(
                "Registry candidate online. nodeId={NodeId} hostname={Hostname} virtualIp={VirtualIp} serviceName={ServiceName}",
                nodeId,
                peer?.Hostname ?? "<unknown>",
                string.IsNullOrWhiteSpace(peer?.VirtualIp) ? "<empty>" : peer!.VirtualIp,
                _options.RegistryWorkerServiceName ?? _options.WorkerServiceName ?? "<unknown>");
        }

        foreach (var nodeId in _lastCandidateNodeIds.Except(candidateNodeIds, StringComparer.Ordinal))
        {
            _logger.LogInformation("Registry candidate offline. nodeId={NodeId}", nodeId);
        }

        if (!_lastCandidateNodeIds.SetEquals(candidateNodeIds) || !_lastInstanceIds.SetEquals(instanceIds))
        {
            _logger.LogInformation(
                "Registry catalog updated. eligibleNodes={EligibleNodeCount} serviceInstances={ServiceInstanceCount}",
                snapshot.Nodes.Count,
                snapshot.Instances.Count);
        }

        _lastCandidateNodeIds = candidateNodeIds;
        _lastInstanceIds = instanceIds;
    }
}
