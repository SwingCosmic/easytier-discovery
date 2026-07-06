using EtDiscovery.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace EtDiscovery.Tests;

[TestFixture]
public class EtDiscoveryWebBootstrapTests
{
    [Test]
    public void LoadOptionsFromConfigurationAndRolesFromCommandLine()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "C:\\tools\\easytier-core.exe",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
            ["EtDiscovery:Peers:0"] = "tcp://127.0.0.1:11010",
            ["EtDiscovery:RegistryPeer"] = "10.144.144.1",
            ["EtDiscovery:Services:0:ServiceName"] = "echo",
            ["EtDiscovery:Services:0:Port"] = "8081",
            ["EtDiscovery:Services:0:Protocol"] = "http",
        });

        var options = EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry,worker"]);

        Assert.That(options.Roles, Is.EqualTo(new[] { RoleName.Registry, RoleName.Worker }));
        Assert.That(options.Peers, Is.EqualTo(new[] { "tcp://127.0.0.1:11010" }));
        Assert.That(options.EasyTierCliPath, Does.Contain("easytier-cli"));
        Assert.That(options.EasyTierInstanceName, Is.EqualTo("etdiscovery-demo-net"));
        Assert.That(options.Services.Single().ServiceName, Is.EqualTo("echo"));
    }

    [Test]
    public void EmptyCliPathFallsBackToSiblingBinary()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "C:\\tools\\easytier-core.exe",
            ["EtDiscovery:EasyTierCliPath"] = "",
            ["EtDiscovery:EasyTierInstanceName"] = "registry-a",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
            ["EtDiscovery:Ipv4"] = "10.144.144.1",
        });

        var options = EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]);

        Assert.That(options.EasyTierCliPath, Is.EqualTo("C:\\tools\\easytier-cli.exe"));
        Assert.That(options.EasyTierInstanceName, Is.EqualTo("registry-a"));
        Assert.That(options.ShouldEnableDhcp, Is.False);
    }

    [Test]
    public void MissingRolesOnCommandLineThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, []),
            Throws.Exception.With.Message.Contains("--roles"));
    }

    [Test]
    public void MissingWorkerServicesThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
            ["EtDiscovery:RegistryPeer"] = "10.144.144.1",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "worker"]),
            Throws.Exception.With.Message.Contains("EtDiscovery:Services"));
    }

    [Test]
    public void WorkerWithoutRegistryEndpointThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
            ["EtDiscovery:Peers:0"] = "tcp://127.0.0.1:11010",
            ["EtDiscovery:Services:0:ServiceName"] = "echo",
            ["EtDiscovery:Services:0:Port"] = "8081",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "worker"]),
            Throws.Exception.With.Message.Contains("RegistryPeer"));
    }

    [Test]
    public void RegistryWithoutIpv4OrPeersThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]),
            Throws.Exception.With.Message.Contains("Registry requires EtDiscovery:Ipv4"));
    }

    [Test]
    public void ConfiguredIpv4MustBelongToVirtualNetworkCidr()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EtDiscovery:EasyTierCorePath"] = "C:\\tools\\easytier-core.exe",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:Ipv4"] = "10.200.1.9",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]),
            Throws.Exception.With.Message.Contains("EtDiscovery:Ipv4 must belong"));
    }
}
