namespace EtDiscovery.Core.Models;

/// <summary>
/// Mutually exclusive service publication capabilities for the local node.
/// </summary>
public enum ServicePublicationCapability
{
    None = 0,

    PublishOwnedServices = 1,
}
