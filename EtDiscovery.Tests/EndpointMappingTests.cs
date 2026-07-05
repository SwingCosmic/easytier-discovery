using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;
using EtDiscovery.Web;
using EtDiscovery.Web.Endpoints;
using EtDiscovery.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EtDiscovery.Tests;

[TestFixture]
public class EndpointMappingTests
{
    [Test]
    public void MapsRoutesWithoutApiPrefix()
    {
        var builder = WebApplication.CreateBuilder();
        var options = new EtDiscoveryWebOptions
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

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new DiscoveryNodeContext("local-node", [NodeRole.Registry]));
        builder.Services.AddSingleton(new DiscoveryEngine(new ReachableNodeProcessingPolicy(), new RoundRobinServiceSelectionPolicy()));
        builder.Services.AddSingleton<EtDiscoveryProcessManager>();
        builder.Services.AddSingleton<EasyTierCliClient>();
        builder.Services.AddSingleton<PeerObservationMapper>();
        builder.Services.AddSingleton<EasyTierObservationService>();
        builder.Services.AddSingleton<RegistrySnapshotBuilder>();
        builder.Services.AddSingleton<DiscoveryCatalogService>();

        var app = builder.Build();
        app.MapEtDiscoveryEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        Assert.That(routes, Does.Contain("/health"));
        Assert.That(routes, Does.Contain("/easytier/peers"));
        Assert.That(routes, Does.Contain("/test/ping"));
        Assert.That(routes, Does.Contain("/discovery/services"));
        Assert.That(routes, Does.Contain("/discovery/select"));
        Assert.That(routes.Any(route => route is not null && route.StartsWith("/api", StringComparison.Ordinal)), Is.False);
    }
}
