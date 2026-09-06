using Overline.Core.Osc;
using Xunit;

namespace Overline.Tests.Core;

public class OscChatboxEncoderTests
{
    [Fact]
    public void Encode_produces_address_type_tag_and_payload()
    {
        var packet = OscChatboxEncoder.Encode(new OscChatboxMessage("hi"));

        var text = System.Text.Encoding.ASCII.GetString(packet);
        Assert.StartsWith("/chatbox/input", text);
        Assert.Contains(",sT", text);
        Assert.Contains("hi", text);
    }

    [Fact]
    public void Encode_pads_every_blob_to_4_byte_alignment()
    {
        var packet = OscChatboxEncoder.Encode(new OscChatboxMessage("hi"));
        // Address + type-tag + text are each NUL-terminated and 4-aligned.
        Assert.Equal(0, packet.Length % 4);
        var text = System.Text.Encoding.ASCII.GetString(packet);
        Assert.EndsWith("hi\0\0", text);
    }

    [Fact]
    public void Encode_send_immediately_uses_true_typetag_and_no_payload()
    {
        var immediate = OscChatboxEncoder.Encode(new OscChatboxMessage("hi", SendImmediately: true));
        var deferred = OscChatboxEncoder.Encode(new OscChatboxMessage("hi", SendImmediately: false));

        var immediateText = System.Text.Encoding.ASCII.GetString(immediate);
        var deferredText = System.Text.Encoding.ASCII.GetString(deferred);

        Assert.Contains(",sT", immediateText);
        Assert.Contains(",sF", deferredText);

        // The only difference between the two packets is the type-tag letter;
        // a boolean carries no payload bytes.
        Assert.Equal(immediate.Length, deferred.Length);
    }

    [Fact]
    public void Encode_throws_over_limit()
    {
        Assert.Throws<ArgumentException>(
            () => OscChatboxEncoder.Encode(new OscChatboxMessage(new string('a', 145))));
    }

    [Fact]
    public void Truncate_keeps_text_at_or_under_limit()
    {
        var longText = new string('a', 500);
        var truncated = OscChatboxEncoder.TruncateToLimit(longText);
        Assert.True(truncated.Length <= OscChatboxEncoder.MaxChatboxCharacters);
    }

    [Fact]
    public void Truncate_never_splits_surrogate_pairs()
    {
        var pair = "𝕏";   // surrogate pair (2 UTF-16 units)
        var text = new string('a', 143) + pair;
        var truncated = OscChatboxEncoder.TruncateToLimit(text);
        Assert.Equal(new string('a', 143), truncated);
        Assert.DoesNotContain(pair, truncated);
    }
}
