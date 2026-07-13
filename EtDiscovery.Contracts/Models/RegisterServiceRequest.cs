namespace EtDiscovery.Core.Models;

/// <summary>
/// Registration payload accepted by the public discovery API.
/// </summary>
public sealed class RegisterServiceRequest
{
    public RegisterServiceRequest(ServiceDefinition definition, ServiceInstance instance)
    {
        Definition = definition;
        Instance = instance;
    }

    public ServiceDefinition Definition { get; }

    public ServiceInstance Instance { get; }

    public HealthCheckDefinition? HealthCheck { get; set; }
}
