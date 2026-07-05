using EtDiscovery.Web;
using EtDiscovery.Web.Models;
using EtDiscovery.Web.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class PeerObservationMapperTests
{
    [Test]
    public void MarksForeignNetworkPeerAsIneligible()
    {
        var options = CreateOptions();
        var mapper = new PeerObservationMapper();

        var snapshot = mapper.Map(
            options,
            new EasyTierNodeInfo
            {
                PeerId = 1U,
                Hostname = "registry",
                Ipv4Addr = "10.144.144.1/24",
            },
            [
                new EasyTierPeerListItem
                {
                    Cidr = "10.144.144.1/24",
                    Ipv4 = "10.144.144.1",
                    Hostname = "registry",
                    Cost = "Local",
                    Id = "1",
                },
                new EasyTierPeerListItem
                {
                    Cidr = "10.144.144.2/24",
                    Ipv4 = "10.144.144.2",
                    Hostname = "worker-a",
                    Cost = "p2p",
                    Id = "2",
                },
                new EasyTierPeerListItem
                {
                    Cidr = "10.200.1.2/24",
                    Ipv4 = "10.200.1.2",
                    Hostname = "foreign-worker",
                    Cost = "p2p",
                    Id = "3",
                },
            ],
            new Dictionary<string, ForeignNetworkEntry>(StringComparer.Ordinal)
            {
                ["foreign-net"] = new ForeignNetworkEntry
                {
                    Peers = [new ForeignNetworkPeer { PeerId = 3U }],
                },
            });

        var sameNetworkPeer = snapshot.Peers.Single(peer => peer.PeerId == 2U);
        var foreignPeer = snapshot.Peers.Single(peer => peer.PeerId == 3U);

        Assert.That(sameNetworkPeer.SameNetwork, Is.True);
        Assert.That(sameNetworkPeer.InVirtualNetworkCidr, Is.True);
        Assert.That(sameNetworkPeer.EligibleForDiscovery, Is.True);

        Assert.That(foreignPeer.SameNetwork, Is.False);
        Assert.That(foreignPeer.NetworkName, Is.EqualTo("foreign-net"));
        Assert.That(foreignPeer.EligibleForDiscovery, Is.False);
    }

    [Test]
    public void RejectsPeerOutsideVirtualNetworkRange()
    {
        var options = CreateOptions();
        var mapper = new PeerObservationMapper();

        var snapshot = mapper.Map(
            options,
            new EasyTierNodeInfo
            {
                PeerId = 1U,
                Hostname = "registry",
                Ipv4Addr = "10.144.144.1/24",
            },
            [
                new EasyTierPeerListItem
                {
                    Cidr = "10.144.145.2/24",
                    Ipv4 = "10.144.145.2",
                    Hostname = "worker-b",
                    Cost = "p2p",
                    Id = "2",
                },
            ],
            new Dictionary<string, ForeignNetworkEntry>(StringComparer.Ordinal));

        var peer = snapshot.Peers.Single();
        Assert.That(peer.SameNetwork, Is.True);
        Assert.That(peer.InVirtualNetworkCidr, Is.False);
        Assert.That(peer.EligibleForDiscovery, Is.False);
    }

    [Test]
    public void FallsBackToConfiguredLocalVirtualIpWhenNodeInfoIsEmpty()
    {
        var baseOptions = CreateOptions();
        var options = new EtDiscoveryWebOptions
        {
            Roles = baseOptions.Roles,
            EasyTierCorePath = baseOptions.EasyTierCorePath,
            EasyTierCliPath = baseOptions.EasyTierCliPath,
            NetworkName = baseOptions.NetworkName,
            NetworkSecret = baseOptions.NetworkSecret,
            VirtualNetworkCidr = baseOptions.VirtualNetworkCidr,
            Ipv4 = "10.144.144.9",
            ListenUrl = baseOptions.ListenUrl,
            Peers = baseOptions.Peers,
            RegistryWorkerServiceName = baseOptions.RegistryWorkerServiceName,
            RegistryWorkerServicePort = baseOptions.RegistryWorkerServicePort,
        };

        var mapper = new PeerObservationMapper();
        var snapshot = mapper.Map(
            options,
            new EasyTierNodeInfo
            {
                PeerId = 1U,
                Hostname = "registry",
                Ipv4Addr = "",
            },
            [
                new EasyTierPeerListItem
                {
                    Cidr = "",
                    Ipv4 = "",
                    Hostname = "registry",
                    Cost = "Local",
                    Id = "1",
                },
            ],
            new Dictionary<string, ForeignNetworkEntry>(StringComparer.Ordinal));

        Assert.That(snapshot.LocalNode.VirtualIp, Is.EqualTo("10.144.144.9"));
        Assert.That(snapshot.Peers.Single().VirtualIp, Is.EqualTo("10.144.144.9"));
        Assert.That(snapshot.Peers.Single().EligibleForDiscovery, Is.True);
    }

    private static EtDiscoveryWebOptions CreateOptions() => new()
    {
        Roles = [RoleName.Registry],
        EasyTierCorePath = "/usr/local/bin/easytier-core",
        EasyTierCliPath = "/usr/local/bin/easytier-cli",
        NetworkName = "demo-net",
        NetworkSecret = "demo-secret",
        VirtualNetworkCidr = Ipv4Cidr.Parse("10.144.144.0/24"),
        ListenUrl = "http://127.0.0.1:8080",
        Peers = [],
        RegistryWorkerServiceName = "echo",
        RegistryWorkerServicePort = 8081,
    };
}
