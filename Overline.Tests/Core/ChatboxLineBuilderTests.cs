using Overline.Core.Osc;
using Xunit;

namespace Overline.Tests.Core;

public class ChatboxLineBuilderTests
{
    private const string Sep = ChatboxLineBuilder.Separator;

    [Fact]
    public void Joins_segments_with_separator()
    {
        var result = ChatboxLineBuilder.Build(
        [
            new OscSegment("cpu 40%", SegmentPriority.Normal),
            new OscSegment("21:41", SegmentPriority.High),
        ]);

        Assert.Equal($"21:41{Sep}cpu 40%", result.Line);
        Assert.False(result.HasDrops);
    }

    [Fact]
    public void Drops_optional_first_when_overflowing()
    {
        var high = new string('h', 60);
        var normal = new string('n', 60);
        var optional = new string('o', 60); // 3×60 + separators > 144
        var result = ChatboxLineBuilder.Build(
        [
            new OscSegment(high, SegmentPriority.High),
            new OscSegment(normal, SegmentPriority.Normal),
            new OscSegment(optional, SegmentPriority.Optional),
        ]);

        Assert.True(result.HasDrops);
        Assert.Contains(high, result.Line);
        Assert.Contains(normal, result.Line);
        Assert.DoesNotContain(optional, result.Line);
        Assert.True(result.Line.Length <= 144);
    }

    [Fact]
    public void Critical_is_never_dropped()
    {
        var critical = new string('c', 140);
        var other = new string('o', 20);
        var result = ChatboxLineBuilder.Build(
        [
            new OscSegment(critical, SegmentPriority.Critical),
            new OscSegment(other, SegmentPriority.High),
        ]);

        Assert.StartsWith("c", result.Line);
        Assert.True(result.Line.Length <= 144);
        Assert.True(result.HasDrops);
    }

    [Fact]
    public void Empty_segments_are_ignored()
    {
        var result = ChatboxLineBuilder.Build(
        [
            new OscSegment(""),
            new OscSegment("  "),
            new OscSegment("kept"),
        ]);

        Assert.Equal("kept", result.Line);
    }
}
