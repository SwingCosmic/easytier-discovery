using EtDiscovery.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Web.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get(
        [FromServices] EtDiscoveryWebOptions options,
        [FromServices] EtDiscoveryProcessManager processManager,
        [FromServices] DiscoveryCatalogService catalogService,
        [FromServices] EasyTierObservationService observationService,
        [FromServices] RegistryLocator registryLocator)
    {
        var lastObservation = observationService.GetLastSnapshot();
        var selectedRegistry = registryLocator.GetLastResolved();
        var (appId, flags) = options.GetAdvertisedNodeTypeMetadata();
        return Ok(new
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
    }
}
