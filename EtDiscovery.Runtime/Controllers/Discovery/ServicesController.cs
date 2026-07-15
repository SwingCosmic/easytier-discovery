using EtDiscovery.Runtime.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Runtime.Controllers.Discovery;

[ApiController]
public sealed class ServicesController : ControllerBase
{
    [HttpGet("/discovery/services")]
    public IActionResult Resolve(
        [FromQuery] string? serviceName,
        [FromServices] EtDiscoveryRuntimeOptions options,
        [FromServices] DiscoveryCatalogService catalogService)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
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

        return Ok(services);
    }
}
