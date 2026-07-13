namespace EtDiscovery.Core.Models;

/// <summary>
/// High-level error classification for reported call failures.
/// </summary>
public enum CallErrorType
{
    None = 0,
    Timeout = 1,
    ConnectionFailed = 2,
    Refused = 3,
    Canceled = 4,
    Unknown = 255,
}
