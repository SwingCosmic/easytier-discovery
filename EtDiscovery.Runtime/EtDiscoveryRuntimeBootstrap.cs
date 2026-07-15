using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace EtDiscovery.Runtime;

public static class EtDiscoveryRuntimeBootstrap
{
    public static EtDiscoveryRuntimeOptions LoadOptions(WebApplicationBuilder builder, string[] args)
    {
        var bootstrap = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables(prefix: "ETDISCOVERY_")
            .Build();

        var configFile = FirstNonEmpty(
            bootstrap["config-file"],
            bootstrap["CONFIG_FILE"],
            bootstrap["config_file"]);
        if (!string.IsNullOrWhiteSpace(configFile))
        {
            builder.Configuration.AddJsonFile(configFile, optional: false, reloadOnChange: false);
        }

        var roles = ParseRolesFromArgs(FirstNonEmpty(
            bootstrap["roles"],
            bootstrap["ROLES"]));
        var settings = builder.Configuration
            .GetSection(EtDiscoveryConfiguration.SectionName)
            .Get<EtDiscoveryConfiguration>()
            ?? new EtDiscoveryConfiguration();
        var easyTierSettings = builder.Configuration
            .GetSection(EasyTierConfiguration.SectionName)
            .Get<EasyTierConfiguration>()
            ?? new EasyTierConfiguration();

        return settings.BuildOptions(roles, easyTierSettings);
    }

    private static IReadOnlyList<RoleName> ParseRolesFromArgs(string? rolesValue)
    {
        if (string.IsNullOrWhiteSpace(rolesValue))
        {
            throw new ArgumentException(
                "Roles are required via --roles, ETDISCOVERY_roles, or ETDISCOVERY_ROLES (comma-separated: registry, worker, client).");
        }

        var roles = rolesValue
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseRole)
            .Distinct()
            .ToArray();

        if (roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be provided via --roles / ETDISCOVERY_ROLES.");
        }

        return roles;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static RoleName ParseRole(string value) => value.ToLowerInvariant() switch
    {
        "registry" => RoleName.Registry,
        "worker" => RoleName.Worker,
        "client" => RoleName.Client,
        _ => throw new ArgumentException($"Unsupported role '{value}'. Allowed values: registry, worker, client."),
    };
}

public sealed class EtDiscoveryConfiguration
{
    public const string SectionName = "EtDiscovery";

    public string? NetworkName { get; set; }

    public string? NetworkSecret { get; set; }

    public string? VirtualNetworkCidr { get; set; }

    public string? ListenUrl { get; set; }

    public List<string> RegistryCandidates { get; set; } = [];

    /// <summary>
    /// Compatibility alias that is folded into <see cref="RegistryCandidates"/>.
    /// </summary>
    public string? RegistryPeer { get; set; }

    public int? DiscoveryPort { get; set; }

    public bool? AutoDiscoverFromRouteMetadata { get; set; }

    public List<PublishedServiceConfiguration> Services { get; set; } = [];

    public int? RefreshIntervalSeconds { get; set; }

