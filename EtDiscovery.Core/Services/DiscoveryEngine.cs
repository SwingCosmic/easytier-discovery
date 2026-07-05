using EtDiscovery.Core.Abstractions;
using EtDiscovery.Core.Models;

namespace EtDiscovery.Core.Services;

/// <summary>
/// Pure in-memory kernel for snapshot application, catalog lookup and selection.
/// </summary>
public sealed class DiscoveryEngine
{
    private readonly INodeProcessingPolicy _catalogBuilder;
    private readonly IServiceSelectionPolicy _selectionPolicy;
    private readonly object _sync = new();
    private ServiceCatalog _catalog = new(new Dictionary<ServiceKey, IReadOnlyList<ServiceInstance>>());

    public DiscoveryEngine(
        INodeProcessingPolicy catalogBuilder,
        IServiceSelectionPolicy selectionPolicy)
    {
        _catalogBuilder = catalogBuilder;
        _selectionPolicy = selectionPolicy;
    }

    /// <summary>
    /// Applies one fresh snapshot to rebuild the current catalog view.
    /// </summary>
    public void ApplySnapshot(DiscoverySnapshot snapshot)
    {
        lock (_sync)
        {
            _catalog = _catalogBuilder.BuildCatalog(snapshot);
        }
    }

    /// <summary>
    /// Resolves service instances using the already-built catalog.
    /// </summary>
    public IReadOnlyList<ServiceInstance> Resolve(ServiceQuery query)
    {
        lock (_sync)
        {
            return _catalog.GetInstances(query);
        }
    }

    /// <summary>
    /// Selects one instance using the configured selection policy.
    /// </summary>
    public SelectedInstance? SelectOne(ServiceQuery query)
    {
        lock (_sync)
        {
            var candidates = _catalog.GetInstances(query);
            var selected = _selectionPolicy.Select(query.ToServiceKey(), candidates);
            return selected is null ? null : ToSelectedInstance(selected);
        }
    }

    /// <summary>
    /// Selects a limited ordered set of instances from the current catalog.
    /// </summary>
    public IReadOnlyList<SelectedInstance> SelectMany(ServiceQuery query, int limit)
    {
        lock (_sync)
        {
            return _catalog.GetInstances(query)
                .Take(limit)
                .Select(ToSelectedInstance)
                .ToArray();
        }
    }

    /// <summary>
    /// Reads one cached node profile from the current catalog.
    /// </summary>
    public NodeProfile? GetNodeProfile(string nodeId)
    {
        lock (_sync)
        {
            return _catalog.GetNodeProfile(nodeId);
        }
    }

    /// <summary>
    /// Builds a lightweight call-mode recommendation from the current catalog.
    /// </summary>
    public CallModeRecommendation? RecommendCallMode(string instanceId)
    {
        lock (_sync)
        {
            var instance = _catalog.ServiceKeys
                .SelectMany(key => _catalog.GetInstances(key))
                .FirstOrDefault(item => item.InstanceId == instanceId);

            if (instance is null)
            {
                return null;
            }

            var endpoint = instance.Endpoints.FirstOrDefault() ?? new EndpointDescriptor(instance.Address, instance.Port)
            {
                Protocol = instance.Protocol,
                CallMode = CallMode.Direct,
                IsRecommended = true,
            };

            return new CallModeRecommendation(instanceId)
            {
                RecommendedCallMode = endpoint.CallMode,
                RecommendedEndpoint = endpoint,
                Reason = "placeholder",
            };
        }
    }

    private SelectedInstance ToSelectedInstance(ServiceInstance instance)
    {
        var endpoints = instance.Endpoints?.Count > 0
            ? instance.Endpoints
            : [new EndpointDescriptor(instance.Address, instance.Port)
            {
                Protocol = instance.Protocol,
                CallMode = CallMode.Direct,
                IsRecommended = true,
            }];

        return new SelectedInstance(instance.ServiceKey.ServiceName, instance.InstanceId, instance.NodeId)
        {
            VirtualIp = instance.VirtualIp,
            Endpoints = endpoints,
            Protocols = [instance.Protocol],
            RecommendedEndpoint = endpoints.FirstOrDefault(endpoint => endpoint.IsRecommended) ?? endpoints[0],
            RecommendedCallMode = endpoints.FirstOrDefault(endpoint => endpoint.IsRecommended)?.CallMode ?? CallMode.Direct,
            HealthState = instance.HealthState,
            NodeProfile = _catalog.GetNodeProfile(instance.NodeId),
            ConfigEpoch = instance.ConfigEpoch,
            AclEpoch = instance.AclEpoch,
            ConfigValidity = instance.ConfigValidity,
        };
    }
}
