namespace EtDiscovery.Core.Models;

/// <summary>
/// Query object used by public discovery APIs.
/// </summary>
public sealed class ServiceQuery
{
    public ServiceQuery(string @namespace, string serviceName, string protocol)
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

    public IReadOnlyDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public ServiceKey ToServiceKey() => new(Namespace, ServiceName, Protocol, Version, Group);
}
