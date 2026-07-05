using EtDiscovery.Web.Services;

namespace EtDiscovery.Web.Endpoints;

public static class EndpointMappingExtensions
{
    public static IEndpointRouteBuilder MapEtDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", (
            EtDiscoveryWebOptions options,
            EtDiscoveryProcessManager processManager,
            DiscoveryCatalogService catalogService,
            EasyTierObservationService observationService) =>
        {
            var lastObservation = observationService.GetLastSnapshot();
            return Results.Ok(new
            {
                roles = options.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
                networkName = options.NetworkName,
                virtualNetworkCidr = options.VirtualNetworkCidr.ToString(),
                configuredVirtualIp = options.ConfiguredVirtualIp,
                dhcpEnabled = options.ShouldEnableDhcp,
                requiresTunDevice = options.RequiresTunDevice,
                requiresWindowsElevationForEasyTier = options.RequiresWindowsElevationForEasyTier,
                processElevated = EtDiscoveryWebOptions.IsCurrentProcessElevated(),
                privilegeChecklist = options.GetPrivilegeChecklist(),
                observedLocalVirtualIp = lastObservation?.LocalNode.VirtualIp,
                observedLocalNodeId = lastObservation?.LocalNode.NodeId,
                easyTier = processManager.GetStatus(),
                lastObservationAt = lastObservation?.ObservedAt,
                lastRefreshAt = catalogService.GetLastRefreshAt(),
            });
        });

        endpoints.MapGet("/easytier/peers", async (
            EasyTierObservationService observationService,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await observationService.GetCurrentSnapshotAsync(cancellationToken);
            var peers = snapshot.Peers.Select(peer => new
            {
                nodeId = peer.NodeId,
                peerId = peer.PeerId,
                hostname = peer.Hostname,
                networkName = peer.NetworkName,
                virtualIp = peer.VirtualIp,
                sameNetwork = peer.SameNetwork,
                inVirtualNetworkCidr = peer.InVirtualNetworkCidr,
                eligibleForDiscovery = peer.EligibleForDiscovery,
                isLocal = peer.IsLocal,
                foreignNetworkName = peer.ForeignNetworkName,
                cost = peer.Cost,
            });

            return Results.Ok(new
            {
                observedAt = snapshot.ObservedAt,
                peers,
            });
        });

        endpoints.MapGet("/test/ping", (EtDiscoveryWebOptions options) =>
        {
            if (!options.IsRegistry && !options.IsWorker)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return Results.Ok(new
            {
                roles = options.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
                serviceName = options.WorkerServiceName ?? options.RegistryWorkerServiceName,
                servicePort = options.WorkerServicePort ?? options.RegistryWorkerServicePort,
                networkName = options.NetworkName,
                timestamp = DateTimeOffset.UtcNow,
            });
        });

        endpoints.MapGet("/discovery/services", (EtDiscoveryWebOptions options, DiscoveryCatalogService catalogService) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var services = catalogService.ResolveServices()
                .Select(instance => new
                {
                    instanceId = instance.InstanceId,
                    nodeId = instance.NodeId,
                    serviceName = instance.ServiceKey.ServiceName,
                    address = instance.Address,
                    port = instance.Port,
                    virtualIp = instance.VirtualIp,
                });

            return Results.Ok(services);
        });

        endpoints.MapGet("/discovery/select", (string serviceName, EtDiscoveryWebOptions options, DiscoveryCatalogService catalogService) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var selected = catalogService.SelectOne(serviceName);
            if (selected?.RecommendedEndpoint is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                serviceName = selected.ServiceName,
                instanceId = selected.InstanceId,
                nodeId = selected.NodeId,
                address = selected.RecommendedEndpoint.Address,
                port = selected.RecommendedEndpoint.Port,
                url = $"http://{selected.RecommendedEndpoint.Address}:{selected.RecommendedEndpoint.Port}/test/ping",
            });
        });

        return endpoints;
    }
}
