using EtDiscovery.Runtime;
using EtDiscovery.Runtime.Models;
using EtDiscovery.Runtime.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EtDiscovery.Tests;

[TestFixture]
public class WorkerRegistrationOrchestratorTests
{
    [Test]
    public async Task RegistryWorkerRegistersItsOwnServicesIntoLocalRegistry()
    {
        var options = TestSamples.WebOptions(
            roles: [RoleName.Registry, RoleName.Worker],
            services:
            [
                new PublishedServiceOptions
                {
                    ServiceName = "echo",
                    Port = 8081,
                },
                new PublishedServiceOptions
                {
                    ServiceName = "echo",
                    Port = 8082,
                    InstanceId = "echo-secondary",
                },
            ]);

        var registry = new DiscoveryInstanceRegistry();
        var locator = new RegistryLocator(options, new StubHttpClientFactory(), NullLogger<RegistryLocator>.Instance);
        var orchestrator = new WorkerRegistrationOrchestrator(
            options,
            registry,
            locator,
            new StubHttpClientFactory(),
            NullLogger<WorkerRegistrationOrchestrator>.Instance);

        await orchestrator.SyncAsync(new EasyTierObservationSnapshot
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

        var instances = registry.List("echo");
        Assert.That(instances.Select(item => item.InstanceId), Is.EquivalentTo(new[] { "peer:1:echo:http:8081", "echo-secondary" }));
        Assert.That(instances.Select(item => item.Instance.Port), Is.EquivalentTo(new[] { 8081, 8082 }));
    }

    [Test]
    public async Task WorkerWithoutRegistryCandidateSkipsRemoteRegistration()
    {
        var options = TestSamples.WebOptions(
            roles: [RoleName.Worker],
            services:
            [
                new PublishedServiceOptions
                {
                    ServiceName = "echo",
                    Port = 8081,
                },
            ],
            registryCandidates: [],
            autoDiscoverFromRouteMetadata: true);

        var registry = new DiscoveryInstanceRegistry();
        var locator = new RegistryLocator(options, new StubHttpClientFactory(statusCode: System.Net.HttpStatusCode.NotFound), NullLogger<RegistryLocator>.Instance);
        var orchestrator = new WorkerRegistrationOrchestrator(
            options,
            registry,
            locator,
            new StubHttpClientFactory(),
            NullLogger<WorkerRegistrationOrchestrator>.Instance);

        await orchestrator.SyncAsync(new EasyTierObservationSnapshot
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
                    // plain peer without registry bit must not become registry
                    NodeTypeAppId = null,
                    NodeTypeFlags = 0,
                    Roles = [EtDiscovery.Core.Models.NodeRole.Worker],
                },
            ],
        }, CancellationToken.None);

        Assert.That(registry.List("echo"), Is.Empty);
        Assert.That(locator.GetLastResolved(), Is.Null);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly System.Net.HttpStatusCode _statusCode;

        public StubHttpClientFactory(System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            _statusCode = statusCode;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHandler(_statusCode));
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly System.Net.HttpStatusCode _statusCode;

        public StubHandler(System.Net.HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
