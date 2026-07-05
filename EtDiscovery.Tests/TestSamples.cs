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
}

internal sealed class TestSnapshotProvider : IDiscoverySnapshotProvider
{
    public DiscoverySnapshot Snapshot { get; set; } = DiscoverySnapshot.Empty;

    public Task<DiscoverySnapshot> GetSnapshotAsync(DiscoveryNodeContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Snapshot);
    }
}
