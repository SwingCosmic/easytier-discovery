[简体中文](./README.md) | [English](./README_EN.md)

# EasyTier Discovery

EasyTier Discovery (abbreviated in code as `EtDiscovery`) is an application-layer service discovery and instance selection middleware built on top of EasyTier.

This repository is still in a very early prototype stage. The current work is focused on validating one core idea:

- keep EasyTier as the network substrate
- build a discovery and control plane above it
- expose a service-registry-like API that remains practical in weak-network, NAT, relay, and cross-region environments

## Goals

EtDiscovery is intended to become a middleware layer for:

- service registration and deregistration
- service instance discovery
- healthy instance selection
- topology-aware and network-aware routing recommendations
- application-side feedback collection for later scheduling decisions

The long-term design target is not to replace existing RPC stacks. EtDiscovery is meant to act as:

- a smarter address book
- an instance selector
- a discovery and control-plane integration point for existing business frameworks

## Features And Progress

- ✅ Runtime foundation
  - ✅ host EasyTier from a .NET web application
  - ✅ read real node and peer state through `easytier-cli`
  - ✅ map peer observations into discovery candidates
- ✅ Service registry
  - ✅ registry-maintained in-memory service catalog
  - ✅ worker-driven HTTP registration
  - ✅ multiple instances per service
  - ✅ registry nodes that can also act as workers and register local services
  - ✅ return real registered instances instead of fixed config stubs
- ✅ Basic selection
  - ✅ select instances by service name
  - ✅ support client-side or gateway-side integration of instance selection
- ✅ Current concurrency model
  - ✅ one shared in-memory data source
  - ✅ concurrent updates
  - ✅ point-in-time snapshot reads
  - ✅ allow short-lived inconsistencies across reads
- ⬜ Remaining work
  - ⬜ lease renewal
  - ⬜ health updates
  - ⬜ draining and status override
  - ⬜ standalone metadata updates
  - ⬜ node-level management APIs
  - ⬜ watch streams
  - ⬜ feedback-driven scheduling and weak-network-aware scoring
  - ⬜ stable multi-language SDK and integration story

## Current Stage

This project is currently a prototype intended for design validation and early integration discussion.

What is already working:

- EasyTier process hosting from a .NET web app
- reading real EasyTier node and peer state via `easytier-cli`
- mapping peer observations into discovery candidates
- an in-memory registry for registered service instances
- worker-to-registry HTTP registration
- auto-fallback from explicit `RegistryPeer` to the first remote discoverable peer's virtual IP
- `registry`-side `/discovery/services` and `/discovery/select`
- `worker`-side self-registration based on `Services[]`
- a concurrency model based on one shared in-memory data source with concurrent updates and point-in-time snapshot reads

## Current Assumptions

The current prototype assumes:

- most remote peers are registry peers
- relay-only or transit-only nodes are not modeled yet
- service discovery data is eventually consistent
- short-lived inconsistency between node state and instance state is acceptable

This means two reads close in time may legitimately return different results.

## Licensing

This repository is intended to be licensed under `AGPL-3.0-only`. See [LICENSE](./LICENSE).

Why this choice:

- the project is middleware intended to run as a network-facing service
- the goal is to discourage service providers from shipping privately modified, not-fully-compatible middleware forks without publishing the corresponding source
- a standard, OSI-approved license is preferable to writing custom "AGPL-like" terms
- `AGPL-3.0-only` is a more widely understood choice than alternatives such as `OSL-3.0`

More specifically, the project is trying to express the following policy goal:

- if someone modifies EasyTier Discovery itself and offers that modified version to third parties as a network service, the corresponding source for those EasyTier Discovery modifications should be available to those users
- the focus is not an abstract desire that "all SaaS must be open source", but a practical desire to reduce opaque long-lived forks whose incompatibilities and adaptation costs are pushed onto downstream developers and integrators
- this goal should be expressed through a standard license text, not through custom add-on clauses or vague repository-level wording

A representative risk looks like this:

