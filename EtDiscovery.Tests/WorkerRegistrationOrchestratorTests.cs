using EtDiscovery.Web;
using EtDiscovery.Web.Models;
using EtDiscovery.Web.Services;
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
        var orchestrator = new WorkerRegistrationOrchestrator(
            options,
            registry,
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

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHandler());
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
