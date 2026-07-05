using EtDiscovery.Web;
using EtDiscovery.Web.Models;
using EtDiscovery.Web.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class RegistrySnapshotBuilderTests
{
    [Test]
    public void BuildsRemoteAndLocalWorkerInstances()
    {
        var options = new EtDiscoveryWebOptions
        {
            Roles = [RoleName.Registry, RoleName.Worker],
            EasyTierCorePath = "/usr/local/bin/easytier-core",
            EasyTierCliPath = "/usr/local/bin/easytier-cli",
            NetworkName = "demo-net",
            NetworkSecret = "demo-secret",
            VirtualNetworkCidr = Ipv4Cidr.Parse("10.144.144.0/24"),
            ListenUrl = "http://127.0.0.1:8080",
            Peers = [],
            WorkerServiceName = "worker-ping",
            WorkerServicePort = 9000,
            RegistryWorkerServiceName = "remote-ping",
            RegistryWorkerServicePort = 8081,
        };

        var builder = new RegistrySnapshotBuilder(options);
        var snapshot = builder.Build(new EasyTierObservationSnapshot
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
        });

        Assert.That(snapshot.Nodes.Select(node => node.NodeId), Is.EquivalentTo(new[] { "peer:1", "peer:2" }));
        Assert.That(snapshot.Instances.Select(instance => instance.ServiceKey.ServiceName), Is.EquivalentTo(new[] { "worker-ping", "remote-ping" }));
        Assert.That(snapshot.Instances.Single(instance => instance.NodeId == "peer:2").Port, Is.EqualTo(8081));
        Assert.That(snapshot.Instances.Single(instance => instance.NodeId == "peer:1").Port, Is.EqualTo(9000));
    }
}
