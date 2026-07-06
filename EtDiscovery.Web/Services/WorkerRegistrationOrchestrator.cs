using System.Net.Http.Json;
using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class WorkerRegistrationOrchestrator
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly DiscoveryInstanceRegistry _instanceRegistry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkerRegistrationOrchestrator> _logger;
    private readonly HashSet<string> _registeredInstanceIds = [];

    public WorkerRegistrationOrchestrator(
        EtDiscoveryWebOptions options,
        DiscoveryInstanceRegistry instanceRegistry,
        IHttpClientFactory httpClientFactory,
        ILogger<WorkerRegistrationOrchestrator> logger)
    {
        _options = options;
        _instanceRegistry = instanceRegistry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SyncAsync(EasyTierObservationSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (!_options.IsWorker || !_options.HasPublishedServices)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(snapshot.LocalNode.VirtualIp))
        {
            _logger.LogDebug("Skipping worker registration because local EasyTier virtual IP is not ready.");
            return;
        }

        var requests = BuildRequests(snapshot.LocalNode);
        var registryAddress = ResolveRegistryAddress(snapshot);
        foreach (var request in requests)
        {
            if (_options.IsRegistry)
            {
                _instanceRegistry.Upsert(request);
            }
            else
            {
                await RegisterRemoteAsync(request, registryAddress, cancellationToken);
            }

            _registeredInstanceIds.Add(request.Instance.InstanceId);
        }
    }

    public async Task DeregisterAllAsync(CancellationToken cancellationToken)
    {
        if (_registeredInstanceIds.Count == 0)
        {
            return;
        }

        foreach (var instanceId in _registeredInstanceIds.ToArray())
        {
            if (_options.IsRegistry)
            {
                _instanceRegistry.Deregister(instanceId);
            }
            else
            {
                await DeregisterRemoteAsync(instanceId, _options.RegistryPeer, cancellationToken);
            }
        }

        _registeredInstanceIds.Clear();
    }

    public IReadOnlyList<RegisterServiceRequest> BuildRequests(LocalNodeView localNode)
    {
        if (string.IsNullOrWhiteSpace(localNode.VirtualIp))
        {
            return [];
        }

        return _options.Services
            .Select(service => BuildRequest(localNode, service))
            .ToArray();
    }

    private RegisterServiceRequest BuildRequest(LocalNodeView localNode, PublishedServiceOptions service)
    {
        var definition = service.CreateDefinition();
        definition.OwnerNodeId = localNode.NodeId;

        var mergedTags = service.MergeTags();
        var serviceKey = definition.ToServiceKey();
        var instanceId = service.ResolveInstanceId(localNode.NodeId);
        var instance = new ServiceInstance(
            instanceId,
            serviceKey,
            localNode.NodeId,
            localNode.VirtualIp!,
            service.Port)
        {
            Protocol = service.Protocol,
            VirtualIp = localNode.VirtualIp,
            Version = service.Version,
            Group = service.Group,
            Weight = service.Weight,
            Tags = mergedTags,
            OwnerNodeId = localNode.NodeId,
            Endpoints =
            [
                new EndpointDescriptor(localNode.VirtualIp!, service.Port)
                {
                    Protocol = service.Protocol,
                    CallMode = CallMode.Direct,
                    IsRecommended = true,
                },
            ],
        };

        return new RegisterServiceRequest(definition, instance);
    }

    private string? ResolveRegistryAddress(EasyTierObservationSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(_options.RegistryPeer))
        {
            return _options.RegistryPeer;
        }

        return snapshot.Peers
            .FirstOrDefault(peer => !peer.IsLocal && peer.EligibleForDiscovery && !string.IsNullOrWhiteSpace(peer.VirtualIp))
            ?.VirtualIp;
    }

    private async Task RegisterRemoteAsync(RegisterServiceRequest request, string? registryAddress, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(nameof(WorkerRegistrationOrchestrator));
        var endpoint = new Uri(_options.GetRegistryBaseUri(registryAddress), "/discovery/instances");
        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeregisterRemoteAsync(string instanceId, string? registryAddress, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(nameof(WorkerRegistrationOrchestrator));
        var endpoint = new Uri(_options.GetRegistryBaseUri(registryAddress), $"/discovery/instances/{Uri.EscapeDataString(instanceId)}");
        using var response = await client.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
