using EtDiscovery.Runtime.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtDiscovery.Runtime.Controllers;

[ApiController]
public sealed class EasyTierController : ControllerBase
{
    [HttpGet("/easytier/peers")]
    public async Task<IActionResult> GetPeers(
        [FromServices] EasyTierObservationService observationService,
        CancellationToken cancellationToken)
    {
        var snapshot = await observationService.GetCurrentSnapshotAsync(cancellationToken);
        var peers = snapshot.Peers.Select(peer => new
        {
            nodeId = peer.NodeId,
            peerId = peer.PeerId,
            hostname = peer.Hostname,
            networkName = peer.NetworkName,
            virtualIp = peer.VirtualIp,
            sameNetwork = peer.SameNetwork,
            inVirtualNetworkCidr = peer.InVirtualNetworkCidr,
            eligibleForDiscovery = peer.EligibleForDiscovery,
            isLocal = peer.IsLocal,
            foreignNetworkName = peer.ForeignNetworkName,
            cost = peer.Cost,
            nodeTypeAppId = peer.NodeTypeAppId,
            nodeTypeFlags = peer.NodeTypeFlags,
            roles = peer.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
            isRegistryCandidate = peer.IsRegistryCandidate,
        });

        return Ok(new
        {
            observedAt = snapshot.ObservedAt,
            localNode = new
            {
                nodeId = snapshot.LocalNode.NodeId,
                virtualIp = snapshot.LocalNode.VirtualIp,
                nodeTypeAppId = snapshot.LocalNode.NodeTypeAppId,
                nodeTypeFlags = snapshot.LocalNode.NodeTypeFlags,
                roles = snapshot.LocalNode.Roles.Select(role => role.ToString().ToLowerInvariant()).ToArray(),
            },
            peers,
        });
    }
}
