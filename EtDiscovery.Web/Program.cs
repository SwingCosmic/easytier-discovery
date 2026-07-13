using EtDiscovery.Core.Models;
using EtDiscovery.Core.Services;
using EtDiscovery.Web;
using EtDiscovery.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var options = EtDiscoveryWebBootstrap.LoadOptions(builder, args);
builder.WebHost.UseUrls(options.ListenUrl);

builder.Services.AddControllers();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton(new DiscoveryNodeContext(
    "local-node",
    options.Roles.Select(RoleNameMapper.ToNodeRole)));
builder.Services.AddSingleton(sp => new DiscoveryEngine(
    new ReachableNodeProcessingPolicy(),
    new RoundRobinServiceSelectionPolicy()));
builder.Services.AddSingleton<DiscoveryInstanceRegistry>();
builder.Services.AddSingleton<EasyTierConfigGenerator>();
builder.Services.AddSingleton<EtDiscoveryProcessManager>();
builder.Services.AddSingleton<EasyTierCliClient>();
builder.Services.AddSingleton<PeerObservationMapper>();
builder.Services.AddSingleton<EasyTierObservationService>();
builder.Services.AddSingleton<RegistryLocator>();
builder.Services.AddSingleton<RegistrySnapshotBuilder>();
builder.Services.AddSingleton<DiscoveryCatalogService>();
builder.Services.AddSingleton<WorkerRegistrationOrchestrator>();
builder.Services.AddHttpClient(nameof(WorkerRegistrationOrchestrator));
builder.Services.AddHttpClient(nameof(RegistryLocator));
builder.Services.AddHostedService(sp => sp.GetRequiredService<EtDiscoveryProcessManager>());
builder.Services.AddHostedService<EasyTierVirtualIpMonitor>();
builder.Services.AddHostedService<DiscoveryRefreshBackgroundService>();
builder.Services.AddHostedService<WorkerRegistrationBackgroundService>();

var app = builder.Build();
app.MapControllers();
app.Run();
