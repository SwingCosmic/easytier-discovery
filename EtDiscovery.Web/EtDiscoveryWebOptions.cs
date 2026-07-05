using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Web;

public sealed class EtDiscoveryWebOptions
{
    public required IReadOnlyList<RoleName> Roles { get; init; }

    public required string EasyTierCorePath { get; init; }

    public required string EasyTierCliPath { get; init; }

    public required string EasyTierInstanceName { get; init; }

    public required string NetworkName { get; init; }

    public required string NetworkSecret { get; init; }

    public required Ipv4Cidr VirtualNetworkCidr { get; init; }

    public string? Ipv4 { get; init; }

    public required string ListenUrl { get; init; }

    public required IReadOnlyList<string> Peers { get; init; }

    public string? WorkerServiceName { get; init; }

    public int? WorkerServicePort { get; init; }

    public string? RegistryWorkerServiceName { get; init; }

    public int? RegistryWorkerServicePort { get; init; }

    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromSeconds(5);

    public bool HasRole(RoleName role) => Roles.Contains(role);

    public bool IsWorker => HasRole(RoleName.Worker);

    public bool IsRegistry => HasRole(RoleName.Registry);

    public bool IsClient => HasRole(RoleName.Client);

    public bool HasConfiguredVirtualIp => !string.IsNullOrWhiteSpace(ConfiguredVirtualIp);

    public string? ConfiguredVirtualIp => NormalizeIpv4(Ipv4);

    public bool HasPeers => Peers.Count > 0;

    public bool RequiresPeerProvidedVirtualIp => !HasConfiguredVirtualIp;

    public bool ShouldEnableDhcp =>
        IsWorker && RequiresPeerProvidedVirtualIp;

    public bool RequiresWindowsElevationForEasyTier =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        (IsRegistry || HasConfiguredVirtualIp || ShouldEnableDhcp);

    public bool RequiresTunDevice =>
        HasConfiguredVirtualIp || ShouldEnableDhcp;

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

    public ServiceKey GetRegistryServiceKey()
    {
        if (RegistryWorkerServiceName is null)
        {
            throw new InvalidOperationException("Registry worker service name is not configured.");
        }

        return new ServiceKey("default", RegistryWorkerServiceName, "http");
    }

    public ServiceKey GetWorkerServiceKey()
    {
        if (WorkerServiceName is null)
        {
            throw new InvalidOperationException("Worker service name is not configured.");
        }

        return new ServiceKey("default", WorkerServiceName, "http");
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
