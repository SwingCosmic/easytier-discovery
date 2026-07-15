using Microsoft.Extensions.Hosting;

namespace EtDiscovery.Runtime.Services;

public sealed class EasyTierVirtualIpMonitor : IHostedService
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);

    private readonly EtDiscoveryRuntimeOptions _options;
    private readonly EasyTierObservationService _observationService;
    private readonly ILogger<EasyTierVirtualIpMonitor> _logger;

    public EasyTierVirtualIpMonitor(
        EtDiscoveryRuntimeOptions options,
        EasyTierObservationService observationService,
        ILogger<EasyTierVirtualIpMonitor> logger)
    {
        _options = options;
        _observationService = observationService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.HasConfiguredVirtualIp)
        {
            _logger.LogInformation(
                "Using configured local virtual IP. roles={Roles} virtualIp={VirtualIp}",
                string.Join(",", _options.Roles.Select(role => role.ToString().ToLowerInvariant())),
                _options.ConfiguredVirtualIp);
            return;
        }

        _logger.LogInformation(
            "Waiting for peer-provided EasyTier virtual IP. roles={Roles} peers={PeerCount} dhcpEnabled={DhcpEnabled} timeoutSeconds={TimeoutSeconds}",
            string.Join(",", _options.Roles.Select(role => role.ToString().ToLowerInvariant())),
            _options.EasyTier.Peers.Count,
            _options.ShouldEnableDhcp,
            (int)StartupTimeout.TotalSeconds);

        var startedAt = DateTimeOffset.UtcNow;
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow - startedAt < StartupTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var snapshot = await _observationService.GetCurrentSnapshotAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(snapshot.LocalNode.VirtualIp))
                {
                    _logger.LogInformation(
                        "Acquired EasyTier virtual IP from peer network. nodeId={NodeId} virtualIp={VirtualIp}",
                        snapshot.LocalNode.NodeId,
                        snapshot.LocalNode.VirtualIp);
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Failed while waiting for a peer-provided EasyTier virtual IP.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException(
            $"No EasyTier virtual IP was acquired within {(int)StartupTimeout.TotalSeconds} seconds. " +
            "Configure EasyTier:Ipv4 explicitly, ensure EasyTier:Peers can connect to the registry " +
            "(registry must listen on the advertised peer port), or verify DHCP on a joined network.",
            lastError);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
