using System.Net;
using System.Text;

namespace EtDiscovery.Sdk.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        Requests.Add((request.Method, path, body));
        return Responder(request);
    }
}