    public EtDiscoveryRuntimeOptions BuildOptions(IReadOnlyList<RoleName> roles, EasyTierConfiguration easyTierSettings)
    {
        var corePath = NormalizeBinaryPath(Require(NormalizeConfiguredPath(easyTierSettings.CorePath), "EasyTier:CorePath"));
        var cliPath = NormalizeBinaryPath(NormalizeConfiguredPath(easyTierSettings.CliPath) ?? InferCliPath(corePath));
        var networkName = Require(NullIfWhiteSpace(NetworkName), "EtDiscovery:NetworkName");
        var listenUrl = Require(NullIfWhiteSpace(ListenUrl), "EtDiscovery:ListenUrl");
        var listenPort = new Uri(listenUrl, UriKind.Absolute).Port;

        var options = new EtDiscoveryRuntimeOptions
        {
            Roles = roles,
            NetworkName = networkName,
            NetworkSecret = Require(NullIfWhiteSpace(NetworkSecret), "EtDiscovery:NetworkSecret"),
            VirtualNetworkCidr = Ipv4Cidr.Parse(Require(NullIfWhiteSpace(VirtualNetworkCidr), "EtDiscovery:VirtualNetworkCidr")),
            ListenUrl = listenUrl,
            RegistryCandidates = BuildRegistryCandidates(RegistryCandidates, RegistryPeer),
            DiscoveryPort = DiscoveryPort.GetValueOrDefault(listenPort),
            AutoDiscoverFromRouteMetadata = AutoDiscoverFromRouteMetadata.GetValueOrDefault(true),
            Services = Services.Select(MapService).ToArray(),
            RefreshInterval = TimeSpan.FromSeconds(RefreshIntervalSeconds.GetValueOrDefault(5)),
            EasyTier = new EasyTierRuntimeOptions
            {
                CorePath = corePath,
                CliPath = cliPath,
                InstanceName = NullIfWhiteSpace(easyTierSettings.InstanceName)
                    ?? $"etdiscovery-{networkName}",
                Ipv4 = NullIfWhiteSpace(easyTierSettings.Ipv4),
                Dhcp = easyTierSettings.Dhcp,
                Peers = easyTierSettings.Peers
                    .Where(static peer => !string.IsNullOrWhiteSpace(peer))
                    .ToArray(),
                Hostname = NullIfWhiteSpace(easyTierSettings.Hostname),
                Listeners = easyTierSettings.Listeners
                    .Where(static listener => !string.IsNullOrWhiteSpace(listener))
                    .ToArray(),
                ExternalNode = NullIfWhiteSpace(easyTierSettings.ExternalNode),
                ProxyNetworks = easyTierSettings.ProxyNetworks
                    .Where(static network => !string.IsNullOrWhiteSpace(network))
                    .ToArray(),
                Flags = easyTierSettings.Flags?.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal),
            },
        };

