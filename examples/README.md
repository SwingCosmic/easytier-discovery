# EtDiscovery examples

Minimal ASP.NET apps that wire the thin SDK (`AddEtDiscovery` / `UseEtDiscovery`).

- **ServiceA** — `service-a` on port 9001  
- **ServiceB** — `service-b` on port 9002  

They do **not** implement cross-service business calls yet.

## Target topology (contract)

Per [application integration](../docs/service-registry-application-layer.md):

1. **EasyTier** up (external or orchestrated).  
2. **Independent Runtime** (`EtDiscovery.Runtime`) with `roles=registry`, typically `mode=embedded`, exposing `/discovery/*`.  
3. **ServiceA / ServiceB** hold **Sdk only** (worker/client) — **ActiveRenewal** is periodic `POST /discovery/instances` upsert to the control plane (register + renew are one call).  
4. Registry **cannot** live inside a normal app process (v1). A process that is both registry and worker is a **quasi-monolith**: Runtime + business + Sdk registering to itself.

## Current code status (outdated skeleton)

Checked-in Sdk still targets **`/runtime/v1/*`** (aborted). Migrate to control-plane upsert. Progress: [plan](../docs/service-registry-plan.md).

## Run (build / DI smoke)

```bash
dotnet run --project examples/EtDiscovery.Examples.ServiceA
dotnet run --project examples/EtDiscovery.Examples.ServiceB
```

Probe DI wiring:

```http
GET http://127.0.0.1:9001/health
GET http://127.0.0.1:9001/discovery/self
GET http://127.0.0.1:9001/discovery/client
```

Without a reachable control plane (and until Sdk is migrated), auto-register/heartbeat logs warnings and fails softly.
