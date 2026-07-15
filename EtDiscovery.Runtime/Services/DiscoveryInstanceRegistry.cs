using System.Collections.Concurrent;
using EtDiscovery.Core.Models;
using EtDiscovery.Runtime.Models;

namespace EtDiscovery.Runtime.Services;

public sealed class DiscoveryInstanceRegistry
{
    private readonly ConcurrentDictionary<string, RegisteredDiscoveryInstance> _instances = new(StringComparer.Ordinal);

    public RegisteredDiscoveryInstance Upsert(RegisterServiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalized = Normalize(request);
        var now = DateTimeOffset.UtcNow;
        var record = _instances.AddOrUpdate(
            normalized.Instance.InstanceId,
            _ => new RegisteredDiscoveryInstance
            {
                InstanceId = normalized.Instance.InstanceId,
                Definition = normalized.Definition,
                Instance = normalized.Instance,
                HealthCheck = normalized.HealthCheck,
                RegisteredAt = now,
                UpdatedAt = now,
            },
            (_, existing) => new RegisteredDiscoveryInstance
            {
                InstanceId = normalized.Instance.InstanceId,
                Definition = normalized.Definition,
                Instance = normalized.Instance,
                HealthCheck = normalized.HealthCheck,
                RegisteredAt = existing.RegisteredAt,
                UpdatedAt = now,
            });

        return Clone(record);
    }

    public bool Deregister(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return _instances.TryRemove(instanceId, out _);
    }

    public RegisteredDiscoveryInstance? Get(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return _instances.TryGetValue(instanceId, out var record)
            ? Clone(record)
            : null;
    }

    public IReadOnlyList<RegisteredDiscoveryInstance> List(string? serviceName = null, string? nodeId = null)
    {
        return _instances.Values
            .Where(record =>
                (serviceName is null || string.Equals(record.Definition.ServiceName, serviceName, StringComparison.Ordinal)) &&
                (nodeId is null || string.Equals(record.Instance.NodeId, nodeId, StringComparison.Ordinal)))
            .OrderBy(record => record.Definition.ServiceName, StringComparer.Ordinal)
            .ThenBy(record => record.InstanceId, StringComparer.Ordinal)
            .Select(Clone)
            .ToArray();
    }

    public IReadOnlyList<ServiceInstance> GetRegisteredInstances()
    {
        return _instances.Values
            .OrderBy(record => record.Definition.ServiceName, StringComparer.Ordinal)
            .ThenBy(record => record.InstanceId, StringComparer.Ordinal)
            .Select(record => Clone(record.Instance))
            .ToArray();
    }

    private static RegisterServiceRequest Normalize(RegisterServiceRequest request)
    {
        var definition = new ServiceDefinition(
            request.Definition.Namespace,
            request.Definition.ServiceName,
            request.Definition.Protocol)
        {
            Version = request.Definition.Version,
            Group = request.Definition.Group,
            Tags = CloneDictionary(request.Definition.Tags),
            RoutingPolicy = request.Definition.RoutingPolicy,
            OwnerNodeId = request.Definition.OwnerNodeId,
            ConfigEpoch = request.Definition.ConfigEpoch,
            AclPolicyRef = request.Definition.AclPolicyRef,
        };

        var serviceKey = definition.ToServiceKey();
        var instance = new ServiceInstance(
            request.Instance.InstanceId,
            serviceKey,
            request.Instance.NodeId,
            request.Instance.Address,
            request.Instance.Port)
        {
            Status = request.Instance.Status,
            Weight = request.Instance.Weight,
            Protocol = request.Instance.Protocol,
            VirtualIp = request.Instance.VirtualIp,
            Version = request.Instance.Version ?? definition.Version,
            Group = request.Instance.Group ?? definition.Group,
            Tags = CloneDictionary(request.Instance.Tags.Count == 0 ? definition.Tags : request.Instance.Tags),
            LeaseId = request.Instance.LeaseId,
            LeaseEpoch = request.Instance.LeaseEpoch,
            HealthState = request.Instance.HealthState,
            ExposeMode = request.Instance.ExposeMode,
            Endpoints = request.Instance.Endpoints.Select(Clone).ToArray(),
            OwnerNodeId = request.Instance.OwnerNodeId ?? request.Instance.NodeId,
            ConfigEpoch = request.Instance.ConfigEpoch,
            AclEpoch = request.Instance.AclEpoch,
            ConfigValidity = request.Instance.ConfigValidity,
        };

        return new RegisterServiceRequest(definition, instance)
        {
            HealthCheck = request.HealthCheck,
        };
    }

    private static RegisteredDiscoveryInstance Clone(RegisteredDiscoveryInstance record)
    {
        return new RegisteredDiscoveryInstance
        {
            InstanceId = record.InstanceId,
            Definition = new ServiceDefinition(
                record.Definition.Namespace,
                record.Definition.ServiceName,
                record.Definition.Protocol)
            {
                Version = record.Definition.Version,
                Group = record.Definition.Group,
                Tags = CloneDictionary(record.Definition.Tags),
                RoutingPolicy = record.Definition.RoutingPolicy,
                OwnerNodeId = record.Definition.OwnerNodeId,
                ConfigEpoch = record.Definition.ConfigEpoch,
                AclPolicyRef = record.Definition.AclPolicyRef,
            },
            Instance = Clone(record.Instance),
            HealthCheck = record.HealthCheck,
            RegisteredAt = record.RegisteredAt,
            UpdatedAt = record.UpdatedAt,
        };
    }

    private static ServiceInstance Clone(ServiceInstance instance)
    {
        return new ServiceInstance(
            instance.InstanceId,
            new ServiceKey(
                instance.ServiceKey.Namespace,
                instance.ServiceKey.ServiceName,
                instance.ServiceKey.Protocol,
                instance.ServiceKey.Version,
                instance.ServiceKey.Group),
            instance.NodeId,
            instance.Address,
            instance.Port)
        {
            Status = instance.Status,
            Weight = instance.Weight,
            Protocol = instance.Protocol,
            VirtualIp = instance.VirtualIp,
            Version = instance.Version,
            Group = instance.Group,
            Tags = CloneDictionary(instance.Tags),
            LeaseId = instance.LeaseId,
            LeaseEpoch = instance.LeaseEpoch,
            HealthState = instance.HealthState,
            ExposeMode = instance.ExposeMode,
            Endpoints = instance.Endpoints.Select(Clone).ToArray(),
            OwnerNodeId = instance.OwnerNodeId,
            ConfigEpoch = instance.ConfigEpoch,
            AclEpoch = instance.AclEpoch,
            ConfigValidity = instance.ConfigValidity,
        };
    }

    private static EndpointDescriptor Clone(EndpointDescriptor endpoint)
    {
        return new EndpointDescriptor(endpoint.Address, endpoint.Port)
        {
            Protocol = endpoint.Protocol,
            CallMode = endpoint.CallMode,
            IsRecommended = endpoint.IsRecommended,
        };
    }

    private static IReadOnlyDictionary<string, string> CloneDictionary(IReadOnlyDictionary<string, string> source)
    {
        return source.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }
}
