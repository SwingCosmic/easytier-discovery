namespace EtDiscovery.Core.Models;

/// <summary>
/// Captures caller-side hints that may influence resolution or selection.
/// </summary>
public sealed class CallContext
{
    /// <summary>
    /// Roles currently held by the caller node.
    /// </summary>
    public IReadOnlyList<NodeRole> CallerRoles { get; set; } = [NodeRole.Client];

    /// <summary>
    /// Optional logical region for locality decisions.
    /// </summary>
    public string? Region { get; set; }

    public string? Zone { get; set; }

    public string? NetworkPreference { get; set; }

    public string? ProtocolRequirement { get; set; }

    public TimeSpan? TimeoutBudget { get; set; }

    public string? CallerNodeId { get; set; }

    public string? CallerAddress { get; set; }

    public NetworkType NetworkType { get; set; } = NetworkType.Unknown;

    public bool Foreground { get; set; } = true;

    public bool BackgroundRestricted { get; set; }

    public bool MobileTun { get; set; }

    public bool Roaming { get; set; }

    public int? Battery { get; set; }
}
