using EtDiscovery.Core.Models;
using EtDiscovery.Web;
using EtDiscovery.Web.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class EasyTierConfigGeneratorTests
{
    [Test]
    public void GeneratesTomlWithRoleDerivedNodeTypeFlagsAndPeers()
    {
        var options = new EtDiscoveryWebOptions
        {
            Roles = [RoleName.Registry, RoleName.Worker],
            NetworkName = "demo-net",
            NetworkSecret = "demo-secret",
            VirtualNetworkCidr = Ipv4Cidr.Parse("10.144.144.0/24"),
            ListenUrl = "http://127.0.0.1:8080",
            RegistryCandidates = [],
            DiscoveryPort = 8080,
            AutoDiscoverFromRouteMetadata = true,
            Services = [],
            EasyTier = new EasyTierRuntimeOptions
            {
                CorePath = "/usr/local/bin/easytier-core",
                CliPath = "/usr/local/bin/easytier-cli",
                InstanceName = "registry-a",
                Ipv4 = "10.144.144.1",
                Peers = ["tcp://seed.example.com:11010"],
                Hostname = "registry-host",
                Listeners = ["tcp://0.0.0.0:11010", "udp://0.0.0.0:11010"],
                Flags = new Dictionary<string, string>
                {
                    ["latency_first"] = "true",
                },
            },
        };

        var toml = new EasyTierConfigGenerator().GenerateToml(options);
        var expectedFlags = EtDiscoveryNodeTypeFlags.Registry | EtDiscoveryNodeTypeFlags.Worker;

        Assert.That(toml, Does.Contain("instance_name = \"registry-a\""));
        Assert.That(toml, Does.Contain("hostname = \"registry-host\""));
        Assert.That(toml, Does.Contain("ipv4 = \"10.144.144.1\""));
        Assert.That(toml, Does.Contain("dhcp = false"));
        Assert.That(toml, Does.Contain($"node_type_app_id = {EtDiscoveryNodeTypeFlags.AppId}"));
        Assert.That(toml, Does.Contain($"node_type_flags = {expectedFlags}"));
        Assert.That(toml, Does.Contain("network_name = \"demo-net\""));
        Assert.That(toml, Does.Contain("network_secret = \"demo-secret\""));
        Assert.That(toml, Does.Contain("[[peer]]"));
        Assert.That(toml, Does.Contain("uri = \"tcp://seed.example.com:11010\""));
        Assert.That(toml, Does.Contain("listeners = [\"tcp://0.0.0.0:11010\", \"udp://0.0.0.0:11010\"]"));
        Assert.That(toml, Does.Contain("[flags]"));
        Assert.That(toml, Does.Contain("latency_first = true"));
    }

    [Test]
    public void EmitsDefaultListenersWhenNoneConfigured()
    {
        var options = TestSamples.WebOptions(roles: [RoleName.Registry], ipv4: "10.144.144.1");
        var toml = new EasyTierConfigGenerator().GenerateToml(options);

        Assert.That(toml, Does.Contain("listeners = ["));
        Assert.That(toml, Does.Contain("tcp://0.0.0.0:11010"));
        Assert.That(toml, Does.Contain("udp://0.0.0.0:11010"));
    }
}
