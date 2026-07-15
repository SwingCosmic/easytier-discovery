using EtDiscovery.Runtime.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Runtime.Controllers.Discovery;

[ApiController]
public sealed class SelectController : ControllerBase
{
    [HttpGet("/discovery/select")]
    public IActionResult SelectOne(
        [FromQuery] string serviceName,
        [FromServices] EtDiscoveryRuntimeOptions options,
        [FromServices] DiscoveryCatalogService catalogService)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var selected = catalogService.SelectOne(serviceName);
        if (selected?.RecommendedEndpoint is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            serviceName = selected.ServiceName,
            instanceId = selected.InstanceId,
            nodeId = selected.NodeId,
            address = selected.RecommendedEndpoint.Address,
            port = selected.RecommendedEndpoint.Port,
            url = $"http://{selected.RecommendedEndpoint.Address}:{selected.RecommendedEndpoint.Port}/test/ping",
        });
    }
}
