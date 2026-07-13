using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EtDiscovery.Sdk;

/// <summary>
/// DI registration for business hosts (AddEtDiscovery).
/// </summary>
public static class EtDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddEtDiscovery(
        this IServiceCollection services,
        Action<EtDiscoveryClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<EtDiscoveryClientOptions>()
            .Configure(configure)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.RuntimeEndpoint),
                "EtDiscovery:RuntimeEndpoint is required.")
            .ValidateOnStart();

        return AddEtDiscoveryCore(services);
    }

    public static IServiceCollection AddEtDiscovery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EtDiscoveryClientOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.RuntimeEndpoint),
                "EtDiscovery:RuntimeEndpoint is required.")
            .ValidateOnStart();

        return AddEtDiscoveryCore(services);
    }

    private static IServiceCollection AddEtDiscoveryCore(IServiceCollection services)
    {
        services.AddHttpClient(EtDiscoveryClientOptions.HttpClientName)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<EtDiscoveryClientOptions>>().Value;
                client.BaseAddress = new Uri(options.RuntimeEndpoint.TrimEnd('/') + "/", UriKind.Absolute);
                client.Timeout = options.HttpTimeout;
            });

        services.TryAddSingleton<IEtDiscoveryClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient(EtDiscoveryClientOptions.HttpClientName);
            var options = sp.GetRequiredService<IOptions<EtDiscoveryClientOptions>>();
            return new EtDiscoveryHttpClient(http, options);
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, EtDiscoveryHeartbeatHostedService>());

        return services;
    }
}
