namespace EtDiscovery.Core.Models;

/// <summary>
/// Client feedback payload for one completed call attempt.
/// </summary>
public sealed class ReportCallResultRequest
{
    public ReportCallResultRequest(string selectedInstanceId, bool success)
    {
        SelectedInstanceId = selectedInstanceId;
        Success = success;
    }

    public string SelectedInstanceId { get; }

    public bool Success { get; }

    public TimeSpan? Latency { get; set; }

    public CallErrorType ErrorType { get; set; } = CallErrorType.None;

    public string? Message { get; set; }
}
