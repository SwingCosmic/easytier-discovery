namespace EtDiscovery.Core.Models;

/// <summary>
/// Placeholder description of one application-side health check.
/// </summary>
public sealed class HealthCheckDefinition
{
    public string? CheckType { get; set; }

    public string? Endpoint { get; set; }

    public TimeSpan? Interval { get; set; }

    public TimeSpan? Timeout { get; set; }
}
