using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Web.Controllers.Discovery;

[ApiController]
public sealed class NodesController : ControllerBase
{
    [HttpGet("/discovery/nodes/{nodeId}/instances")]
    [HttpPut("/discovery/nodes/{nodeId}/status")]
    [HttpDelete("/discovery/nodes/{nodeId}/status")]
    public IActionResult NotImplementedPlaceholder()
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
