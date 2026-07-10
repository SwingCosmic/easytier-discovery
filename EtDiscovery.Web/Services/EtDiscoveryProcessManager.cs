using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace EtDiscovery.Web.Services;

public sealed class EtDiscoveryProcessManager : IHostedService, IDisposable
{
    private const int MaxBufferedLogLines = 20;
    private readonly EtDiscoveryWebOptions _options;
    private readonly EasyTierConfigGenerator _configGenerator;
    private readonly ILogger<EtDiscoveryProcessManager> _logger;
    private readonly object _sync = new();
    private Process? _process;
    private DateTimeOffset? _startedAt;
    private int? _exitCode;
    private string? _lastError;
    private string? _generatedConfigPath;
    private readonly Queue<string> _recentStdout = new();
    private readonly Queue<string> _recentStderr = new();

    public EtDiscoveryProcessManager(
        EtDiscoveryWebOptions options,
        EasyTierConfigGenerator configGenerator,
        ILogger<EtDiscoveryProcessManager> logger)
    {
        _options = options;
        _configGenerator = configGenerator;
        _logger = logger;
        RpcPortalAddress = AllocateRpcPortalAddress();
    }

    public string RpcPortalAddress { get; }

    public string? GeneratedConfigPath
    {
        get
        {
            lock (_sync)
            {
                return _generatedConfigPath;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogPrivilegeChecklist();

        lock (_sync)
        {
            _recentStdout.Clear();
            _recentStderr.Clear();
            _exitCode = null;
            _lastError = null;

            if (_process is not null)
            {
                return;
            }

            EnsurePrivileges();

            _generatedConfigPath = _configGenerator.WriteTempConfig(_options);
            var startInfo = BuildStartInfo(_generatedConfigPath);
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            process.Exited += (_, _) =>
            {
                string[] recentStderr;
                string[] recentStdout;
                lock (_sync)
                {
                    _exitCode = process.ExitCode;
                    recentStderr = _recentStderr.ToArray();
                    recentStdout = _recentStdout.ToArray();
                }

                _logger.LogWarning(
                    "easytier-core exited. pid={Pid} exitCode={ExitCode} rpcPortal={RpcPortalAddress} config={ConfigPath} recentStderr={RecentStderr} recentStdout={RecentStdout}",
                    process.Id,
                    process.ExitCode,
                    RpcPortalAddress,
                    _generatedConfigPath,
                    recentStderr.Length == 0 ? "<empty>" : string.Join(Environment.NewLine, recentStderr),
                    recentStdout.Length == 0 ? "<empty>" : string.Join(Environment.NewLine, recentStdout));
            };

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                {
                    lock (_sync)
                    {
                        EnqueueRecentLine(_recentStdout, eventArgs.Data);
                    }

                    _logger.LogDebug("easytier-core stdout: {Message}", eventArgs.Data);
                }
            };

            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                {
                    lock (_sync)
                    {
                        EnqueueRecentLine(_recentStderr, eventArgs.Data);
                        _lastError = eventArgs.Data;
                    }

                    _logger.LogDebug("easytier-core stderr: {Message}", eventArgs.Data);
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start easytier-core.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _process = process;
            _startedAt = DateTimeOffset.UtcNow;

            var (appId, flags) = _options.GetAdvertisedNodeTypeMetadata();
            _logger.LogInformation(
                "Started easytier-core. pid={Pid} roles={Roles} networkName={NetworkName} instanceName={InstanceName} virtualIp={VirtualIp} dhcpEnabled={DhcpEnabled} rpcPortal={RpcPortalAddress} config={ConfigPath} peers={PeerCount} nodeTypeAppId={NodeTypeAppId} nodeTypeFlags=0x{NodeTypeFlags:X8}",
                process.Id,
                string.Join(",", _options.Roles.Select(role => role.ToString().ToLowerInvariant())),
                _options.NetworkName,
                _options.EasyTier.InstanceName,
                _options.ConfiguredVirtualIp ?? "<auto>",
                _options.ShouldEnableDhcp,
                RpcPortalAddress,
                _generatedConfigPath,
                _options.EasyTier.Peers.Count,
                appId,
                flags);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Process? process;
        string? configPath;
        lock (_sync)
        {
            process = _process;
            _process = null;
            configPath = _generatedConfigPath;
        }

        if (process is not null)
        {
            _logger.LogInformation("Stopping easytier-core. pid={Pid}", process.Id);

            try
            {
                if (process.HasExited)
                {
                    _exitCode = process.ExitCode;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(cancellationToken);
                    _exitCode = process.ExitCode;
                }
                else
                {
                    try
                    {
                        var killStartInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/kill",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                        };
                        killStartInfo.ArgumentList.Add("-TERM");
                        killStartInfo.ArgumentList.Add(process.Id.ToString());
                        using var killer = Process.Start(killStartInfo);
                    }
                    catch (Exception ex)
                    {
                        _lastError = ex.Message;
                    }

                    await Task.WhenAny(process.WaitForExitAsync(cancellationToken), Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }

                    await process.WaitForExitAsync(cancellationToken);
                    _exitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        TryDeleteConfig(configPath);
    }

    public EasyTierProcessStatus GetStatus()
    {
        lock (_sync)
        {
            var (appId, flags) = _options.GetAdvertisedNodeTypeMetadata();
            return new EasyTierProcessStatus
            {
                RpcPortalAddress = RpcPortalAddress,
                Pid = _process?.HasExited == false ? _process.Id : null,
                StartedAt = _startedAt,
                ExitCode = _exitCode,
                Running = _process?.HasExited == false,
                LastError = _lastError,
                RecentStdout = _recentStdout.ToArray(),
                RecentStderr = _recentStderr.ToArray(),
                ConfigPath = _generatedConfigPath,
                Arguments = BuildArgumentSummary(_generatedConfigPath),
                NodeTypeAppId = appId,
                NodeTypeFlags = flags,
            };
        }
    }

    private ProcessStartInfo BuildStartInfo(string configPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.EasyTier.CorePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(configPath);
        startInfo.ArgumentList.Add("--rpc-portal");
        startInfo.ArgumentList.Add(RpcPortalAddress);
        return startInfo;
    }

    private IReadOnlyList<string> BuildArgumentSummary(string? configPath)
    {
        return
        [
            "-c", configPath ?? "<pending>",
            "--rpc-portal", RpcPortalAddress,
        ];
    }

    private static string AllocateRpcPortalAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        return $"{endpoint.Address}:{endpoint.Port}";
    }

    private void LogPrivilegeChecklist()
    {
        _logger.LogInformation(
            "EasyTier privilege summary. platform={Platform} elevated={Elevated} requiresTunDevice={RequiresTunDevice} requiresWindowsElevation={RequiresWindowsElevation} checklist={Checklist}",
            RuntimeInformation.OSDescription,
            EtDiscoveryWebOptions.IsCurrentProcessElevated(),
            _options.RequiresTunDevice,
            _options.RequiresWindowsElevationForEasyTier,
            string.Join(Environment.NewLine, _options.GetPrivilegeChecklist()));
    }

    private void EnsurePrivileges()
    {
        if (!_options.RequiresWindowsElevationForEasyTier)
        {
            return;
        }

        if (EtDiscoveryWebOptions.IsCurrentProcessElevated())
        {
            return;
        }

        throw new InvalidOperationException(
            "This EasyTier node requires administrator privileges on Windows because EasyTier needs to create or manage the virtual adapter for a registry node, a static virtual IP, or DHCP-assigned virtual IP.");
    }

    private static void EnqueueRecentLine(Queue<string> queue, string line)
    {
        queue.Enqueue(line);
        while (queue.Count > MaxBufferedLogLines)
        {
            queue.Dequeue();
        }
    }

    private void TryDeleteConfig(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return;
        }

        try
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete generated EasyTier config {ConfigPath}", configPath);
        }
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            try
            {
                _process.CancelOutputRead();
                _process.CancelErrorRead();
            }
            catch (InvalidOperationException)
            {
            }
        }

        _process?.Dispose();
        TryDeleteConfig(_generatedConfigPath);
    }
}

public sealed class EasyTierProcessStatus
{
    public string? RpcPortalAddress { get; init; }

    public int? Pid { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public int? ExitCode { get; init; }

    public bool Running { get; init; }

    public string? LastError { get; init; }

    public IReadOnlyList<string> RecentStdout { get; init; } = [];

    public IReadOnlyList<string> RecentStderr { get; init; } = [];

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string? ConfigPath { get; init; }

    public uint NodeTypeAppId { get; init; }

    public uint NodeTypeFlags { get; init; }
}
