using System.Diagnostics;
using System.Text.Json;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class EasyTierCliClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private const int MaxTransientAttempts = 10;

    private readonly EtDiscoveryWebOptions _options;
    private readonly EtDiscoveryProcessManager _processManager;
    private readonly ILogger<EasyTierCliClient> _logger;

    public EasyTierCliClient(
        EtDiscoveryWebOptions options,
        EtDiscoveryProcessManager processManager,
        ILogger<EasyTierCliClient> logger)
    {
        _options = options;
        _processManager = processManager;
        _logger = logger;
    }

    public Task<IReadOnlyList<EasyTierPeerListItem>> GetPeerListAsync(CancellationToken cancellationToken)
        => ReadOrEmptyAsync<IReadOnlyList<EasyTierPeerListItem>>(new[] { "peer", "list" }, cancellationToken);

    public Task<EasyTierNodeInfo?> GetNodeInfoAsync(CancellationToken cancellationToken)
        => RunJsonAsync<EasyTierNodeInfo>(new[] { "node", "info" }, cancellationToken);

    public Task<Dictionary<string, ForeignNetworkEntry>> GetForeignNetworksAsync(CancellationToken cancellationToken)
        => ReadOrEmptyAsync<Dictionary<string, ForeignNetworkEntry>>(new[] { "peer", "list-foreign" }, cancellationToken);

    private async Task<T> ReadOrEmptyAsync<T>(IReadOnlyList<string> subcommand, CancellationToken cancellationToken) where T : class
    {
        return await RunJsonAsync<T>(subcommand, cancellationToken) ?? CreateEmptyResult<T>();
    }

    private async Task<T?> RunJsonAsync<T>(IReadOnlyList<string> subcommand, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var result = await RunOnceAsync<T>(subcommand, cancellationToken);
            if (result.Success)
            {
                return result.Value;
            }

            if (attempt >= MaxTransientAttempts || !IsTransientInstanceUnavailable(result.ExitCode, result.StandardError))
            {
                throw new InvalidOperationException($"easytier-cli exited with code {result.ExitCode}: {result.StandardError}");
            }

            _logger.LogInformation(
                "easytier-cli API is not ready yet. command={Command} attempt={Attempt}/{MaxAttempts} rpcPortal={RpcPortalAddress} instanceName={InstanceName}",
                string.Join(' ', subcommand),
                attempt,
                MaxTransientAttempts,
                _processManager.RpcPortalAddress,
                _options.EasyTierInstanceName);

            await Task.Delay(RetryDelay, cancellationToken);
        }
    }

    private async Task<CliInvocationResult<T>> RunOnceAsync<T>(IReadOnlyList<string> subcommand, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.EasyTierCliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(_processManager.RpcPortalAddress);
        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add(_options.EasyTierInstanceName);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("json");

        foreach (var part in subcommand)
        {
            startInfo.ArgumentList.Add(part);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start easytier-cli.");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            return new CliInvocationResult<T>(false, default, process.ExitCode, stderr);
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            return new CliInvocationResult<T>(true, default, process.ExitCode, stderr);
        }

        return new CliInvocationResult<T>(true, JsonSerializer.Deserialize<T>(stdout, JsonOptions), process.ExitCode, stderr);
    }

    private static T CreateEmptyResult<T>() where T : class
    {
        if (typeof(T) == typeof(IReadOnlyList<EasyTierPeerListItem>))
        {
            return (T)(object)Array.Empty<EasyTierPeerListItem>();
        }

        if (typeof(T) == typeof(Dictionary<string, ForeignNetworkEntry>))
        {
            return (T)(object)new Dictionary<string, ForeignNetworkEntry>(StringComparer.Ordinal);
        }

        throw new InvalidOperationException($"No empty result factory is registered for {typeof(T).FullName}.");
    }

    private static bool IsTransientInstanceUnavailable(int exitCode, string? stderr)
    {
        if (exitCode == 0 || string.IsNullOrWhiteSpace(stderr))
        {
            return false;
        }

        return stderr.Contains("Instance not found or API service not available", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CliInvocationResult<T>(bool Success, T? Value, int ExitCode, string? StandardError);
}
