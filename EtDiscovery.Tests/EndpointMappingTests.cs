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
        var options = TestSamples.WebOptions(
            roles: [RoleName.Registry],
            services:
            [
                new PublishedServiceOptions
                {
                    ServiceName = "echo",
                    Port = 8081,
                },
            ]);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new DiscoveryNodeContext("local-node", [NodeRole.Registry]));
        builder.Services.AddSingleton(new DiscoveryEngine(new ReachableNodeProcessingPolicy(), new RoundRobinServiceSelectionPolicy()));
        builder.Services.AddSingleton<DiscoveryInstanceRegistry>();
        builder.Services.AddSingleton<EtDiscoveryProcessManager>();
        builder.Services.AddSingleton<EasyTierCliClient>();
        builder.Services.AddSingleton<PeerObservationMapper>();
        builder.Services.AddSingleton<EasyTierObservationService>();
        builder.Services.AddSingleton<RegistrySnapshotBuilder>();
        builder.Services.AddSingleton<DiscoveryCatalogService>();
        builder.Services.AddLogging();

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
        Assert.That(routes, Does.Contain("/discovery/instances"));
        Assert.That(routes, Does.Contain("/discovery/instances/{instanceId}"));
        Assert.That(routes, Does.Contain("/discovery/services"));
        Assert.That(routes, Does.Contain("/discovery/select"));
        Assert.That(routes, Does.Contain("/discovery/instances/{instanceId}/lease"));
        Assert.That(routes.Any(route => route is not null && route.StartsWith("/api", StringComparison.Ordinal)), Is.False);
    }
}
