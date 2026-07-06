using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class RegistrySnapshotBuilder
{
    public DiscoverySnapshot Build(EasyTierObservationSnapshot observation, IReadOnlyList<ServiceInstance>? instances = null)
    {
        var nodes = new List<NodeSnapshot>();
        var nodeProfiles = new List<NodeProfile>();

        foreach (var peer in observation.Peers.Where(peer => peer.EligibleForDiscovery))
        {
            nodes.Add(ToNodeSnapshot(peer));
            nodeProfiles.Add(ToNodeProfile(peer));
        }

        return new DiscoverySnapshot(nodes, instances ?? [], nodeProfiles);
    }

    private static NodeSnapshot ToNodeSnapshot(ObservedPeer peer) => new(peer.NodeId, peer.Reachable)
    {
        VirtualIp = peer.VirtualIp,
        Address = peer.VirtualIp,
        Roles = peer.Roles,
        NetworkType = NetworkType.Unknown,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static NodeProfile ToNodeProfile(ObservedPeer peer) => new(peer.NodeId)
    {
        Roles = peer.Roles,
        VirtualIp = peer.VirtualIp,
        NetworkType = peer.NetworkName,
    };
}
