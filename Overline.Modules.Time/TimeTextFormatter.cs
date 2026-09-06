using System.Globalization;

namespace Overline.Modules.Time;

/// <summary>
/// Pure text formatter for the Time module — no clocks, no state, trivially
/// unit-testable in any timezone via injected DateTimeOffset.
/// </summary>
public static class TimeTextFormatter
{
    public static string Format(DateTimeOffset localTime, TimeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var sb = new System.Text.StringBuilder();

        if (settings.ShowDate)
        {
            sb.Append(localTime.ToString("d MMM", CultureInfo.InvariantCulture)).Append(' ');
        }

        if (settings.ShowEmoji)
        {
            sb.Append(ClockFaceEmoji(localTime.Hour)).Append(' ');
        }

        sb.Append(FormatTime(localTime, settings));

        if (settings.ShowTimeZone)
        {
            var zone = TimeZoneInfo.Local.IsDaylightSavingTime(localTime.DateTime)
                ? TimeZoneInfo.Local.DaylightName
                : TimeZoneInfo.Local.StandardName;
            var abbreviation = Abbreviate(zone);
            if (!string.IsNullOrEmpty(abbreviation))
            {
                sb.Append(' ').Append(abbreviation);
            }
        }

        return sb.ToString();
    }

    private static string FormatTime(DateTimeOffset localTime, TimeSettings settings)
    {
        var pattern = settings.Use24Hour
            ? (settings.ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (settings.ShowSeconds ? "h:mm:ss tt" : "h:mm tt");
        return localTime.ToString(pattern, CultureInfo.InvariantCulture);
    }

    private static string Abbreviate(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName))
        {
            return string.Empty;
        }

        var words = zoneName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = words
            .Where(w => char.IsUpper(w[0]))
            .Select(w => w[0])
            .ToArray();

        return letters.Length >= 2 && letters.Length <= 4
            ? new string(letters)
            : string.Empty;
    }

    private static readonly string[] ClockFaces =
    [
        "🕛", // 0h
        "🕐", // 1h
        "🕑", // 2h
        "🕒", // 3h
        "🕓", // 4h
        "🕔", // 5h
        "🕕", // 6h
        "🕖", // 7h
        "🕗", // 8h
        "🕘", // 9h
        "🕙", // 10h
        "🕚", // 11h
    ];

    private static string ClockFaceEmoji(int hour) => ClockFaces[hour % 12];
}
