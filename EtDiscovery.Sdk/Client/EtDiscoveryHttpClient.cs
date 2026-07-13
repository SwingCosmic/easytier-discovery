using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EtDiscovery.Core.Models;
using Microsoft.Extensions.Options;

namespace EtDiscovery.Sdk;

/// <summary>
/// HTTP implementation of <see cref="IEtDiscoveryClient"/> against local /runtime/v1.
/// </summary>
public sealed class EtDiscoveryHttpClient : IEtDiscoveryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly EtDiscoveryClientOptions _options;
    private string? _registeredInstanceId;

    public EtDiscoveryHttpClient(HttpClient http, IOptions<EtDiscoveryClientOptions> options)
        : this(http, options.Value)
    {
    }

    public EtDiscoveryHttpClient(HttpClient http, EtDiscoveryClientOptions options)
    {
        _http = http;
        _options = options;
        ConfigureBaseAddress();
    }

    public string? RegisteredInstanceId => _registeredInstanceId;

    public async Task<SelectedInstance?> SelectOneAsync(
        string serviceName,
        string? protocol = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        var query = BuildServiceQuery(serviceName, protocol);
        using var response = await _http.GetAsync($"runtime/v1/select?{query}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SelectedInstance>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceInstance>> ResolveAsync(
        string serviceName,
        string? protocol = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        var query = BuildServiceQuery(serviceName, protocol);
        using var response = await _http.GetAsync($"runtime/v1/resolve?{query}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<ServiceInstance>>(JsonOptions, cancellationToken);
        return list ?? [];
    }

    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CanPublish)
        {
            throw new InvalidOperationException(
                "Register requires EtDiscovery:ServiceName and EtDiscovery:Port to be configured.");
        }

        var payload = new
        {
            serviceName = _options.ServiceName,
            port = _options.Port,
            protocol = _options.Protocol,
            instanceId = _options.InstanceId,
            version = _options.Version,
            group = _options.Group,
            tags = _options.Tags,
        };

        using var response = await _http.PostAsJsonAsync("runtime/v1/instances", payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions, cancellationToken);
        _registeredInstanceId = body?.InstanceId
            ?? _options.InstanceId
            ?? $"{_options.ServiceName}:{_options.Protocol}:{_options.Port}";
    }

    public async Task HeartbeatAsync(CancellationToken cancellationToken = default)
    {
        var instanceId = RequireRegisteredInstanceId();
        using var response = await _http.PutAsync(
            $"runtime/v1/instances/{Uri.EscapeDataString(instanceId)}/heartbeat",
            content: null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeregisterAsync(CancellationToken cancellationToken = default)
    {
        var instanceId = _registeredInstanceId ?? _options.InstanceId;
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return;
        }

        using var response = await _http.DeleteAsync(
            $"runtime/v1/instances/{Uri.EscapeDataString(instanceId)}",
            cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _registeredInstanceId = null;
            return;
        }

        response.EnsureSuccessStatusCode();
        _registeredInstanceId = null;
    }

    private void ConfigureBaseAddress()
    {
        if (_http.BaseAddress is not null)
        {
            return;
        }

        var endpoint = _options.RuntimeEndpoint.TrimEnd('/') + "/";
        _http.BaseAddress = new Uri(endpoint, UriKind.Absolute);
        _http.Timeout = _options.HttpTimeout;
    }

    private static string BuildServiceQuery(string serviceName, string? protocol)
    {
        var parts = new List<string> { $"serviceName={Uri.EscapeDataString(serviceName)}" };
        if (!string.IsNullOrWhiteSpace(protocol))
        {
            parts.Add($"protocol={Uri.EscapeDataString(protocol)}");
        }

        return string.Join('&', parts);
    }

    private string RequireRegisteredInstanceId()
    {
        if (!string.IsNullOrWhiteSpace(_registeredInstanceId))
        {
            return _registeredInstanceId;
        }

        if (!string.IsNullOrWhiteSpace(_options.InstanceId))
        {
            return _options.InstanceId;
        }

        throw new InvalidOperationException("No registered instance id. Call RegisterAsync first.");
    }

    private sealed class RegisterResponse
    {
        public string? InstanceId { get; set; }
    }
}
