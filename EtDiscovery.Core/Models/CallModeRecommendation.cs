namespace EtDiscovery.Core.Models;

/// <summary>
/// Contains a lightweight recommendation for how to reach one instance.
/// </summary>
public sealed class CallModeRecommendation
{
    public CallModeRecommendation(string instanceId)
    {
        InstanceId = instanceId;
    }

    /// <summary>
    /// Identifier of the target instance.
    /// </summary>
    public string InstanceId { get; }

    public CallMode RecommendedCallMode { get; set; } = CallMode.Unknown;

    public EndpointDescriptor? RecommendedEndpoint { get; set; }

    public string? Reason { get; set; }
}
