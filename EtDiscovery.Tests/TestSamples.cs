using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;

namespace EtDiscovery.Tests;

internal static class TestSamples
{
    public static DiscoveryRuntime Runtime(TestSnapshotProvider? snapshotProvider = null)
    {
        var engine = new DiscoveryEngine(
            new ReachableNodeProcessingPolicy(),
            new RoundRobinServiceSelectionPolicy());

        return new DiscoveryRuntime(
            new DiscoveryNodeContext("node-under-test", [NodeRole.Registry]),
            engine,
            snapshotProvider ?? new TestSnapshotProvider());
    }

    public static ServiceKey ServiceKey() => new("default", "echo", "http");

    public static ServiceQuery Query() => new("default", "echo", "http");

    public static DiscoverySnapshot Snapshot(params object[] entries)
    {
        var nodes = entries.OfType<NodeSnapshot>().ToArray();
        var instances = entries.OfType<ServiceInstance>().ToArray();
        return new DiscoverySnapshot(nodes, instances);
    }

    public static EtDiscovery.Runtime.EtDiscoveryRuntimeOptions WebOptions(
        IReadOnlyList<EtDiscovery.Runtime.RoleName>? roles = null,
        IReadOnlyList<EtDiscovery.Runtime.PublishedServiceOptions>? services = null,
        string? ipv4 = "10.144.144.1",
        IReadOnlyList<string>? registryCandidates = null,
        IReadOnlyList<string>? peers = null,
        bool autoDiscoverFromRouteMetadata = true)
    {
        return new EtDiscovery.Runtime.EtDiscoveryRuntimeOptions
        {
            Roles = roles ?? [EtDiscovery.Runtime.RoleName.Registry],
            NetworkName = "demo-net",
            NetworkSecret = "demo-secret",
            VirtualNetworkCidr = EtDiscovery.Runtime.Ipv4Cidr.Parse("10.144.144.0/24"),
            ListenUrl = "http://0.0.0.0:8080",
            RegistryCandidates = registryCandidates ?? ["10.144.144.1"],
            DiscoveryPort = 8080,
            AutoDiscoverFromRouteMetadata = autoDiscoverFromRouteMetadata,
            Services = services ?? [],
            EasyTier = new EtDiscovery.Runtime.EasyTierRuntimeOptions
            {
                CorePath = "/usr/local/bin/easytier-core",
                CliPath = "/usr/local/bin/easytier-cli",
                InstanceName = "etdiscovery-demo-net",
                Ipv4 = ipv4,
                Peers = peers ?? ["tcp://127.0.0.1:11010"],
            },
        };
    }
}

internal sealed class TestSnapshotProvider : IDiscoverySnapshotProvider
{
    public DiscoverySnapshot Snapshot { get; set; } = DiscoverySnapshot.Empty;

    public Task<DiscoverySnapshot> GetSnapshotAsync(DiscoveryNodeContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Snapshot);
    }
}
