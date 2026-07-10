namespace EtDiscovery.Core.Models;

/// <summary>
/// EtDiscovery application-namespace bits carried in EasyTier <c>node_type_flags</c>.
/// EasyTier does not interpret these bits; EtDiscovery owns the convention.
/// </summary>
public static class EtDiscoveryNodeTypeFlags
{
    /// <summary>
    /// Application namespace id written into EasyTier <c>node_type_app_id</c>.
    /// </summary>
    public const uint AppId = 1;

    /// <summary>
    /// High-bit marker: peer declares the EtDiscovery registry role.
    /// </summary>
    public const uint Registry = 1u << 16;

    /// <summary>
    /// High-bit marker: peer declares the EtDiscovery worker role.
    /// </summary>
    public const uint Worker = 1u << 17;

    /// <summary>
    /// High-bit marker: peer declares the EtDiscovery client role.
    /// </summary>
    public const uint Client = 1u << 18;

    public const uint RoleMask = Registry | Worker | Client;

    /// <summary>
    /// Encodes local EtDiscovery roles into EasyTier route metadata values.
    /// Flags are always derived from roles; callers must not accept external overrides.
    /// </summary>
    public static (uint AppId, uint Flags) EncodeRoles(IEnumerable<NodeRole> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        uint flags = 0;
        foreach (var role in roles.Distinct())
        {
            flags |= role switch
            {
                NodeRole.Registry => Registry,
                NodeRole.Worker => Worker,
                NodeRole.Client => Client,
                _ => 0u,
            };
        }

        // Empty or unknown-only role sets still advertise the EtDiscovery app id with no role bits.
        // Consumers treat "no role bits" as worker by default.
        return (AppId, flags);
    }

    /// <summary>
    /// Decodes EasyTier route metadata into EtDiscovery roles.
    /// Missing / foreign / empty high bits default to worker.
    /// </summary>
    public static IReadOnlyList<NodeRole> DecodeRoles(uint? appId, uint flags)
    {
        if (appId is not AppId)
        {
            return [NodeRole.Worker];
        }

        var roles = new List<NodeRole>(3);
        if ((flags & Registry) != 0)
        {
            roles.Add(NodeRole.Registry);
        }

        if ((flags & Worker) != 0)
        {
            roles.Add(NodeRole.Worker);
        }

        if ((flags & Client) != 0)
        {
            roles.Add(NodeRole.Client);
        }

        return roles.Count == 0 ? [NodeRole.Worker] : roles;
    }

    public static bool IsRegistryCandidate(uint? appId, uint flags)
        => appId is AppId && (flags & Registry) != 0;
}
