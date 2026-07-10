using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Web;

public sealed class EtDiscoveryWebOptions
{
    public required IReadOnlyList<RoleName> Roles { get; init; }

    /// <summary>
    /// Temporarily kept on the EtDiscovery section for network identity matching and EasyTier TOML generation.
    /// </summary>
    public required string NetworkName { get; init; }

    /// <summary>
    /// Temporarily kept on the EtDiscovery section for EasyTier TOML generation.
    /// </summary>
    public required string NetworkSecret { get; init; }

    public required Ipv4Cidr VirtualNetworkCidr { get; init; }

    public required string ListenUrl { get; init; }

    public required IReadOnlyList<string> RegistryCandidates { get; init; }

    public required int DiscoveryPort { get; init; }

    public bool AutoDiscoverFromRouteMetadata { get; init; } = true;

    public required IReadOnlyList<PublishedServiceOptions> Services { get; init; }

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromSeconds(5);

    public required EasyTierRuntimeOptions EasyTier { get; init; }

    public bool HasRole(RoleName role) => Roles.Contains(role);

    public bool IsWorker => HasRole(RoleName.Worker);

    public bool IsRegistry => HasRole(RoleName.Registry);

    public bool IsClient => HasRole(RoleName.Client);

    public bool HasConfiguredVirtualIp => !string.IsNullOrWhiteSpace(ConfiguredVirtualIp);

    public string? ConfiguredVirtualIp => NormalizeIpv4(EasyTier.Ipv4);

    public bool HasPeers => EasyTier.Peers.Count > 0;

    public bool HasRegistryCandidates => RegistryCandidates.Count > 0;

    public bool RequiresPeerProvidedVirtualIp => !HasConfiguredVirtualIp;

    public bool ShouldEnableDhcp =>
        EasyTier.Dhcp ?? (IsWorker && RequiresPeerProvidedVirtualIp);

    public bool RequiresWindowsElevationForEasyTier =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        (IsRegistry || HasConfiguredVirtualIp || ShouldEnableDhcp);

    public bool RequiresTunDevice =>
        HasConfiguredVirtualIp || ShouldEnableDhcp;

    public bool HasPublishedServices => Services.Count > 0;

    public int ListenPort => new Uri(ListenUrl, UriKind.Absolute).Port;

    /// <summary>
    /// Role bits are always derived from <see cref="Roles"/> and never accepted from config.
    /// </summary>
    public (uint AppId, uint Flags) GetAdvertisedNodeTypeMetadata()
    {
        var nodeRoles = Roles.Select(RoleNameMapper.ToNodeRole);
        return EtDiscoveryNodeTypeFlags.EncodeRoles(nodeRoles);
    }

    public IReadOnlyList<string> GetPrivilegeChecklist()
    {
        var items = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (RequiresWindowsElevationForEasyTier)
            {
                items.Add("Windows: run this process as Administrator because EasyTier needs to create or update the virtual adapter.");
            }
            else
            {
                items.Add("Windows: Administrator is not strictly required for the current role/configuration, but may still be needed for some optional EasyTier features.");
            }

            items.Add("Windows: if DHCP or a virtual IPv4 is enabled, losing Administrator privileges can prevent the worker or registry from obtaining a usable virtual IP.");
            items.Add("Windows: firewall allowlist and raw UDP broadcast helper features may also require Administrator privileges.");
            return items;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (RequiresTunDevice)
            {
                items.Add("Linux: a TUN-capable setup is required when using a static virtual IPv4 or DHCP-assigned virtual IPv4.");
                items.Add("Linux: EasyTier source code suggests running as root, or otherwise ensuring the process can access /dev/net/tun and create or configure the TUN device.");
                items.Add("Linux: if /dev/net/tun is missing, load the tun module with modprobe and ensure the device node exists.");
            }
            else
            {
                items.Add("Linux: current configuration does not require a local TUN device because no static virtual IPv4 or DHCP-assigned virtual IPv4 is requested.");
            }

            items.Add("Linux: some optional socket-marking features require CAP_NET_ADMIN, but that is separate from the minimum TUN requirement.");
            return items;
        }

