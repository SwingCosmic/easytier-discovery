namespace EtDiscovery.Core.Models;

/// <summary>
/// Describes how a watch payload should be interpreted.
/// </summary>
public enum WatchEventType
{
    Snapshot = 0,
    Updated = 1,
    Removed = 2,
}
