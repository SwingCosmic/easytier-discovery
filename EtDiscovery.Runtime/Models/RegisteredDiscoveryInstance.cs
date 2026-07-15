using EtDiscovery.Core.Models;

namespace EtDiscovery.Runtime.Models;

public sealed class RegisteredDiscoveryInstance
{
    public required string InstanceId { get; init; }

    public required ServiceDefinition Definition { get; init; }

    public required ServiceInstance Instance { get; init; }

    public HealthCheckDefinition? HealthCheck { get; init; }

    public required DateTimeOffset RegisteredAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
