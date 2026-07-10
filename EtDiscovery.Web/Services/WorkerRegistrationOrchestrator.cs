using System.Net.Http.Json;
using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class WorkerRegistrationOrchestrator
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly DiscoveryInstanceRegistry _instanceRegistry;
    private readonly RegistryLocator _registryLocator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkerRegistrationOrchestrator> _logger;
    private readonly HashSet<string> _registeredInstanceIds = [];

    public WorkerRegistrationOrchestrator(
        EtDiscoveryWebOptions options,
        DiscoveryInstanceRegistry instanceRegistry,
        RegistryLocator registryLocator,
        IHttpClientFactory httpClientFactory,
        ILogger<WorkerRegistrationOrchestrator> logger)
    {
        _options = options;
        _instanceRegistry = instanceRegistry;
        _registryLocator = registryLocator;
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
        var registry = await _registryLocator.ResolveAsync(snapshot, cancellationToken);
        if (!_options.IsRegistry && registry is null)
        {
            _logger.LogWarning(
                "Skipping worker registration because no registry candidate was resolved. reason=registry_candidate_missing");
            return;
        }

        foreach (var request in requests)
        {
            if (_options.IsRegistry)
            {
                _instanceRegistry.Upsert(request);
            }
            else
            {
                await RegisterRemoteAsync(request, registry!.Address, cancellationToken);
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

        var registry = _registryLocator.GetLastResolved();
        foreach (var instanceId in _registeredInstanceIds.ToArray())
        {
            if (_options.IsRegistry)
            {
                _instanceRegistry.Deregister(instanceId);
            }
            else if (registry is not null)
            {
                await DeregisterRemoteAsync(instanceId, registry.Address, cancellationToken);
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

    private async Task RegisterRemoteAsync(RegisterServiceRequest request, string registryAddress, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(nameof(WorkerRegistrationOrchestrator));
        var endpoint = new Uri(_options.GetRegistryBaseUri(registryAddress), "/discovery/instances");
        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeregisterRemoteAsync(string instanceId, string registryAddress, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(nameof(WorkerRegistrationOrchestrator));
        var endpoint = new Uri(_options.GetRegistryBaseUri(registryAddress), $"/discovery/instances/{Uri.EscapeDataString(instanceId)}");
        using var response = await client.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
