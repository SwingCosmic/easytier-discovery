using System.Net;
using System.Text;
using System.Text.Json;
using EtDiscovery.Core.Models;
using NUnit.Framework;

namespace EtDiscovery.Sdk.Tests;

public sealed class EtDiscoveryHttpClientTests
{
    [Test]
    public async Task SelectOneAsync_IssuesGetToRuntimeSelect()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => JsonResponse(new SelectedInstance("svc-b", "id-1", "node-1")
            {
                VirtualIp = "10.1.1.2",
                RecommendedEndpoint = new EndpointDescriptor("10.1.1.2", 9002)
                {
                    Protocol = "http",
                    IsRecommended = true,
                },
            }),
        };

        var client = CreateClient(handler);
        var selected = await client.SelectOneAsync("svc-b", protocol: "http");

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.ServiceName, Is.EqualTo("svc-b"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(handler.Requests[0].Path, Does.Contain("/runtime/v1/select"));
        Assert.That(handler.Requests[0].Path, Does.Contain("serviceName=svc-b"));
        Assert.That(handler.Requests[0].Path, Does.Contain("protocol=http"));
    }

    [Test]
    public async Task RegisterAndHeartbeat_UseExpectedPaths()
    {
        var handler = new RecordingHandler
        {
            Responder = req =>
            {
                if (req.Method == HttpMethod.Post)
                {
                    return JsonResponse(new { instanceId = "inst-1" });
                }

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            },
        };

        var client = CreateClient(handler, o =>
        {
            o.ServiceName = "svc-a";
            o.Port = 9001;
            o.Protocol = "http";
        });

        await client.RegisterAsync();
        await client.HeartbeatAsync();

        Assert.That(client.RegisteredInstanceId, Is.EqualTo("inst-1"));
        Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.Requests[0].Path, Is.EqualTo("/runtime/v1/instances"));
        Assert.That(handler.Requests[1].Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(handler.Requests[1].Path, Is.EqualTo("/runtime/v1/instances/inst-1/heartbeat"));
    }

    [Test]
    public async Task DeregisterAsync_UsesDelete()
    {
        var handler = new RecordingHandler
        {
            Responder = req =>
            {
                if (req.Method == HttpMethod.Post)
                {
                    return JsonResponse(new { instanceId = "inst-x" });
                }

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            },
        };

        var client = CreateClient(handler, o =>
        {
            o.ServiceName = "svc-a";
            o.Port = 9001;
        });

        await client.RegisterAsync();
        await client.DeregisterAsync();

        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(handler.Requests.Last().Path, Is.EqualTo("/runtime/v1/instances/inst-x"));
        Assert.That(client.RegisteredInstanceId, Is.Null);
    }

    private static EtDiscoveryHttpClient CreateClient(
        RecordingHandler handler,
        Action<EtDiscoveryClientOptions>? configure = null)
    {
        var options = new EtDiscoveryClientOptions
        {
            RuntimeEndpoint = "http://127.0.0.1:8081",
        };
        configure?.Invoke(options);
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:8081/"),
        };
        return new EtDiscoveryHttpClient(http, options);
    }

    private static HttpResponseMessage JsonResponse(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}