        items.Add("Current platform privilege checklist is only curated for Windows and Linux.");
        return items;
    }

    public Uri GetRegistryBaseUri(string? registryAddress = null)
    {
        if (IsRegistry)
        {
            return new Uri(ListenUrl, UriKind.Absolute);
        }

        if (string.IsNullOrWhiteSpace(registryAddress))
        {
            throw new InvalidOperationException("Registry endpoint is not configured.");
        }

        if (Uri.TryCreate(registryAddress, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri($"http://{registryAddress}:{DiscoveryPort}", UriKind.Absolute);
    }

    public static string? NormalizeIpv4(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var slash = value.IndexOf('/');
        return slash >= 0 ? value[..slash] : value;
    }

    public static bool IsCurrentProcessElevated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IsCurrentProcessPrivilegedOnUnixLike();
        }

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool IsCurrentProcessPrivilegedOnUnixLike()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return true;
        }

        try
        {
            return geteuid() == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("libc")]
    private static extern uint geteuid();
}

/// <summary>
/// EasyTier-native runtime options. Peers and underlay knobs live here, not under EtDiscovery.
/// Node type flags are intentionally absent and always derived from roles.
/// </summary>
public sealed class EasyTierRuntimeOptions
{
    public required string CorePath { get; init; }

    public required string CliPath { get; init; }

    public required string InstanceName { get; init; }

    public string? Ipv4 { get; init; }

    public bool? Dhcp { get; init; }

    public required IReadOnlyList<string> Peers { get; init; }

    public string? Hostname { get; init; }

    public IReadOnlyList<string> Listeners { get; init; } = [];

    public string? ExternalNode { get; init; }

    public IReadOnlyList<string> ProxyNetworks { get; init; } = [];

    public IReadOnlyDictionary<string, string> Flags { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class PublishedServiceOptions
{
    public required string ServiceName { get; init; }

    public required int Port { get; init; }

    public string Protocol { get; init; } = "http";

    public string? InstanceId { get; init; }

    public string? Version { get; init; }

    public string? Group { get; init; }

    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public int Weight { get; init; } = 1;

    public string ResolveInstanceId(string nodeId)
    {
        if (!string.IsNullOrWhiteSpace(InstanceId))
        {
            return InstanceId;
        }

        return $"{nodeId}:{ServiceName}:{Protocol}:{Port}";
    }

    public ServiceDefinition CreateDefinition()
    {
        return new ServiceDefinition("default", ServiceName, Protocol)
        {
            Version = Version,
            Group = Group,
            Tags = MergeTags(),
        };
    }

    public IReadOnlyDictionary<string, string> MergeTags()
    {
        var tags = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in Tags)
        {
            tags[entry.Key] = entry.Value;
        }

        foreach (var entry in Metadata)
        {
            tags[entry.Key] = entry.Value;
        }

        return tags;
    }
}

public enum RoleName
{
    Registry,
    Worker,
    Client,
}

public static class RoleNameMapper
{
    public static NodeRole ToNodeRole(RoleName role) => role switch
    {
        RoleName.Registry => NodeRole.Registry,
        RoleName.Worker => NodeRole.Worker,
        RoleName.Client => NodeRole.Client,
        _ => NodeRole.Unknown,
    };
}

public readonly record struct Ipv4Cidr(IPAddress NetworkAddress, int PrefixLength)
{
    public static Ipv4Cidr Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid IPv4 CIDR value '{value}'.");
        }

        if (!IPAddress.TryParse(parts[0], out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new ArgumentException($"Invalid IPv4 address in CIDR '{value}'.");
        }

        if (!int.TryParse(parts[1], out var prefixLength) || prefixLength is < 0 or > 32)
        {
            throw new ArgumentException($"Invalid IPv4 prefix length in CIDR '{value}'.");
        }

        return new Ipv4Cidr(address, prefixLength);
    }

    public bool Contains(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || !IPAddress.TryParse(address, out var parsed))
        {
            return false;
        }

        return Contains(parsed);
    }

    public bool Contains(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        Span<byte> networkBytes = stackalloc byte[4];
        Span<byte> addressBytes = stackalloc byte[4];
        NetworkAddress.TryWriteBytes(networkBytes, out _);
        address.TryWriteBytes(addressBytes, out _);

        var fullBytes = PrefixLength / 8;
        var remainingBits = PrefixLength % 8;

        for (var index = 0; index < fullBytes; index++)
        {
            if (networkBytes[index] != addressBytes[index])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (networkBytes[fullBytes] & mask) == (addressBytes[fullBytes] & mask);
    }

    public override string ToString() => $"{NetworkAddress}/{PrefixLength}";
}
