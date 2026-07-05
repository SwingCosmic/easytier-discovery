namespace EtDiscovery.Core.Models;

/// <summary>
/// Rich result returned to callers after one selection operation.
/// </summary>
public sealed class SelectedInstance
{
    public SelectedInstance(string serviceName, string instanceId, string nodeId)
    {
        ServiceName = serviceName;
        InstanceId = instanceId;
        NodeId = nodeId;
    }

    public string ServiceName { get; }

    public string InstanceId { get; }

    public string NodeId { get; }

    public string? VirtualIp { get; set; }

    /// <summary>
    /// All currently known endpoints for this selection.
    /// </summary>
    public IReadOnlyList<EndpointDescriptor> Endpoints { get; set; } = [];

    public IReadOnlyList<string> Protocols { get; set; } = [];

    public EndpointDescriptor? RecommendedEndpoint { get; set; }

    public CallMode RecommendedCallMode { get; set; } = CallMode.Unknown;

    public HealthState HealthState { get; set; } = HealthState.Healthy;

    public NodeProfile? NodeProfile { get; set; }

    public LinkProfile? LinkProfile { get; set; }

    public IReadOnlyList<string> TopologyPath { get; set; } = [];

    public long? ConfigEpoch { get; set; }

    public long? AclEpoch { get; set; }

    public ConfigValidity ConfigValidity { get; set; } = ConfigValidity.Unknown;
}
