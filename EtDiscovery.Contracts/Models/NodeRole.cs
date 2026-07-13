namespace EtDiscovery.Core.Models;

/// <summary>
/// Describes the responsibility a node承担 within discovery.
/// </summary>
public enum NodeRole
{
    /// <summary>
    /// Role is not yet known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Stable registry or control-plane node.
    /// </summary>
    Registry = 1,

    /// <summary>
    /// Service provider or worker node.
    /// </summary>
    Worker = 2,

    /// <summary>
    /// Consumer-oriented client node.
    /// </summary>
    Client = 3,
}
