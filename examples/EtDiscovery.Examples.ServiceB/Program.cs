using EtDiscovery.Sdk;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEtDiscovery(builder.Configuration.GetSection(EtDiscoveryClientOptions.DefaultSectionName));

var app = builder.Build();
app.UseEtDiscovery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/discovery/self", (IOptions<EtDiscoveryClientOptions> options) =>
{
    var o = options.Value;
    return Results.Ok(new
    {
        runtimeEndpoint = o.RuntimeEndpoint,
        serviceName = o.ServiceName,
        port = o.Port,
        protocol = o.Protocol,
        note = "DI integration only. Cross-service calls are intentionally omitted.",
    });
});

app.MapGet("/discovery/client", (IEtDiscoveryClient client) =>
    Results.Ok(new
    {
        clientType = client.GetType().FullName,
        registeredInstanceId = client.RegisteredInstanceId,
    }));

app.Run();
