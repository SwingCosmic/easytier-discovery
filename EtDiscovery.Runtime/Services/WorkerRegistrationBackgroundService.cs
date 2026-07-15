using Microsoft.Extensions.Hosting;

namespace EtDiscovery.Runtime.Services;

public sealed class WorkerRegistrationBackgroundService : BackgroundService
{
    private readonly EtDiscoveryRuntimeOptions _options;
    private readonly EasyTierObservationService _observationService;
    private readonly WorkerRegistrationOrchestrator _orchestrator;
    private readonly ILogger<WorkerRegistrationBackgroundService> _logger;

    public WorkerRegistrationBackgroundService(
        EtDiscoveryRuntimeOptions options,
        EasyTierObservationService observationService,
        WorkerRegistrationOrchestrator orchestrator,
        ILogger<WorkerRegistrationBackgroundService> logger)
    {
        _options = options;
        _observationService = observationService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsWorker)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await _observationService.GetCurrentSnapshotAsync(stoppingToken);
                await _orchestrator.SyncAsync(snapshot, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to synchronize worker service registrations.");
            }

            await Task.Delay(_options.RefreshInterval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _orchestrator.DeregisterAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deregister worker services during shutdown.");
        }

        await base.StopAsync(cancellationToken);
    }
}
