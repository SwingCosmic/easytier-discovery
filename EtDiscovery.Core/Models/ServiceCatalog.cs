namespace EtDiscovery.Core.Models;

/// <summary>
/// Queryable in-memory catalog built from the latest snapshot.
/// </summary>
public sealed class ServiceCatalog
{
    private readonly IReadOnlyDictionary<ServiceKey, IReadOnlyList<ServiceInstance>> _services;
    private readonly IReadOnlyDictionary<string, NodeProfile> _nodeProfiles;
    private readonly IReadOnlyDictionary<string, LinkProfile> _linkProfiles;

    public ServiceCatalog(
        IReadOnlyDictionary<ServiceKey, IReadOnlyList<ServiceInstance>> services,
        IReadOnlyDictionary<string, NodeProfile>? nodeProfiles = null,
        IReadOnlyDictionary<string, LinkProfile>? linkProfiles = null)
    {
        _services = services;
        _nodeProfiles = nodeProfiles ?? new Dictionary<string, NodeProfile>(StringComparer.Ordinal);
        _linkProfiles = linkProfiles ?? new Dictionary<string, LinkProfile>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Service identities currently available in the catalog.
    /// </summary>
    public IReadOnlyCollection<ServiceKey> ServiceKeys => _services.Keys.ToArray();

    public IReadOnlyList<ServiceInstance> GetInstances(ServiceKey serviceKey)
    {
        return _services.TryGetValue(serviceKey, out var instances) ? instances : [];
    }

    public IReadOnlyList<ServiceInstance> GetInstances(ServiceQuery query)
    {
        return GetInstances(query.ToServiceKey())
            .Where(instance =>
                (query.Version is null || instance.Version == query.Version) &&
                (query.Group is null || instance.Group == query.Group) &&
                (query.Tags.Count == 0 || query.Tags.All(tag => instance.Tags is not null && instance.Tags.TryGetValue(tag.Key, out var value) && value == tag.Value)))
            .ToArray();
    }

    public NodeProfile? GetNodeProfile(string nodeId)
    {
        return _nodeProfiles.TryGetValue(nodeId, out var profile) ? profile : null;
    }

    public LinkProfile? GetLinkProfile(string linkId)
    {
        return _linkProfiles.TryGetValue(linkId, out var profile) ? profile : null;
    }
}
