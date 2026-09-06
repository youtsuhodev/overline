using Overline.Modules.Media;
using Xunit;

namespace Overline.Tests.Modules;

public class MediaTextFormatterTests
{
    private static MediaSnapshot Track(string? artist = "Ado") =>
        new("Show", artist ?? string.Empty, "Album", IsPlaying: true, TimeSpan.FromSeconds(83), TimeSpan.FromMinutes(4).Add(TimeSpan.FromSeconds(5)));

    [Fact]
    public void Format_emoji_title_artist_time()
    {
        var text = MediaTextFormatter.Format(Track(), new MediaSettings());
        Assert.StartsWith("🎵 Show", text);
        Assert.Contains("Ado", text);
    }

    [Fact]
    public void Format_includes_progress_bar_and_elapsed()
    {
        var text = MediaTextFormatter.Format(Track(), new MediaSettings { ShowEmoji = false });
        Assert.Contains("[", text);
        Assert.Contains("1:23 / 4:05", text);
    }

    [Fact]
    public void Format_hides_artist_when_disabled()
    {
        var text = MediaTextFormatter.Format(Track(), new MediaSettings { ShowEmoji = false, ShowArtist = false });
        Assert.Equal("Show [███░░░░░] 1:23 / 4:05", text);
    }

    [Fact]
    public void Format_hides_time_when_disabled()
    {
        var text = MediaTextFormatter.Format(Track(), new MediaSettings { ShowEmoji = false, ShowTime = false, ShowProgressBar = false });
        Assert.Equal("Show — Ado", text);
    }

    [Fact]
    public void Format_empty_when_no_title()
    {
        Assert.Equal(string.Empty, MediaTextFormatter.Format(null, new MediaSettings()));
    }

    [Fact]
    public void Format_omits_progress_when_duration_zero()
    {
        var live = Track() with { Duration = TimeSpan.Zero };
        var text = MediaTextFormatter.Format(live, new MediaSettings { ShowEmoji = false });
        Assert.DoesNotContain("[", text);
        Assert.DoesNotContain("/", text);
    }

    [Fact]
    public void Format_uses_hours_when_long()
    {
        var longMedia = Track() with { Duration = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(12)), Position = TimeSpan.FromMinutes(30) };
        var text = MediaTextFormatter.Format(longMedia, new MediaSettings { ShowEmoji = false, ShowProgressBar = false });
        Assert.Contains("30:00 / 1:12:00", text);
    }
}