using EtDiscovery.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace EtDiscovery.Tests;

[TestFixture]
public class EtDiscoveryWebBootstrapTests
{
    [Test]
    public void LoadOptionsFromSplitConfigurationAndRolesFromCommandLine()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "C:\\tools\\easytier-core.exe",
            ["EasyTier:Peers:0"] = "tcp://127.0.0.1:11010",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
            ["EtDiscovery:RegistryPeer"] = "10.144.144.1",
            ["EtDiscovery:Services:0:ServiceName"] = "echo",
            ["EtDiscovery:Services:0:Port"] = "8081",
            ["EtDiscovery:Services:0:Protocol"] = "http",
        });

        var options = EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry,worker"]);

        Assert.That(options.Roles, Is.EqualTo(new[] { RoleName.Registry, RoleName.Worker }));
        Assert.That(options.EasyTier.Peers, Is.EqualTo(new[] { "tcp://127.0.0.1:11010" }));
        Assert.That(options.EasyTier.CliPath, Does.Contain("easytier-cli"));
        Assert.That(options.EasyTier.InstanceName, Is.EqualTo("etdiscovery-demo-net"));
        Assert.That(options.RegistryCandidates, Is.EqualTo(new[] { "10.144.144.1" }));
        Assert.That(options.Services.Single().ServiceName, Is.EqualTo("echo"));

        var (appId, flags) = options.GetAdvertisedNodeTypeMetadata();
        Assert.That(appId, Is.EqualTo(EtDiscovery.Core.Models.EtDiscoveryNodeTypeFlags.AppId));
        Assert.That(flags & EtDiscovery.Core.Models.EtDiscoveryNodeTypeFlags.Registry, Is.Not.EqualTo(0u));
        Assert.That(flags & EtDiscovery.Core.Models.EtDiscoveryNodeTypeFlags.Worker, Is.Not.EqualTo(0u));
    }

    [Test]
    public void EmptyCliPathFallsBackToSiblingBinary()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "C:\\tools\\easytier-core.exe",
            ["EasyTier:CliPath"] = "",
            ["EasyTier:InstanceName"] = "registry-a",
            ["EasyTier:Ipv4"] = "10.144.144.1",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
        });

        var options = EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]);

        Assert.That(options.EasyTier.CliPath, Is.EqualTo("C:\\tools\\easytier-cli.exe"));
        Assert.That(options.EasyTier.InstanceName, Is.EqualTo("registry-a"));
        Assert.That(options.ShouldEnableDhcp, Is.False);
    }

    [Test]
    public void MissingRolesOnCommandLineThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
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
            ["EasyTier:CorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
            ["EtDiscovery:RegistryPeer"] = "10.144.144.1",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "worker"]),
            Throws.Exception.With.Message.Contains("EtDiscovery:Services"));
    }

    [Test]
    public void WorkerWithoutRegistryEndpointAndAutoDiscoverDisabledThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "/usr/local/bin/easytier-core",
            ["EasyTier:Peers:0"] = "tcp://127.0.0.1:11010",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
            ["EtDiscovery:AutoDiscoverFromRouteMetadata"] = "false",
            ["EtDiscovery:Services:0:ServiceName"] = "echo",
            ["EtDiscovery:Services:0:Port"] = "8081",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "worker"]),
            Throws.Exception.With.Message.Contains("RegistryCandidates"));
    }

    [Test]
    public void RegistryWithoutIpv4OrPeersThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "/usr/local/bin/easytier-core",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]),
            Throws.Exception.With.Message.Contains("Registry requires EasyTier:Ipv4"));
    }

    [Test]
    public void ConfiguredIpv4MustBelongToVirtualNetworkCidr()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "C:\\tools\\easytier-core.exe",
            ["EasyTier:Ipv4"] = "10.200.1.9",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://0.0.0.0:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]),
            Throws.Exception.With.Message.Contains("EasyTier:Ipv4 must belong"));
    }

    [Test]
    public void RegistryLoopbackListenUrlThrows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyTier:CorePath"] = "C:\\tools\\easytier-core.exe",
            ["EasyTier:Ipv4"] = "10.144.144.1",
            ["EtDiscovery:NetworkName"] = "demo-net",
            ["EtDiscovery:NetworkSecret"] = "demo-secret",
            ["EtDiscovery:VirtualNetworkCidr"] = "10.144.144.0/24",
            ["EtDiscovery:ListenUrl"] = "http://127.0.0.1:8080",
        });

        Assert.That(() => EtDiscoveryWebBootstrap.LoadOptions(builder, ["--roles", "registry"]),
            Throws.Exception.With.Message.Contains("must not bind only to loopback"));
    }
}
