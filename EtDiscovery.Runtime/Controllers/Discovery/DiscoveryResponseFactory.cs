using EtDiscovery.Runtime.Models;

namespace EtDiscovery.Runtime.Controllers.Discovery;

internal static class DiscoveryResponseFactory
{
    public static object ToInstancePayload(RegisteredDiscoveryInstance record)
    {
        return new
        {
            instanceId = record.InstanceId,
            service = new
            {
                @namespace = record.Definition.Namespace,
                serviceName = record.Definition.ServiceName,
                protocol = record.Definition.Protocol,
                version = record.Definition.Version,
                group = record.Definition.Group,
                tags = record.Definition.Tags,
            },
            instance = new
            {
                nodeId = record.Instance.NodeId,
                address = record.Instance.Address,
                port = record.Instance.Port,
                virtualIp = record.Instance.VirtualIp,
                protocol = record.Instance.Protocol,
                weight = record.Instance.Weight,
                status = record.Instance.Status.ToString(),
                healthState = record.Instance.HealthState.ToString(),
                endpoints = record.Instance.Endpoints.Select(endpoint => new
                {
                    endpoint.Address,
                    endpoint.Port,
                    endpoint.Protocol,
                    callMode = endpoint.CallMode.ToString(),
                    endpoint.IsRecommended,
                }),
            },
            registeredAt = record.RegisteredAt,
            updatedAt = record.UpdatedAt,
        };
    }
}
