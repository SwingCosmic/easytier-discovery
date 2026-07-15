using System.Text.Json;
using EtDiscovery.Runtime.Models;

namespace EtDiscovery.Tests;

[TestFixture]
public class EasyTierCliRouteJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    [Test]
    public void DeserializesVerbosePeerRouteIgnoringNestedIpv4Object()
    {
        // Real easytier-cli `peer list -v` shape: ipv4_addr.address is { "addr": <u32> }, not a string.
        const string json = """
            [
              {
                "route": {
                  "peer_id": 9,
                  "hostname": "registry",
                  "ipv4_addr": {
                    "address": { "addr": 167837953 },
                    "network_length": 24
                  },
                  "node_type_flags": 65536,
                  "node_type_app_id": 1
                }
              }
            ]
            """;

        var pairs = JsonSerializer.Deserialize<IReadOnlyList<EasyTierPeerRoutePair>>(json, JsonOptions);

        Assert.That(pairs, Is.Not.Null);
        Assert.That(pairs!, Has.Count.EqualTo(1));
        Assert.That(pairs[0].Route, Is.Not.Null);
        Assert.That(pairs[0].Route!.PeerId, Is.EqualTo(9u));
        Assert.That(pairs[0].Route.Hostname, Is.EqualTo("registry"));
        Assert.That(pairs[0].Route.NodeTypeAppId, Is.EqualTo(1u));
        Assert.That(pairs[0].Route.NodeTypeFlags, Is.EqualTo(65536u));
    }
}
