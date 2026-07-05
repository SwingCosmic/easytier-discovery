namespace EtDiscovery.Core.Models;

/// <summary>
/// Describes one concrete endpoint exposed by a selected instance.
/// </summary>
public sealed class EndpointDescriptor
{
    public EndpointDescriptor(string address, int port)
    {
        Address = address;
        Port = port;
    }

    public string Address { get; set; }

    public int Port { get; set; }

    public string Protocol { get; set; } = "http";

    public CallMode CallMode { get; set; } = CallMode.Direct;

    public bool IsRecommended { get; set; }
}
