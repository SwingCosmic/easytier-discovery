using System.Globalization;
using System.Text;

namespace EtDiscovery.Web.Services;

/// <summary>
/// Builds an EasyTier-native TOML config file for <c>easytier-core -c</c>.
/// Node type metadata is always derived from local roles and never taken from app config.
/// </summary>
public sealed class EasyTierConfigGenerator
{
    /// <summary>
    /// Matches easytier-core CLI default when listeners are omitted:
    /// port 11010 expands to tcp/udp/ws/wss/wg style endpoints.
    /// Required because launching with only <c>-c</c> + <c>--rpc-portal</c> does not
    /// re-apply NetworkOptions listener defaults (can_merge is false).
    /// </summary>
    private static readonly string[] DefaultListeners =
    [
        "tcp://0.0.0.0:11010",
        "udp://0.0.0.0:11010",
        "ws://0.0.0.0:11011/",
        "wss://0.0.0.0:11012/",
        "wg://0.0.0.0:11011",
    ];

    public string GenerateToml(EtDiscoveryWebOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var easyTier = options.EasyTier;
        var (appId, flags) = options.GetAdvertisedNodeTypeMetadata();
        var builder = new StringBuilder();

        AppendAssignment(builder, "instance_name", easyTier.InstanceName);
        if (!string.IsNullOrWhiteSpace(easyTier.Hostname))
        {
            AppendAssignment(builder, "hostname", easyTier.Hostname);
        }

        if (!string.IsNullOrWhiteSpace(easyTier.Ipv4))
        {
            AppendAssignment(builder, "ipv4", easyTier.Ipv4);
        }

        AppendAssignment(builder, "dhcp", options.ShouldEnableDhcp);
        AppendAssignment(builder, "node_type_app_id", appId);
        AppendAssignment(builder, "node_type_flags", flags);

        if (!string.IsNullOrWhiteSpace(easyTier.ExternalNode))
        {
            AppendAssignment(builder, "external_node", easyTier.ExternalNode);
        }

        // Always emit listeners. An empty list previously produced configs with only
        // ring://, so remote peers could no longer connect to tcp://host:11010.
        var listeners = easyTier.Listeners.Count > 0 ? easyTier.Listeners : DefaultListeners;
        AppendStringArray(builder, "listeners", listeners);

        if (easyTier.ProxyNetworks.Count > 0)
        {
            AppendStringArray(builder, "proxy_networks", easyTier.ProxyNetworks);
        }

        builder.AppendLine();
        builder.AppendLine("[network_identity]");
        AppendAssignment(builder, "network_name", options.NetworkName);
        AppendAssignment(builder, "network_secret", options.NetworkSecret);

        foreach (var peer in easyTier.Peers)
        {
            builder.AppendLine();
            builder.AppendLine("[[peer]]");
            AppendAssignment(builder, "uri", peer);
        }

        if (easyTier.Flags.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("[flags]");
            foreach (var (key, value) in easyTier.Flags.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                AppendRawAssignment(builder, key, value);
            }
        }

        return builder.ToString();
    }

    public string WriteTempConfig(EtDiscoveryWebOptions options)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "etdiscovery",
            SanitizePathSegment(options.EasyTier.InstanceName));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "easytier.toml");
        // Avoid UTF-8 BOM; some parsers tolerate it, but EasyTier samples are BOM-less.
        File.WriteAllText(path, GenerateToml(options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars);
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }

    private static void AppendAssignment(StringBuilder builder, string key, string value)
    {
        builder.Append(key);
        builder.Append(" = ");
        builder.AppendLine(Quote(value));
    }

    private static void AppendAssignment(StringBuilder builder, string key, bool value)
    {
        builder.Append(key);
        builder.Append(" = ");
        builder.AppendLine(value ? "true" : "false");
    }

    private static void AppendAssignment(StringBuilder builder, string key, uint value)
    {
        builder.Append(key);
        builder.Append(" = ");
        builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendStringArray(StringBuilder builder, string key, IReadOnlyList<string> values)
    {
        builder.Append(key);
        builder.Append(" = [");
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Quote(values[index]));
        }

        builder.AppendLine("]");
    }

    private static void AppendRawAssignment(StringBuilder builder, string key, string value)
    {
        // Flags may be bool/number/string. Keep simple JSON-ish literals unquoted when safe.
        if (bool.TryParse(value, out _) ||
            long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            builder.Append(key);
            builder.Append(" = ");
            builder.AppendLine(value);
            return;
        }

        AppendAssignment(builder, key, value);
    }

    private static string Quote(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }
}
