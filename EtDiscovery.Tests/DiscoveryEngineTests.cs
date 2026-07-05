using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;

namespace EtDiscovery.Tests;

[TestFixture]
public class DiscoveryEngineTests
{
    [Test]
    [Description("节点不可达后，对应实例应立即从服务目录中移除。")]
    public async Task NodeDownHidesInstances()
    {
        var serviceKey = TestSamples.ServiceKey();
        var query = TestSamples.Query();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That(
            (await runtime.ResolveAsync(query)).Select(instance => instance.InstanceId),
            Is.EqualTo(new[] { "inst-a" }));

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", false),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That(await runtime.ResolveAsync(query), Is.Empty);
    }

    [Test]
    [Description("节点恢复可达后，对应实例应重新出现在服务目录中。")]
    public async Task NodeRecoveryRestoresInstances()
    {
        var serviceKey = TestSamples.ServiceKey();
        var query = TestSamples.Query();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", false),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That(await runtime.ResolveAsync(query), Is.Empty);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That(
            (await runtime.ResolveAsync(query)).Select(instance => instance.InstanceId),
            Is.EqualTo(new[] { "inst-a" }));
    }

    [Test]
    [Description("只有一个可见实例时，重复选择应始终返回该实例。")]
    public async Task SingleCandidateAlwaysSelected()
    {
        var serviceKey = TestSamples.ServiceKey();
        var query = TestSamples.Query();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That((await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId, Is.EqualTo("inst-a"));
        Assert.That((await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId, Is.EqualTo("inst-a"));
    }

    [Test]
    [Description("多个可见实例时，选择结果应按稳定轮询顺序返回。")]
    public async Task RoundRobinCyclesCandidates()
    {
        var serviceKey = TestSamples.ServiceKey();
        var query = TestSamples.Query();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new NodeSnapshot("node-b", true),
            new NodeSnapshot("node-c", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000),
            new ServiceInstance("inst-b", serviceKey, "node-b", "10.0.0.3", 5000),
            new ServiceInstance("inst-c", serviceKey, "node-c", "10.0.0.4", 5000));
        await runtime.RefreshOnceAsync();

        var sequence = Enumerable.Range(0, 5)
            .Select(async _ => (await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId)
            .ToArray();

        var actual = await Task.WhenAll(sequence);

        Assert.That(actual, Is.EqualTo(new[] { "inst-a", "inst-b", "inst-c", "inst-a", "inst-b" }));
    }

    [Test]
    [Description("实例在刷新后被移除时，后续选择结果不应再命中该实例。")]
    public async Task RemovedInstanceIsSkipped()
    {
        var serviceKey = TestSamples.ServiceKey();
        var query = TestSamples.Query();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new NodeSnapshot("node-b", true),
            new NodeSnapshot("node-c", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000),
            new ServiceInstance("inst-b", serviceKey, "node-b", "10.0.0.3", 5000),
            new ServiceInstance("inst-c", serviceKey, "node-c", "10.0.0.4", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That((await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId, Is.EqualTo("inst-a"));
        Assert.That((await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId, Is.EqualTo("inst-b"));

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new NodeSnapshot("node-b", false),
            new NodeSnapshot("node-c", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000),
            new ServiceInstance("inst-b", serviceKey, "node-b", "10.0.0.3", 5000),
            new ServiceInstance("inst-c", serviceKey, "node-c", "10.0.0.4", 5000));
        await runtime.RefreshOnceAsync();

        var next = (await runtime.SelectOneHealthyInstanceAsync(query))?.InstanceId;
        Assert.That(next, Is.Not.EqualTo("inst-b"));
        Assert.That(next, Is.AnyOf("inst-a", "inst-c"));
    }

    [Test]
    [Description("查询不存在的服务时，应返回空列表且不选出任何实例。")]
    public async Task UnknownServiceReturnsEmpty()
    {
        var serviceKey = TestSamples.ServiceKey();
        var provider = new TestSnapshotProvider();
        var runtime = TestSamples.Runtime(provider);
        var unknownQuery = new ServiceQuery("default", "unknown", "http");

        provider.Snapshot = TestSamples.Snapshot(
            new NodeSnapshot("node-a", true),
            new ServiceInstance("inst-a", serviceKey, "node-a", "10.0.0.2", 5000));
        await runtime.RefreshOnceAsync();

        Assert.That(await runtime.ResolveAsync(unknownQuery), Is.Empty);
        Assert.That(await runtime.SelectOneHealthyInstanceAsync(unknownQuery), Is.Null);
    }
}
