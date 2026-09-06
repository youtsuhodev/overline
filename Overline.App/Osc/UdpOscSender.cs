using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Overline.Core.Osc;

namespace Overline.App.Osc;

/// <summary>
/// Sends encoded OSC packets over UDP to VRChat. Thin on purpose:
/// all interesting logic (encoding, budget, assembly) lives in Core.
/// </summary>
public interface IOscSender : IDisposable
{
    /// <summary>Send a chatbox update. Returns false when the send failed (logged, never thrown).</summary>
    bool SendChatbox(OscChatboxMessage message);
}

public sealed class UdpOscSender : IOscSender
{
    private readonly ILogger<UdpOscSender> _logger;
    private readonly UdpClient _client;
    private readonly IPEndPoint _endpoint;

    public UdpOscSender(ILogger<UdpOscSender> logger, int port = OscChatboxEncoder.VrChatDefaultOscPort, string host = "127.0.0.1")
    {
        _logger = logger;
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        _client = new UdpClient();
    }

    public bool SendChatbox(OscChatboxMessage message)
    {
        try
        {
            var line = OscChatboxEncoder.TruncateToLimit(message.Text);
            var packet = OscChatboxEncoder.Encode(message with { Text = line });
            var sent = _client.Send(packet, packet.Length, _endpoint);
            _logger.LogDebug("OSC chatbox: {Bytes} bytes to {Endpoint}", sent, _endpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSC chatbox send failed");
            return false;
        }
    }

    public void Dispose() => _client.Dispose();
}
