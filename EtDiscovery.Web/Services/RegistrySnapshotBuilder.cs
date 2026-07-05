using EtDiscovery.Core.Models;
using EtDiscovery.Web.Models;

namespace EtDiscovery.Web.Services;

public sealed class RegistrySnapshotBuilder
{
    private readonly EtDiscoveryWebOptions _options;

    public RegistrySnapshotBuilder(EtDiscoveryWebOptions options)
    {
        _options = options;
    }

    public DiscoverySnapshot Build(EasyTierObservationSnapshot observation)
    {
        var nodes = new List<NodeSnapshot>();
        var nodeProfiles = new List<NodeProfile>();
        var instances = new List<ServiceInstance>();

        foreach (var peer in observation.Peers.Where(peer => !peer.IsLocal && peer.EligibleForDiscovery))
        {
            nodes.Add(ToNodeSnapshot(peer));
            nodeProfiles.Add(ToNodeProfile(peer));
            instances.Add(CreateRemoteWorkerInstance(peer));
        }

        if (_options.IsWorker)
        {
            var localPeer = observation.Peers.FirstOrDefault(peer => peer.IsLocal);
            if (localPeer is not null && localPeer.EligibleForDiscovery)
            {
                nodes.Add(ToNodeSnapshot(localPeer));
                nodeProfiles.Add(ToNodeProfile(localPeer));
                instances.Add(CreateLocalWorkerInstance(localPeer));
            }
        }

        return new DiscoverySnapshot(nodes, instances, nodeProfiles);
    }

    private NodeSnapshot ToNodeSnapshot(ObservedPeer peer) => new(peer.NodeId, peer.Reachable)
    {
        VirtualIp = peer.VirtualIp,
        Address = peer.VirtualIp,
        Roles = peer.Roles,
        NetworkType = NetworkType.Unknown,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private NodeProfile ToNodeProfile(ObservedPeer peer) => new(peer.NodeId)
    {
        Roles = peer.Roles,
        VirtualIp = peer.VirtualIp,
        NetworkType = peer.NetworkName,
    };

    private ServiceInstance CreateRemoteWorkerInstance(ObservedPeer peer)
    {
        if (string.IsNullOrWhiteSpace(peer.VirtualIp) || _options.RegistryWorkerServicePort is null)
        {
            throw new InvalidOperationException("Registry worker instance requires a virtual IP and configured service port.");
        }

        return new ServiceInstance(
            $"remote-worker-{peer.NodeId}",
            _options.GetRegistryServiceKey(),
            peer.NodeId,
            peer.VirtualIp,
            _options.RegistryWorkerServicePort.Value)
        {
            Protocol = "http",
            VirtualIp = peer.VirtualIp,
            OwnerNodeId = peer.NodeId,
            Endpoints =
            [
                new EndpointDescriptor(peer.VirtualIp, _options.RegistryWorkerServicePort.Value)
                {
                    Protocol = "http",
                    CallMode = CallMode.Direct,
                    IsRecommended = true,
                },
            ],
        };
    }

    private ServiceInstance CreateLocalWorkerInstance(ObservedPeer peer)
    {
        if (string.IsNullOrWhiteSpace(peer.VirtualIp) || _options.WorkerServicePort is null)
        {
            throw new InvalidOperationException("Local worker instance requires a virtual IP and configured service port.");
        }

        return new ServiceInstance(
            $"local-worker-{peer.NodeId}",
            _options.GetWorkerServiceKey(),
            peer.NodeId,
            peer.VirtualIp,
            _options.WorkerServicePort.Value)
        {
            Protocol = "http",
            VirtualIp = peer.VirtualIp,
            OwnerNodeId = peer.NodeId,
            Endpoints =
            [
                new EndpointDescriptor(peer.VirtualIp, _options.WorkerServicePort.Value)
                {
                    Protocol = "http",
                    CallMode = CallMode.Direct,
                    IsRecommended = true,
                },
            ],
        };
    }
}