        Validate(options);
        return options;
    }

    private static IReadOnlyList<string> BuildRegistryCandidates(IEnumerable<string> candidates, string? registryPeer)
    {
        var result = new List<string>();
        foreach (var candidate in candidates)
        {
            var normalized = NormalizeRegistryEndpoint(candidate);
            if (normalized is not null && !result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(normalized);
            }
        }

        var legacy = NormalizeRegistryEndpoint(registryPeer);
        if (legacy is not null && !result.Contains(legacy, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(legacy);
        }

        return result;
    }

    private static PublishedServiceOptions MapService(PublishedServiceConfiguration service)
    {
        return new PublishedServiceOptions
        {
            ServiceName = Require(NullIfWhiteSpace(service.ServiceName), "EtDiscovery:Services:ServiceName"),
            Port = service.Port ?? throw new ArgumentException("EtDiscovery:Services:Port is required."),
            Protocol = NullIfWhiteSpace(service.Protocol) ?? "http",
            InstanceId = NullIfWhiteSpace(service.InstanceId),
            Version = NullIfWhiteSpace(service.Version),
            Group = NullIfWhiteSpace(service.Group),
            Tags = service.Tags?.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
                ?? new Dictionary<string, string>(StringComparer.Ordinal),
            Metadata = service.Metadata?.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
                ?? new Dictionary<string, string>(StringComparer.Ordinal),
            Weight = service.Weight.GetValueOrDefault(1),
        };
    }

    private static void Validate(EtDiscoveryRuntimeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EasyTier.CorePath))
        {
            throw new ArgumentException("EasyTier:CorePath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EasyTier.CliPath))
        {
            throw new ArgumentException("EasyTier:CliPath is required or must be inferrable from EasyTier:CorePath.");
        }

        if (options.DiscoveryPort is < 1 or > 65535)
        {
            throw new ArgumentException("EtDiscovery:DiscoveryPort must be between 1 and 65535.");
        }

        if (options.IsWorker)
        {
            if (!options.HasPublishedServices)
            {
                throw new ArgumentException("EtDiscovery:Services must contain at least one entry when roles include worker.");
            }

            if (!options.IsRegistry &&
                !options.HasRegistryCandidates &&
                !options.AutoDiscoverFromRouteMetadata)
            {
                throw new ArgumentException(
                    "Worker requires EtDiscovery:RegistryCandidates (or RegistryPeer) or AutoDiscoverFromRouteMetadata=true when registry role is absent.");
            }

            if (options.RequiresPeerProvidedVirtualIp && !options.HasPeers)
            {
                throw new ArgumentException("Worker requires EasyTier:Ipv4 when no EasyTier:Peers are configured.");
            }
        }

        if (options.IsRegistry &&
            options.RequiresPeerProvidedVirtualIp &&
            !options.HasPeers)
        {
            throw new ArgumentException("Registry requires EasyTier:Ipv4 when no EasyTier:Peers are configured.");
        }

        if (options.HasConfiguredVirtualIp && !options.VirtualNetworkCidr.Contains(options.ConfiguredVirtualIp))
        {
            throw new ArgumentException("EasyTier:Ipv4 must belong to EtDiscovery:VirtualNetworkCidr.");
        }

        if (options.IsRegistry && IsLoopbackListenUrl(options.ListenUrl))
        {
            throw new ArgumentException(
                "Registry EtDiscovery:ListenUrl must not bind only to loopback (127.0.0.1/localhost). " +
                "Workers reach the registry via the EasyTier virtual IP (e.g. http://10.x.x.x:8080). " +
                "Use http://0.0.0.0:8080 (or the virtual IP) so Kestrel accepts overlay traffic.");
        }

        var duplicateInstanceId = options.Services
            .GroupBy(service => service.InstanceId, StringComparer.Ordinal)
            .FirstOrDefault(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

        if (duplicateInstanceId is not null)
        {
            throw new ArgumentException($"EtDiscovery:Services contains duplicate InstanceId '{duplicateInstanceId.Key}'.");
        }
    }

    private static string Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{key} is required.");
        }

        return value;
    }

    private static string InferCliPath(string easyTierCorePath)
    {
        var directory = Path.GetDirectoryName(easyTierCorePath);
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "easytier-cli.exe" : "easytier-cli";
        return directory is null ? fileName : Path.Combine(directory, fileName);
    }

    private static string NormalizeBinaryPath(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return path;
        }

        return Path.HasExtension(path) ? path : $"{path}.exe";
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? NormalizeConfiguredPath(string? value)
    {
        var normalized = NullIfWhiteSpace(value);
        if (normalized is null)
        {
            return null;
        }

        var fileName = Path.GetFileName(normalized);
        return string.IsNullOrWhiteSpace(fileName) ? null : normalized;
    }

    private static string? NormalizeRegistryEndpoint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : EtDiscoveryRuntimeOptions.NormalizeIpv4(value);
    }

    private static bool IsLoopbackListenUrl(string listenUrl)
    {
        if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.IsLoopback ||
               string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class EasyTierConfiguration
{
    public const string SectionName = "EasyTier";

    public string? CorePath { get; set; }

    public string? CliPath { get; set; }

    public string? InstanceName { get; set; }

    public string? Ipv4 { get; set; }

    public bool? Dhcp { get; set; }

    public List<string> Peers { get; set; } = [];

    public string? Hostname { get; set; }

    public List<string> Listeners { get; set; } = [];

    public string? ExternalNode { get; set; }

    public List<string> ProxyNetworks { get; set; } = [];

    public Dictionary<string, string>? Flags { get; set; }
}

public sealed class PublishedServiceConfiguration
{
    public string? ServiceName { get; set; }

    public int? Port { get; set; }

    public string? Protocol { get; set; }

    public string? InstanceId { get; set; }

    public string? Version { get; set; }

    public string? Group { get; set; }

    public Dictionary<string, string>? Tags { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public int? Weight { get; set; }
}
