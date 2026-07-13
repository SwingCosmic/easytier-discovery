using EtDiscovery.Core.Models;
using EtDiscovery.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Web.Controllers.Discovery;

[ApiController]
public sealed class InstancesController : ControllerBase
{
    [HttpPost("/discovery/instances")]
    public IActionResult Register(
        [FromBody] RegisterServiceRequest request,
        [FromServices] EtDiscoveryWebOptions options,
        [FromServices] DiscoveryInstanceRegistry registry)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var record = registry.Upsert(request);
        return Ok(DiscoveryResponseFactory.ToInstancePayload(record));
    }

    [HttpDelete("/discovery/instances/{instanceId}")]
    public IActionResult Deregister(
        string instanceId,
        [FromServices] EtDiscoveryWebOptions options,
        [FromServices] DiscoveryInstanceRegistry registry)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return registry.Deregister(instanceId)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("/discovery/instances/{instanceId}")]
    public IActionResult GetById(
        string instanceId,
        [FromServices] EtDiscoveryWebOptions options,
        [FromServices] DiscoveryInstanceRegistry registry)
    {
        if (!options.IsRegistry)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var record = registry.Get(instanceId);
        return record is null
            ? NotFound()
            : Ok(DiscoveryResponseFactory.ToInstancePayload(record));
    }

    [HttpGet("/discovery/instances")]
    [HttpPut("/discovery/instances/{instanceId}/lease")]
    [HttpPut("/discovery/instances/{instanceId}/health")]
    [HttpPut("/discovery/instances/{instanceId}/status")]
    [HttpDelete("/discovery/instances/{instanceId}/status")]
    [HttpPut("/discovery/instances/{instanceId}/metadata")]
    public IActionResult NotImplementedPlaceholder()
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
