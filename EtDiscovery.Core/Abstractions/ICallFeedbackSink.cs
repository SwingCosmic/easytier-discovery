using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Abstractions;

/// <summary>
/// Accepts caller-side feedback that can later influence routing or circuit state.
/// </summary>
public interface ICallFeedbackSink
{
    Task ReportCallResultAsync(ReportCallResultRequest request, CancellationToken cancellationToken = default);

    Task OpenCircuitAsync(string instanceId, string reason, CancellationToken cancellationToken = default);
}
