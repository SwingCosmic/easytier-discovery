using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EtDiscovery.Sdk;

/// <summary>
/// ASP.NET pipeline hook (UseEtDiscovery). Lifecycle is primarily HostedService-driven.
/// </summary>
public static class EtDiscoveryApplicationBuilderExtensions
{
    /// <summary>
    /// Validates that EtDiscovery was registered via <c>AddEtDiscovery</c>.
    /// Heartbeat/register run through <see cref="EtDiscoveryHeartbeatHostedService"/>.
    /// </summary>
    public static IApplicationBuilder UseEtDiscovery(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.ApplicationServices.GetRequiredService<IEtDiscoveryClient>();
        _ = app.ApplicationServices.GetRequiredService<IOptions<EtDiscoveryClientOptions>>().Value;

        return app;
    }
}
