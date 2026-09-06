using System.Globalization;

namespace Overline.Modules.Media;

/// <summary>
/// Pure text formatter for the Media module — no media state, trivially
/// unit-testable. Produces lines like:  🎵 Ado — Show  1:23 / 4:05
/// </summary>
public static class MediaTextFormatter
{
    public static string Format(MediaSnapshot? media, MediaSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (media is null || string.IsNullOrWhiteSpace(media.Title))
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        if (settings.ShowEmoji)
        {
            sb.Append("🎵 ");
        }

        if (settings.ShowTitle)
        {
            sb.Append(media.Title);
        }

        if (settings.ShowArtist && !string.IsNullOrWhiteSpace(media.Artist))
        {
            if (sb.Length > 0)
            {
                sb.Append(" — ");
            }

            sb.Append(media.Artist);
        }

        if (settings.ShowProgressBar && media.Duration > TimeSpan.Zero)
        {
            var ratio = (double)media.Position.Ticks / media.Duration.Ticks;
            var progress = Math.Clamp(ratio, 0, 1);
            var filled = (int)Math.Round(progress * ProgressBarLength);
            filled = Math.Clamp(filled, 0, ProgressBarLength);
            sb.Append(" [")
              .Append(new string('█', filled))
              .Append(new string('░', ProgressBarLength - filled))
              .Append(']');
        }

        if (settings.ShowTime && media.Duration > TimeSpan.Zero)
        {
            sb.Append(' ').Append(FormatTime(media.Position))
              .Append(" / ").Append(FormatTime(media.Duration));
        }

        return sb.ToString();
    }

    public const int ProgressBarLength = 8;

    private static string FormatTime(TimeSpan value)
        => value.ToString(value.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture);
}