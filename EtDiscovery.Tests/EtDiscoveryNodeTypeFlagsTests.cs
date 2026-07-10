using EtDiscovery.Core.Models;

namespace EtDiscovery.Tests;

[TestFixture]
public class EtDiscoveryNodeTypeFlagsTests
{
    [Test]
    public void EncodeRolesSetsAppIdAndRoleBits()
    {
        var (appId, flags) = EtDiscoveryNodeTypeFlags.EncodeRoles([NodeRole.Registry, NodeRole.Worker]);

        Assert.That(appId, Is.EqualTo(EtDiscoveryNodeTypeFlags.AppId));
        Assert.That(flags & EtDiscoveryNodeTypeFlags.Registry, Is.Not.EqualTo(0u));
        Assert.That(flags & EtDiscoveryNodeTypeFlags.Worker, Is.Not.EqualTo(0u));
        Assert.That(flags & EtDiscoveryNodeTypeFlags.Client, Is.EqualTo(0u));
    }

    [Test]
    public void DecodeRolesDefaultsToWorkerWhenAppIdMissing()
    {
        Assert.That(
            EtDiscoveryNodeTypeFlags.DecodeRoles(null, EtDiscoveryNodeTypeFlags.Registry),
            Is.EqualTo(new[] { NodeRole.Worker }));
    }

    [Test]
    public void DecodeRolesDefaultsToWorkerWhenHighBitsEmpty()
    {
        Assert.That(
            EtDiscoveryNodeTypeFlags.DecodeRoles(EtDiscoveryNodeTypeFlags.AppId, 0),
            Is.EqualTo(new[] { NodeRole.Worker }));
    }

    [Test]
    public void DecodeRolesReadsMultiRoleBits()
    {
        var flags = EtDiscoveryNodeTypeFlags.Registry | EtDiscoveryNodeTypeFlags.Client;
        Assert.That(
            EtDiscoveryNodeTypeFlags.DecodeRoles(EtDiscoveryNodeTypeFlags.AppId, flags),
            Is.EqualTo(new[] { NodeRole.Registry, NodeRole.Client }));
    }

    [Test]
    public void IsRegistryCandidateRequiresAppIdAndRegistryBit()
    {
        Assert.That(EtDiscoveryNodeTypeFlags.IsRegistryCandidate(EtDiscoveryNodeTypeFlags.AppId, EtDiscoveryNodeTypeFlags.Registry), Is.True);
        Assert.That(EtDiscoveryNodeTypeFlags.IsRegistryCandidate(EtDiscoveryNodeTypeFlags.AppId, EtDiscoveryNodeTypeFlags.Worker), Is.False);
        Assert.That(EtDiscoveryNodeTypeFlags.IsRegistryCandidate(null, EtDiscoveryNodeTypeFlags.Registry), Is.False);
        Assert.That(EtDiscoveryNodeTypeFlags.IsRegistryCandidate(2, EtDiscoveryNodeTypeFlags.Registry), Is.False);
    }
}
