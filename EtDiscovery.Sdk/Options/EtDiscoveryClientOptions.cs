namespace EtDiscovery.Sdk;

/// <summary>
/// Business-side thin configuration (runtime endpoint + local service identity).
/// No NetworkSecret, EasyTier, Mode, or Roles — those belong to the runtime host.
/// </summary>
public sealed class EtDiscoveryClientOptions
{
    public const string DefaultSectionName = "EtDiscovery";

    public const string HttpClientName = "EtDiscovery.Sdk";

    /// <summary>Local runtime base URL, e.g. http://127.0.0.1:8081</summary>
    public string RuntimeEndpoint { get; set; } = "http://127.0.0.1:8081";

    /// <summary>Logical service name when this process is a provider.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Listening port of this process (must match real bind).</summary>
    public int? Port { get; set; }

    public string Protocol { get; set; } = "http";

    public string? InstanceId { get; set; }

    public string? Version { get; set; }

    public string? Group { get; set; }

    public Dictionary<string, string> Tags { get; set; } = new(StringComparer.Ordinal);

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>When true and ServiceName+Port are set, register on host start.</summary>
    public bool AutoRegisterOnStart { get; set; } = true;

    /// <summary>When true and registered, run background heartbeats.</summary>
    public bool AutoHeartbeat { get; set; } = true;

    public bool CanPublish =>
        !string.IsNullOrWhiteSpace(ServiceName) && Port is > 0 and <= 65535;
}
