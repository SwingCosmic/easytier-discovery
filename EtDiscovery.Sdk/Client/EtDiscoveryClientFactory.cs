namespace EtDiscovery.Sdk;

/// <summary>
/// Non-DI factory for scripts and tests.
/// </summary>
public static class EtDiscoveryClientFactory
{
    public static IEtDiscoveryClient Create(Action<EtDiscoveryClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new EtDiscoveryClientOptions();
        configure(options);
        return Create(options);
    }

    public static IEtDiscoveryClient Create(EtDiscoveryClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var http = new HttpClient
        {
            BaseAddress = new Uri(options.RuntimeEndpoint.TrimEnd('/') + "/", UriKind.Absolute),
            Timeout = options.HttpTimeout,
        };
        return new EtDiscoveryHttpClient(http, options);
    }
}
