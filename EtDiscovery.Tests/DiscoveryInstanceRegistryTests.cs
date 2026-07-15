using EtDiscovery.Core.Models;
using EtDiscovery.Runtime.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class DiscoveryInstanceRegistryTests
{
    [Test]
    public void SupportsMultipleInstancesForOneServiceAndDeregister()
    {
        var registry = new DiscoveryInstanceRegistry();
        var serviceKey = TestSamples.ServiceKey();

        registry.Upsert(new RegisterServiceRequest(
            new ServiceDefinition("default", "echo", "http"),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 8081)));
        registry.Upsert(new RegisterServiceRequest(
            new ServiceDefinition("default", "echo", "http"),
            new ServiceInstance("inst-b", serviceKey, "node-b", "10.0.0.3", 8081)));

        Assert.That(registry.List("echo").Select(item => item.InstanceId), Is.EqualTo(new[] { "inst-a", "inst-b" }));

        Assert.That(registry.Deregister("inst-a"), Is.True);
        Assert.That(registry.List("echo").Select(item => item.InstanceId), Is.EqualTo(new[] { "inst-b" }));
    }
}
