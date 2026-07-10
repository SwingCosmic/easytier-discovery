using System.Net;
using System.Text;
using System.Text.Json;
using EtDiscovery.Core.Models;
using EtDiscovery.Web;
using EtDiscovery.Web.Models;
using EtDiscovery.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EtDiscovery.Tests;

[TestFixture]
public class RegistryLocatorTests
{
    [Test]
    public async Task PrefersExplicitCandidateOverRouteMetadata()
    {
        var options = TestSamples.WebOptions(
            roles: [RoleName.Worker],
            registryCandidates: ["10.144.144.1"],
            autoDiscoverFromRouteMetadata: true);

        var locator = new RegistryLocator(options, new StubHttpClientFactory(), NullLogger<RegistryLocator>.Instance);
        var resolved = await locator.ResolveAsync(CreateSnapshotWithRegistryPeer(), CancellationToken.None);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Address, Is.EqualTo("10.144.144.1"));
        Assert.That(resolved.Source, Is.EqualTo("explicit"));
    }

    [Test]
    public async Task UsesRouteMetadataWhenExplicitCandidatesMissing()
    {
        var options = TestSamples.WebOptions(
            roles: [RoleName.Worker],
            registryCandidates: [],
            autoDiscoverFromRouteMetadata: true);

        var locator = new RegistryLocator(
            options,
            new StubHttpClientFactory(JsonSerializer.Serialize(new RegistryMetadataResponse
            {
                NetworkName = "demo-net",
                NodeId = "peer:9",
                VirtualIp = "10.144.144.9",
                Roles = ["registry"],
                Endpoints = new RegistryEndpoints { Http = "http://10.144.144.9:8080" },
                Capabilities = new RegistryCapabilities(),
            })),
            NullLogger<RegistryLocator>.Instance);

        var resolved = await locator.ResolveAsync(CreateSnapshotWithRegistryPeer(), CancellationToken.None);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Address, Is.EqualTo("10.144.144.9"));
        Assert.That(resolved.Source, Is.EqualTo("route_metadata"));
    }

    [Test]
    public async Task DoesNotSelectPlainPeerAsRegistry()
    {
        var options = TestSamples.WebOptions(
            roles: [RoleName.Worker],
            registryCandidates: [],
            autoDiscoverFromRouteMetadata: true);

        var locator = new RegistryLocator(options, new StubHttpClientFactory(), NullLogger<RegistryLocator>.Instance);
        var resolved = await locator.ResolveAsync(new EasyTierObservationSnapshot
        {
            ObservedAt = DateTimeOffset.UtcNow,
            LocalNode = new LocalNodeView
            {
                NodeId = "peer:2",
                NetworkName = "demo-net",
                VirtualIp = "10.144.144.2",
            },
            Peers =
            [
                new ObservedPeer
                {
                    NodeId = "peer:3",
                    NetworkName = "demo-net",
                    VirtualIp = "10.144.144.3",
                    EligibleForDiscovery = true,
                    SameNetwork = true,
                    InVirtualNetworkCidr = true,
                    Roles = [NodeRole.Worker],
                },
            ],
        }, CancellationToken.None);

        Assert.That(resolved, Is.Null);
    }

    [Test]
    public async Task LocalRegistryUsesListenUrl()
    {
        var options = TestSamples.WebOptions(roles: [RoleName.Registry]);
        var locator = new RegistryLocator(options, new StubHttpClientFactory(), NullLogger<RegistryLocator>.Instance);

        var resolved = await locator.ResolveAsync(new EasyTierObservationSnapshot
        {
            ObservedAt = DateTimeOffset.UtcNow,
            LocalNode = new LocalNodeView
            {
                NodeId = "peer:1",
                NetworkName = "demo-net",
                VirtualIp = "10.144.144.1",
            },
            Peers = [],
        }, CancellationToken.None);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Source, Is.EqualTo("local"));
        Assert.That(resolved.Address, Is.EqualTo(options.ListenUrl));
    }

    private static EasyTierObservationSnapshot CreateSnapshotWithRegistryPeer()
    {
        return new EasyTierObservationSnapshot
        {
            ObservedAt = DateTimeOffset.UtcNow,
            LocalNode = new LocalNodeView
            {
                NodeId = "peer:2",
                NetworkName = "demo-net",
                VirtualIp = "10.144.144.2",
            },
            Peers =
            [
                new ObservedPeer
                {
                    NodeId = "peer:9",
                    NetworkName = "demo-net",
                    VirtualIp = "10.144.144.9",
                    EligibleForDiscovery = true,
                    SameNetwork = true,
                    InVirtualNetworkCidr = true,
                    NodeTypeAppId = EtDiscoveryNodeTypeFlags.AppId,
                    NodeTypeFlags = EtDiscoveryNodeTypeFlags.Registry,
                    Roles = [NodeRole.Registry],
                },
            ],
        };
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string? _jsonBody;

        public StubHttpClientFactory(string? jsonBody = null)
        {
            _jsonBody = jsonBody;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHandler(_jsonBody));
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string? _jsonBody;

        public StubHandler(string? jsonBody)
        {
            _jsonBody = jsonBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_jsonBody is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jsonBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
