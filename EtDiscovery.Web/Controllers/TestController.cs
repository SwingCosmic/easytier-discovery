using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Web.Controllers;

[ApiController]
public sealed class TestController : ControllerBase
{
    [HttpGet("/test/ping")]
    public IActionResult Ping([FromServices] EtDiscoveryWebOptions options)
    {
        if (!options.IsRegistry && !options.IsWorker)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(new
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
    }
}
