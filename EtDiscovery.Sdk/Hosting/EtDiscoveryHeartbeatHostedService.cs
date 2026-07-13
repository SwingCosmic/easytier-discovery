using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EtDiscovery.Sdk;

/// <summary>
/// Registers on start and heartbeats while the host runs (provider path).
/// </summary>
public sealed class EtDiscoveryHeartbeatHostedService : BackgroundService
{
    private readonly IEtDiscoveryClient _client;
    private readonly EtDiscoveryClientOptions _options;
    private readonly ILogger<EtDiscoveryHeartbeatHostedService> _logger;

    public EtDiscoveryHeartbeatHostedService(
        IEtDiscoveryClient client,
        IOptions<EtDiscoveryClientOptions> options,
        ILogger<EtDiscoveryHeartbeatHostedService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.CanPublish || (!_options.AutoRegisterOnStart && !_options.AutoHeartbeat))
        {
            return;
        }

        if (_options.AutoRegisterOnStart)
        {
            try
            {
                await _client.RegisterAsync(stoppingToken);
                _logger.LogInformation(
                    "Registered with local EtDiscovery runtime as {ServiceName}:{Port}",
                    _options.ServiceName,
                    _options.Port);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to register with local EtDiscovery runtime.");
            }
        }

        if (!_options.AutoHeartbeat)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.HeartbeatInterval, stoppingToken);
                await _client.HeartbeatAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EtDiscovery heartbeat failed.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.CanPublish)
        {
            try
            {
                await _client.DeregisterAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EtDiscovery deregister failed during shutdown.");
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
