namespace EtDiscovery.Core.Models;

/// <summary>
/// Represents one concrete callable instance under a logical service.
/// </summary>
public sealed class ServiceInstance
{
    public ServiceInstance(string instanceId, ServiceKey serviceKey, string nodeId, string address, int port)
    {
        InstanceId = instanceId;
        ServiceKey = serviceKey;
        NodeId = nodeId;
        Address = address;
        Port = port;
    }

    public string InstanceId { get; }

    public ServiceKey ServiceKey { get; }

    public string NodeId { get; }

    public string Address { get; set; }

    public int Port { get; set; }

    public ServiceInstanceStatus Status { get; set; } = ServiceInstanceStatus.Active;

    public int Weight { get; set; } = 1;

    public string Protocol { get; set; } = "http";

    public string? VirtualIp { get; set; }

    public string? Version { get; set; }

    public string? Group { get; set; }

    public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string? LeaseId { get; set; }

    public long? LeaseEpoch { get; set; }

    public HealthState HealthState { get; set; } = HealthState.Healthy;

    public ExposeMode ExposeMode { get; set; } = ExposeMode.Direct;

    public IReadOnlyList<EndpointDescriptor> Endpoints { get; set; } = [];

    public string? OwnerNodeId { get; set; }

    public long? ConfigEpoch { get; set; }

    public long? AclEpoch { get; set; }

    public ConfigValidity ConfigValidity { get; set; } = ConfigValidity.Unknown;
}
