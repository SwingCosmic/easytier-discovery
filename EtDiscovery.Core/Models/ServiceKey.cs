namespace EtDiscovery.Core.Models;

/// <summary>
/// Minimal immutable identity for one logical service stream.
/// </summary>
public sealed record ServiceKey(
    string Namespace,
    string ServiceName,
    string Protocol,
    string? Version = null,
    string? Group = null)
{
    public override string ToString() => $"{Namespace}/{ServiceName}:{Protocol}";
}
