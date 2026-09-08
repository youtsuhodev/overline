using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Overline.Core.Osc;

namespace Overline.App.Osc;

/// <summary>Result of one OSC send attempt, for UI status indicators.</summary>
public sealed record OscSendResultEventArgs(bool Success, DateTimeOffset Timestamp, int Bytes);

/// <summary>
/// Sends encoded OSC packets over UDP to VRChat. Thin on purpose:
/// all interesting logic (encoding, budget, assembly) lives in Core.
/// </summary>
public interface IOscSender : IDisposable
{
    /// <summary>Send a chatbox update. Returns false when the send failed (logged, never thrown).</summary>
    bool SendChatbox(OscChatboxMessage message);

    /// <summary>Raised after every send attempt with its outcome (UI status indicator).</summary>
    event EventHandler<OscSendResultEventArgs>? SendCompleted;
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

    public event EventHandler<OscSendResultEventArgs>? SendCompleted;

    public bool SendChatbox(OscChatboxMessage message)
    {
        var success = false;
        var bytes = 0;

        try
        {
            var line = OscChatboxEncoder.TruncateToLimit(message.Text);
            var packet = OscChatboxEncoder.Encode(message with { Text = line });
            bytes = packet.Length;
            var sent = _client.Send(packet, bytes, _endpoint);
            _logger.LogDebug("OSC chatbox: {Bytes} bytes to {Endpoint}", sent, _endpoint);
            success = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSC chatbox send failed");
        }

        SendCompleted?.Invoke(this, new OscSendResultEventArgs(success, DateTimeOffset.Now, bytes));
        return success;
    }

    public void Dispose() => _client.Dispose();
}
