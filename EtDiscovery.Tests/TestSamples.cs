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

    public static EtDiscovery.Web.EtDiscoveryWebOptions WebOptions(
        IReadOnlyList<EtDiscovery.Web.RoleName>? roles = null,
        IReadOnlyList<EtDiscovery.Web.PublishedServiceOptions>? services = null,
        string? ipv4 = "10.144.144.1")
    {
        return new EtDiscovery.Web.EtDiscoveryWebOptions
        {
            Roles = roles ?? [EtDiscovery.Web.RoleName.Registry],
            EasyTierCorePath = "/usr/local/bin/easytier-core",
            EasyTierCliPath = "/usr/local/bin/easytier-cli",
            EasyTierInstanceName = "etdiscovery-demo-net",
            NetworkName = "demo-net",
            NetworkSecret = "demo-secret",
            VirtualNetworkCidr = EtDiscovery.Web.Ipv4Cidr.Parse("10.144.144.0/24"),
            Ipv4 = ipv4,
            ListenUrl = "http://127.0.0.1:8080",
            Peers = ["tcp://127.0.0.1:11010"],
            RegistryPeer = "10.144.144.1",
            Services = services ?? [],
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
