using System.Net.Http.Json;
using System.Text.Json;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

/// <summary>
/// Resolves a registry endpoint from explicit config or EasyTier route metadata.
/// Does not fall back to "first peer with a virtual IP".
/// </summary>
public sealed class RegistryLocator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly EtDiscoveryWebOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RegistryLocator> _logger;
    private readonly object _sync = new();
    private RegistryEndpoint? _lastResolved;

    public RegistryLocator(
        EtDiscoveryWebOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<RegistryLocator> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public RegistryEndpoint? GetLastResolved()
    {
        lock (_sync)
        {
            return _lastResolved;
        }
    }

    public async Task<RegistryEndpoint?> ResolveAsync(
        EasyTierObservationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (_options.IsRegistry)
        {
            var local = new RegistryEndpoint
            {
                Address = _options.ListenUrl,
                Source = "local",
                NodeId = snapshot.LocalNode.NodeId,
                VirtualIp = snapshot.LocalNode.VirtualIp,
            };
            SetLastResolved(local);
            return local;
        }

        foreach (var candidate in EnumerateCandidates(snapshot))
        {
            if (await ValidateCandidateAsync(candidate, cancellationToken))
            {
                _logger.LogInformation(
                    "Selected registry endpoint. address={Address} source={Source} nodeId={NodeId} virtualIp={VirtualIp}",
                    candidate.Address,
                    candidate.Source,
                    candidate.NodeId,
                    candidate.VirtualIp);
                SetLastResolved(candidate);
                return candidate;
            }
        }

        _logger.LogWarning(
            "No registry candidate available. explicitCandidates={ExplicitCount} autoDiscover={AutoDiscover} routeMetadataCandidates={RouteMetadataCount}",
            _options.RegistryCandidates.Count,
            _options.AutoDiscoverFromRouteMetadata,
            snapshot.Peers.Count(peer => !peer.IsLocal && peer.EligibleForDiscovery && peer.IsRegistryCandidate));
        SetLastResolved(null);
        return null;
    }

    public IEnumerable<RegistryEndpoint> EnumerateCandidates(EasyTierObservationSnapshot snapshot)
    {
        foreach (var candidate in _options.RegistryCandidates)
        {
            yield return new RegistryEndpoint
            {
                Address = candidate,
                Source = "explicit",
            };
        }

        if (!_options.AutoDiscoverFromRouteMetadata)
        {
            yield break;
        }

        foreach (var peer in snapshot.Peers
                     .Where(peer => !peer.IsLocal && peer.EligibleForDiscovery && peer.IsRegistryCandidate)
                     .Where(peer => !string.IsNullOrWhiteSpace(peer.VirtualIp)))
        {
            yield return new RegistryEndpoint
            {
                Address = peer.VirtualIp!,
                Source = "route_metadata",
                NodeId = peer.NodeId,
                VirtualIp = peer.VirtualIp,
            };
        }
    }

    private async Task<bool> ValidateCandidateAsync(RegistryEndpoint candidate, CancellationToken cancellationToken)
    {
        // Explicit endpoints are trusted configuration. Route-metadata candidates get a light probe.
        if (string.Equals(candidate.Source, "explicit", StringComparison.Ordinal) ||
            string.Equals(candidate.Source, "local", StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(nameof(RegistryLocator));
            var endpoint = new Uri(_options.GetRegistryBaseUri(candidate.Address), "/discovery/registry");
            using var response = await client.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Registry metadata probe failed. address={Address} status={StatusCode}",
                    candidate.Address,
                    (int)response.StatusCode);
                return false;
            }

            var metadata = await response.Content.ReadFromJsonAsync<RegistryMetadataResponse>(JsonOptions, cancellationToken);
            if (metadata is null)
            {
                return false;
            }

            if (!string.Equals(metadata.NetworkName, _options.NetworkName, StringComparison.Ordinal))
            {
                _logger.LogDebug(
                    "Registry metadata network mismatch. address={Address} expected={Expected} actual={Actual}",
                    candidate.Address,
                    _options.NetworkName,
                    metadata.NetworkName);
                return false;
            }

            var roles = metadata.Roles ?? [];
            if (!roles.Any(role => string.Equals(role, "registry", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Registry metadata missing registry role. address={Address}", candidate.Address);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Registry metadata probe threw. address={Address}", candidate.Address);
            return false;
        }
    }

    private void SetLastResolved(RegistryEndpoint? endpoint)
    {
        lock (_sync)
        {
            _lastResolved = endpoint;
        }
    }
}
