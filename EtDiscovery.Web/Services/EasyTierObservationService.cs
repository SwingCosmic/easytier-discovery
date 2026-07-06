using EtDiscovery.Web.Models;
using System.Threading;

namespace EtDiscovery.Web.Services;

public sealed class EasyTierObservationService
{
    private readonly EtDiscoveryWebOptions _options;
    private readonly EasyTierCliClient _cliClient;
    private readonly PeerObservationMapper _mapper;
    private readonly ILogger<EasyTierObservationService> _logger;
    private EasyTierObservationSnapshot? _lastSnapshot;

    public EasyTierObservationService(
        EtDiscoveryWebOptions options,
        EasyTierCliClient cliClient,
        PeerObservationMapper mapper,
        ILogger<EasyTierObservationService> logger)
    {
        _options = options;
        _cliClient = cliClient;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EasyTierObservationSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        var nodeInfo = await _cliClient.GetNodeInfoAsync(cancellationToken)
            ?? throw new InvalidOperationException("Failed to read local EasyTier node info.");
        var peers = await _cliClient.GetPeerListAsync(cancellationToken);
        Dictionary<string, ForeignNetworkEntry> foreignNetworks;
        try
        {
            foreignNetworks = await _cliClient.GetForeignNetworksAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query foreign networks from easytier-cli. Continuing with an empty foreign-network view.");
            foreignNetworks = new Dictionary<string, ForeignNetworkEntry>(StringComparer.Ordinal);
        }

        var snapshot = _mapper.Map(_options, nodeInfo, peers, foreignNetworks);
        LogSnapshotChanges(snapshot);
        Volatile.Write(ref _lastSnapshot, snapshot);

        return snapshot;
    }

    public EasyTierObservationSnapshot? GetLastSnapshot()
    {
        return Volatile.Read(ref _lastSnapshot);
    }

    private void LogSnapshotChanges(EasyTierObservationSnapshot currentSnapshot)
    {
        var previousSnapshot = Volatile.Read(ref _lastSnapshot);

        if (previousSnapshot is null)
        {
            _logger.LogInformation(
                "EasyTier snapshot initialized. localNodeId={NodeId} localVirtualIp={VirtualIp} peers={PeerCount} eligiblePeers={EligiblePeerCount}",
                currentSnapshot.LocalNode.NodeId,
                DisplayValue(currentSnapshot.LocalNode.VirtualIp),
                currentSnapshot.Peers.Count,
                currentSnapshot.Peers.Count(peer => peer.EligibleForDiscovery));
        }
        else if (!string.Equals(previousSnapshot.LocalNode.VirtualIp, currentSnapshot.LocalNode.VirtualIp, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Local EasyTier virtual IP changed. nodeId={NodeId} previous={PreviousVirtualIp} current={CurrentVirtualIp}",
                currentSnapshot.LocalNode.NodeId,
                DisplayValue(previousSnapshot.LocalNode.VirtualIp),
                DisplayValue(currentSnapshot.LocalNode.VirtualIp));
        }

        var previousPeers = (previousSnapshot?.Peers ?? []).ToDictionary(peer => peer.NodeId, StringComparer.Ordinal);
        var currentPeers = currentSnapshot.Peers.ToDictionary(peer => peer.NodeId, StringComparer.Ordinal);

        foreach (var peer in currentSnapshot.Peers)
        {
            if (!previousPeers.TryGetValue(peer.NodeId, out var previousPeer))
            {
                _logger.LogInformation(
                    "EasyTier peer discovered. nodeId={NodeId} hostname={Hostname} peerId={PeerId} isLocal={IsLocal} networkName={NetworkName} virtualIp={VirtualIp} eligibleForDiscovery={EligibleForDiscovery}",
                    peer.NodeId,
                    peer.Hostname ?? "<unknown>",
                    peer.PeerId,
                    peer.IsLocal,
                    peer.NetworkName,
                    DisplayValue(peer.VirtualIp),
                    peer.EligibleForDiscovery);
                continue;
            }

            if (HasMeaningfulChange(previousPeer, peer))
            {
                _logger.LogInformation(
                    "EasyTier peer changed. nodeId={NodeId} hostname={Hostname} networkName={PreviousNetworkName}->{CurrentNetworkName} virtualIp={PreviousVirtualIp}->{CurrentVirtualIp} sameNetwork={PreviousSameNetwork}->{CurrentSameNetwork} inVirtualNetworkCidr={PreviousInVirtualNetworkCidr}->{CurrentInVirtualNetworkCidr} eligibleForDiscovery={PreviousEligibleForDiscovery}->{CurrentEligibleForDiscovery} cost={PreviousCost}->{CurrentCost}",
                    peer.NodeId,
                    peer.Hostname ?? "<unknown>",
                    previousPeer.NetworkName,
                    peer.NetworkName,
                    DisplayValue(previousPeer.VirtualIp),
                    DisplayValue(peer.VirtualIp),
                    previousPeer.SameNetwork,
                    peer.SameNetwork,
                    previousPeer.InVirtualNetworkCidr,
                    peer.InVirtualNetworkCidr,
                    previousPeer.EligibleForDiscovery,
                    peer.EligibleForDiscovery,
                    DisplayValue(previousPeer.Cost),
                    DisplayValue(peer.Cost));
            }
        }

        foreach (var peer in previousPeers.Values)
        {
            if (!currentPeers.ContainsKey(peer.NodeId))
            {
                _logger.LogInformation(
                    "EasyTier peer lost. nodeId={NodeId} hostname={Hostname} peerId={PeerId} networkName={NetworkName} virtualIp={VirtualIp} eligibleForDiscovery={EligibleForDiscovery}",
                    peer.NodeId,
                    peer.Hostname ?? "<unknown>",
                    peer.PeerId,
                    peer.NetworkName,
                    DisplayValue(peer.VirtualIp),
                    peer.EligibleForDiscovery);
            }
        }

        _logger.LogDebug(
            "EasyTier snapshot refreshed. localVirtualIp={LocalVirtualIp} peers={PeerCount} sameNetworkPeers={SameNetworkPeerCount} eligiblePeers={EligiblePeerCount}",
            DisplayValue(currentSnapshot.LocalNode.VirtualIp),
            currentSnapshot.Peers.Count,
            currentSnapshot.Peers.Count(peer => peer.SameNetwork),
            currentSnapshot.Peers.Count(peer => peer.EligibleForDiscovery));
    }

    private static bool HasMeaningfulChange(ObservedPeer previousPeer, ObservedPeer currentPeer) =>
        !string.Equals(previousPeer.NetworkName, currentPeer.NetworkName, StringComparison.Ordinal) ||
        !string.Equals(previousPeer.VirtualIp, currentPeer.VirtualIp, StringComparison.OrdinalIgnoreCase) ||
        previousPeer.SameNetwork != currentPeer.SameNetwork ||
        previousPeer.InVirtualNetworkCidr != currentPeer.InVirtualNetworkCidr ||
        previousPeer.EligibleForDiscovery != currentPeer.EligibleForDiscovery ||
        !string.Equals(previousPeer.Cost, currentPeer.Cost, StringComparison.OrdinalIgnoreCase);

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
}
