using EtDiscovery.Runtime.Models;
using EtDiscovery.Runtime.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Runtime.Controllers.Discovery;

[ApiController]
public sealed class RegistryController : ControllerBase
{
    [HttpGet("/discovery/registry")]
    public IActionResult GetRegistryMetadata(
        [FromServices] EtDiscoveryRuntimeOptions options,
        [FromServices] EasyTierObservationService observationService)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var snapshot = observationService.GetLastSnapshot();
        var virtualIp = snapshot?.LocalNode.VirtualIp ?? options.ConfiguredVirtualIp;
        var nodeId = snapshot?.LocalNode.NodeId ?? "local-node";
        var httpEndpoint = string.IsNullOrWhiteSpace(virtualIp)
            ? options.ListenUrl
            : $"http://{virtualIp}:{options.ListenPort}";

        return Ok(new RegistryMetadataResponse
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
    }
}
