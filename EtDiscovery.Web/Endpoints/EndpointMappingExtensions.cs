using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;
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
            EasyTierObservationService observationService,
            RegistryLocator registryLocator) =>
        {
            var lastObservation = observationService.GetLastSnapshot();
            var selectedRegistry = registryLocator.GetLastResolved();
            var (appId, flags) = options.GetAdvertisedNodeTypeMetadata();
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
                advertisedNodeTypeAppId = appId,
                advertisedNodeTypeFlags = flags,
                selectedRegistry = selectedRegistry is null
                    ? null
                    : new
                    {
                        address = selectedRegistry.Address,
                        source = selectedRegistry.Source,
                        nodeId = selectedRegistry.NodeId,
                        virtualIp = selectedRegistry.VirtualIp,
                    },
                easyTier = processManager.GetStatus(),
                publishedServices = options.Services.Select(service => new
                {
                    service.ServiceName,
                    service.Port,
                    service.Protocol,
                    service.InstanceId,
                }),
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
                nodeTypeAppId = peer.NodeTypeAppId,
                nodeTypeFlags = peer.NodeTypeFlags,
                roles = peer.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
                isRegistryCandidate = peer.IsRegistryCandidate,
            });

            return Results.Ok(new
            {
                observedAt = snapshot.ObservedAt,
                localNode = new
                {
                    nodeId = snapshot.LocalNode.NodeId,
                    virtualIp = snapshot.LocalNode.VirtualIp,
                    nodeTypeAppId = snapshot.LocalNode.NodeTypeAppId,
                    nodeTypeFlags = snapshot.LocalNode.NodeTypeFlags,
                    roles = snapshot.LocalNode.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
                },
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
                services = options.Services.Select(service => new
                {
                    service.ServiceName,
                    service.Port,
                    service.Protocol,
                    service.InstanceId,
                }),
                networkName = options.NetworkName,
                timestamp = DateTimeOffset.UtcNow,
            });
        });

        endpoints.MapGet("/discovery/registry", (
            EtDiscoveryWebOptions options,
            EasyTierObservationService observationService) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var snapshot = observationService.GetLastSnapshot();
            var virtualIp = snapshot?.LocalNode.VirtualIp ?? options.ConfiguredVirtualIp;
            var nodeId = snapshot?.LocalNode.NodeId ?? "local-node";
            var httpEndpoint = string.IsNullOrWhiteSpace(virtualIp)
                ? options.ListenUrl
                : $"http://{virtualIp}:{options.ListenPort}";

            return Results.Ok(new RegistryMetadataResponse
            {
                NetworkName = options.NetworkName,
                NodeId = nodeId,
                VirtualIp = virtualIp,
                Roles = options.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
                Endpoints = new RegistryEndpoints
                {
                    Http = httpEndpoint,
                },
                Capabilities = new RegistryCapabilities
                {
                    ServiceRegistration = true,
                    ServiceResolve = true,
                },
            });
        });

        endpoints.MapPost("/discovery/instances", (
            RegisterServiceRequest request,
            EtDiscoveryWebOptions options,
            DiscoveryInstanceRegistry registry) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var record = registry.Upsert(request);
            return Results.Ok(ToInstancePayload(record));
        });

        endpoints.MapDelete("/discovery/instances/{instanceId}", (
            string instanceId,
            EtDiscoveryWebOptions options,
            DiscoveryInstanceRegistry registry) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return registry.Deregister(instanceId)
                ? Results.NoContent()
                : Results.NotFound();
        });

        endpoints.MapGet("/discovery/instances/{instanceId}", (
            string instanceId,
            EtDiscoveryWebOptions options,
            DiscoveryInstanceRegistry registry) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var record = registry.Get(instanceId);
            return record is null
                ? Results.NotFound()
                : Results.Ok(ToInstancePayload(record));
        });

        endpoints.MapGet("/discovery/services", (
            string? serviceName,
            EtDiscoveryWebOptions options,
            DiscoveryCatalogService catalogService) =>
        {
            if (!options.IsRegistry)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var services = catalogService.ResolveServices(serviceName)
                .Select(instance => new
                {
                    instanceId = instance.InstanceId,
                    nodeId = instance.NodeId,
                    serviceName = instance.ServiceKey.ServiceName,
                    protocol = instance.Protocol,
                    address = instance.Address,
                    port = instance.Port,
                    virtualIp = instance.VirtualIp,
                    version = instance.Version,
                    group = instance.Group,
                    tags = instance.Tags,
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

        MapPlaceholder(endpoints.MapPut("/discovery/instances/{instanceId}/lease", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapPut("/discovery/instances/{instanceId}/health", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapPut("/discovery/instances/{instanceId}/status", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapDelete("/discovery/instances/{instanceId}/status", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapPut("/discovery/instances/{instanceId}/metadata", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapGet("/discovery/instances", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapGet("/discovery/nodes/{nodeId}/instances", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapPut("/discovery/nodes/{nodeId}/status", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));
        MapPlaceholder(endpoints.MapDelete("/discovery/nodes/{nodeId}/status", () => Results.StatusCode(StatusCodes.Status501NotImplemented)));

        return endpoints;
    }

    private static void MapPlaceholder(RouteHandlerBuilder builder)
    {
        builder.WithDisplayName("placeholder-not-implemented");
    }

    private static object ToInstancePayload(RegisteredDiscoveryInstance record)
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
