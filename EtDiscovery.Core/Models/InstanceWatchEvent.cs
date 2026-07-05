namespace EtDiscovery.Core.Models;

/// <summary>
/// Represents one watch payload emitted by the discovery API.
/// </summary>
public sealed class InstanceWatchEvent
{
    public InstanceWatchEvent(WatchEventType eventType, ServiceQuery query, IReadOnlyList<ServiceInstance> instances, DateTimeOffset occurredAt)
    {
        EventType = eventType;
        Query = query;
        Instances = instances;
        OccurredAt = occurredAt;
    }

    public WatchEventType EventType { get; }

    public ServiceQuery Query { get; }

    public IReadOnlyList<ServiceInstance> Instances { get; }

    public DateTimeOffset OccurredAt { get; }
}
