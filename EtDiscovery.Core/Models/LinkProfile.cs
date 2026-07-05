namespace EtDiscovery.Core.Models;

/// <summary>
/// Holds optional path metadata between two nodes.
/// </summary>
public sealed class LinkProfile
{
    public LinkProfile(string linkId, string sourceNodeId, string targetNodeId)
    {
        LinkId = linkId;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
    }

    public string LinkId { get; }

    public string SourceNodeId { get; }

    public string TargetNodeId { get; }

    public string? NextHop { get; set; }

    public int? HopCount { get; set; }

    public double? PathLatencyMs { get; set; }

    public double? LossRate { get; set; }

    public double? JitterMs { get; set; }

    public string? RoutePolicy { get; set; }
}
