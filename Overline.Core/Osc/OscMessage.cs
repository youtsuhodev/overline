using System.Text;

namespace Overline.Core.Osc;

/// <summary>Everything needed to send one chatbox update.</summary>
public sealed record OscChatboxMessage(
    string Text,
    bool SendImmediately = true);

/// <summary>
/// Minimal OSC 1.0 encoder for VRChat's chatbox endpoint
/// (/chatbox/input). UDP send lives in the app layer so Core stays testable.
/// </summary>
public static class OscChatboxEncoder
{
    public const string ChatboxInputAddress = "/chatbox/input";
    public const int VrChatDefaultOscPort = 9000;

    /// <summary>VRChat's hard limit for chatbox text.</summary>
    public const int MaxChatboxCharacters = 144;

    /// <summary>
    /// Encode a chatbox message into an OSC UDP packet.
    /// VRChat's /chatbox/input takes a string plus a boolean (typing /
    /// send-immediately). In OSC a boolean is a type-tag character (T/F)
    /// and carries no payload, so the packet is just the address, the
    /// type-tag string and the text blob.
    /// Throws when the text exceeds VRChat's 144-character limit; callers
    /// are expected to keep assembled lines within budget.
    /// </summary>
    public static byte[] Encode(OscChatboxMessage message)
    {
        if (message.Text.Length > MaxChatboxCharacters)
        {
            throw new ArgumentException(
                $"Chatbox text is {message.Text.Length} characters; the limit is {MaxChatboxCharacters}.",
                nameof(message));
        }

        var buffer = new MemoryStream();
        WriteString(buffer, ChatboxInputAddress);
        WriteString(buffer, message.SendImmediately ? ",sT" : ",sF");
        WriteString(buffer, message.Text);
        return buffer.ToArray();
    }

    /// <summary>Truncate on grapheme boundaries down to the UTF-8 byte budget of 144 characters.</summary>
    public static string TruncateToLimit(string text)
    {
        if (text.Length <= MaxChatboxCharacters)
        {
            return text;
        }

        var sb = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            if (sb.Length + rune.Utf8SequenceLength > MaxChatboxCharacters)
            {
                break;
            }

            sb.Append(rune);
        }

        return sb.ToString();
    }

    /// <summary>Write an OSC string: UTF-8 bytes, NUL-terminated, padded to a 4-byte boundary.</summary>
    private static void WriteString(MemoryStream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
        stream.WriteByte(0);
        var padding = (4 - ((bytes.Length + 1) % 4)) % 4;
        for (var i = 0; i < padding; i++)
        {
            stream.WriteByte(0);
        }
    }
}