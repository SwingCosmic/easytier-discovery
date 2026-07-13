# EtDiscovery examples

Minimal ASP.NET apps that **only wire the thin SDK** (`AddEtDiscovery` / `UseEtDiscovery`).

- **ServiceA** — `service-a` on port 9001  
- **ServiceB** — `service-b` on port 9002  

They do **not** implement cross-service business calls. That comes after local `/runtime/v1` is available.

## Prerequisites

1. Local EtDiscovery runtime listening at `http://127.0.0.1:8081` with `worker,client` (and EasyTier per mode rules).  
2. Runtime exposes `/runtime/v1/*` (not required just to **build** these projects).

## Run (structure only)

```bash
dotnet run --project examples/EtDiscovery.Examples.ServiceA
dotnet run --project examples/EtDiscovery.Examples.ServiceB
```

Probe integration:

```text
GET http://127.0.0.1:9001/health
GET http://127.0.0.1:9001/discovery/self
GET http://127.0.0.1:9001/discovery/client
```

Without a runtime, auto-register/heartbeat will log warnings and fail softly.