- a service provider takes an open middleware project, heavily modifies it, and markets the result as "compatible with" some protocol or system
- in practice, the compatibility is only partial, but the implementation details of the fork are not published
- external developers are then forced to spend time adapting around behavior gaps, and the ecosystem gradually fragments around opaque vendor-specific variants
- this project wants to reduce the room for that outcome as much as possible

The reason to use `AGPL-3.0-only` directly, instead of inventing an "AGPL-like" license, is to avoid two common failure modes:

- custom wording often makes the boundary less clear rather than more clear, because users now have to interpret a non-standard license
- attempts to make network copyleft "stronger" or "more precise" by adding extra conditions often reduce interoperability, compatibility, and adoption

At the same time, this README also wants to clarify a few common concerns:

- the intended focus is modifications to EasyTier Discovery itself and the deployment of those modifications, not an expansive claim that every independent business system talking to it over the network must automatically be relicensed
- this README is not meant to impose obligations beyond `AGPL-3.0-only`, and it is not meant to argue for a special interpretation narrower or broader than the actual license text

These notes are provided only to explain the project's licensing intent and rationale. The authoritative legal terms are the license text itself. If you need a formal conclusion for a specific deployment, distribution, or compliance scenario, get legal advice.

## Repository Layout

- `EtDiscovery.Web/`
  - ASP.NET Core host
  - EasyTier process management
  - HTTP APIs
  - registration orchestrators
- `EtDiscovery.Core/`
  - shared discovery models
  - catalog building
  - selection policy abstractions
- `EtDiscovery.Tests/`
  - unit and integration-oriented test coverage for the prototype
- `docs/`
  - design notes
  - implementation status
  - external reference material

## Early Developer Experience

This is not a production-ready package yet. The current developer workflow is meant for early validation and design iteration.

> [!WARNING]
> This repository is still in an extremely early stage. APIs, configuration shape, internal behavior, and deployment workflow may change directly without compatibility guarantees.
> If you integrate with it now, assume that substantial API changes may happen at any time and that there is no stable-version promise or detailed migration notice process yet.

### 1. Build

From the repository root:

```powershell
dotnet build EtDiscovery.Web/EtDiscovery.Web.csproj
```

### 2. Configure a Registry

Use a config file under `EtDiscovery.Web/` or a published output directory.

Registry-specific notes:

- use `roles=registry`
- set a fixed `Ipv4`
- `Services[]` can be empty if the registry is not also a worker

### 3. Configure a Worker

Worker-specific notes:

- use `roles=worker`
- define one or more `Services[]`
- optionally set `RegistryPeer`
- if `RegistryPeer` is empty, the worker currently falls back to the first remote discoverable peer's virtual IP

Example worker service config:

```json
{
  "EtDiscovery": {
    "NetworkName": "etd-test",
    "NetworkSecret": "test-secret123!",
    "VirtualNetworkCidr": "10.1.1.0/24",
    "ListenUrl": "http://127.0.0.1:8080",
    "Peers": ["tcp://bootstrap.example.com:11010"],
    "RegistryPeer": "",
    "Services": [
      {
        "ServiceName": "test",
        "Port": 8080,
        "Protocol": "http"
      }
    ]
  }
}
```

### 4. Run

Registry:

```powershell
dotnet run --project EtDiscovery.Web -- --roles registry
```

Worker:

```powershell
dotnet run --project EtDiscovery.Web -- --roles worker
```

For more detailed design and implementation notes, start with [`docs/README.md`](./docs/README.md).

## Status for Contributors

This repository is open for:

- design discussion
- prototype iteration
- API shape review
- service-registry migration feedback
- weak-network and topology-aware scheduling discussion

It is not yet ready for:

- stability guarantees
- backward compatibility guarantees
- production deployment guidance
- fixed SDK contracts

The most useful feedback at this stage is:

- service registry API design
- licensing and contribution model feedback
- framework integration expectations
- behavior under unstable peer and virtual-IP conditions
