using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace EtDiscovery.Web;

public static class EtDiscoveryWebBootstrap
{
    public static EtDiscoveryWebOptions LoadOptions(WebApplicationBuilder builder, string[] args)
    {
        var bootstrap = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables(prefix: "ETDISCOVERY_")
            .Build();

        var configFile = bootstrap["config-file"];
        if (!string.IsNullOrWhiteSpace(configFile))
        {
            builder.Configuration.AddJsonFile(configFile, optional: false, reloadOnChange: false);
        }

        var roles = ParseRolesFromArgs(bootstrap["roles"]);
        var settings = builder.Configuration
            .GetSection(EtDiscoveryConfiguration.SectionName)
            .Get<EtDiscoveryConfiguration>()
            ?? new EtDiscoveryConfiguration();

        return settings.BuildOptions(roles);
    }

    private static IReadOnlyList<RoleName> ParseRolesFromArgs(string? rolesValue)
    {
        if (string.IsNullOrWhiteSpace(rolesValue))
        {
            throw new ArgumentException("--roles is required and must be provided explicitly on the command line.");
        }

        var roles = rolesValue
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseRole)
            .Distinct()
            .ToArray();

        if (roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be provided via --roles.");
        }

        return roles;
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

    public string? EasyTierCorePath { get; set; }

    public string? EasyTierCliPath { get; set; }

    public string? NetworkName { get; set; }

    public string? EasyTierInstanceName { get; set; }

    public string? NetworkSecret { get; set; }

    public string? VirtualNetworkCidr { get; set; }

    public string? Ipv4 { get; set; }

    public string? ListenUrl { get; set; }

    public List<string> Peers { get; set; } = [];

    public string? RegistryPeer { get; set; }

    public List<PublishedServiceConfiguration> Services { get; set; } = [];

    public int? RefreshIntervalSeconds { get; set; }

    public EtDiscoveryWebOptions BuildOptions(IReadOnlyList<RoleName> roles)
    {
        var corePath = NormalizeBinaryPath(Require(NormalizeConfiguredPath(EasyTierCorePath), "EtDiscovery:EasyTierCorePath"));
        var cliPath = NormalizeBinaryPath(NormalizeConfiguredPath(EasyTierCliPath) ?? InferCliPath(corePath));

        var options = new EtDiscoveryWebOptions
        {
            Roles = roles,
            EasyTierCorePath = corePath,
            EasyTierCliPath = cliPath,
            EasyTierInstanceName = NullIfWhiteSpace(EasyTierInstanceName)
                ?? $"etdiscovery-{Require(NullIfWhiteSpace(NetworkName), "EtDiscovery:NetworkName")}",
            NetworkName = Require(NullIfWhiteSpace(NetworkName), "EtDiscovery:NetworkName"),
            NetworkSecret = Require(NullIfWhiteSpace(NetworkSecret), "EtDiscovery:NetworkSecret"),
            VirtualNetworkCidr = Ipv4Cidr.Parse(Require(NullIfWhiteSpace(VirtualNetworkCidr), "EtDiscovery:VirtualNetworkCidr")),
            Ipv4 = NullIfWhiteSpace(Ipv4),
            ListenUrl = Require(NullIfWhiteSpace(ListenUrl), "EtDiscovery:ListenUrl"),
            Peers = Peers
                .Where(static peer => !string.IsNullOrWhiteSpace(peer))
                .ToArray(),
            RegistryPeer = NormalizeRegistryPeer(NullIfWhiteSpace(RegistryPeer)),
            Services = Services.Select(MapService).ToArray(),
            RefreshInterval = TimeSpan.FromSeconds(RefreshIntervalSeconds.GetValueOrDefault(5)),
        };

        Validate(options);
        return options;
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

    private static void Validate(EtDiscoveryWebOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EasyTierCorePath))
        {
            throw new ArgumentException("EtDiscovery:EasyTierCorePath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.EasyTierCliPath))
        {
            throw new ArgumentException("EtDiscovery:EasyTierCliPath is required or must be inferrable from EasyTierCorePath.");
        }

        if (options.IsWorker)
        {
            if (!options.HasPublishedServices)
            {
                throw new ArgumentException("EtDiscovery:Services must contain at least one entry when roles include worker.");
            }

            if (!options.IsRegistry &&
                !options.HasPeers &&
                string.IsNullOrWhiteSpace(options.RegistryPeer))
            {
                throw new ArgumentException("Worker requires EtDiscovery:RegistryPeer or at least one EtDiscovery:Peers entry when registry role is absent.");
            }

            if (options.RequiresPeerProvidedVirtualIp && !options.HasPeers)
            {
                throw new ArgumentException("Worker requires EtDiscovery:Ipv4 when no EtDiscovery:Peers are configured.");
            }
        }

        if (options.IsRegistry &&
            options.RequiresPeerProvidedVirtualIp &&
            !options.HasPeers)
        {
            throw new ArgumentException("Registry requires EtDiscovery:Ipv4 when no EtDiscovery:Peers are configured.");
        }

        if (options.HasConfiguredVirtualIp && !options.VirtualNetworkCidr.Contains(options.ConfiguredVirtualIp))
        {
            throw new ArgumentException("EtDiscovery:Ipv4 must belong to EtDiscovery:VirtualNetworkCidr.");
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

    private static string? NormalizeRegistryPeer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : EtDiscoveryWebOptions.NormalizeIpv4(value);
    }
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
