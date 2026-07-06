using EtDiscovery.Web.Models;
using EtDiscovery.Web.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class RegistrySnapshotBuilderTests
{
    [Test]
    public void BuildsNodeSnapshotsAndKeepsRegisteredInstances()
    {
        var builder = new RegistrySnapshotBuilder();
        var serviceKey = TestSamples.ServiceKey();

        var snapshot = builder.Build(
            new EasyTierObservationSnapshot
            {
                ObservedAt = DateTimeOffset.UtcNow,
                LocalNode = new LocalNodeView
                {
                    NodeId = "peer:1",
                    NetworkName = "demo-net",
                    VirtualIp = "10.144.144.1",
                },
                Peers =
                [
                    new ObservedPeer
                    {
                        NodeId = "peer:1",
                        NetworkName = "demo-net",
                        VirtualIp = "10.144.144.1",
                        PeerId = 1U,
                        IsLocal = true,
                        Reachable = true,
                        SameNetwork = true,
                        InVirtualNetworkCidr = true,
                        EligibleForDiscovery = true,
                        Roles = [EtDiscovery.Core.Models.NodeRole.Registry, EtDiscovery.Core.Models.NodeRole.Worker],
                    },
                    new ObservedPeer
                    {
                        NodeId = "peer:2",
                        NetworkName = "demo-net",
                        VirtualIp = "10.144.144.2",
                        PeerId = 2U,
                        Reachable = true,
                        SameNetwork = true,
                        InVirtualNetworkCidr = true,
                        EligibleForDiscovery = true,
                        Roles = [EtDiscovery.Core.Models.NodeRole.Worker],
                    },
                ],
            },
            [
                new EtDiscovery.Core.Models.ServiceInstance("inst-a", serviceKey, "peer:1", "10.144.144.1", 9000),
                new EtDiscovery.Core.Models.ServiceInstance("inst-b", serviceKey, "peer:2", "10.144.144.2", 8081),
            ]);

        Assert.That(snapshot.Nodes.Select(node => node.NodeId), Is.EquivalentTo(new[] { "peer:1", "peer:2" }));
        Assert.That(snapshot.Instances.Select(instance => instance.InstanceId), Is.EquivalentTo(new[] { "inst-a", "inst-b" }));
    }
}
