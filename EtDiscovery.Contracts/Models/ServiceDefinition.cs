namespace EtDiscovery.Core.Models;

/// <summary>
/// Describes one logical service and its static registration metadata.
/// </summary>
public sealed class ServiceDefinition
{
    public ServiceDefinition(string @namespace, string serviceName, string protocol)
    {
        Namespace = @namespace;
        ServiceName = serviceName;
        Protocol = protocol;
    }

    public string Namespace { get; }

    public string ServiceName { get; }

    public string Protocol { get; }

    public string? Version { get; set; }

    public string? Group { get; set; }

    /// <summary>
    /// Opaque metadata reserved for routing and filtering.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string? RoutingPolicy { get; set; }

    public string? OwnerNodeId { get; set; }

    public long? ConfigEpoch { get; set; }

    public string? AclPolicyRef { get; set; }

    public ServiceKey ToServiceKey() => new(Namespace, ServiceName, Protocol, Version, Group);
}
