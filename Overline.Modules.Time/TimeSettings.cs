namespace Overline.Modules.Time;

/// <summary>Settings of the Time module.</summary>
public sealed class TimeSettings
{
    /// <summary>Show a clock-face emoji before the time (🕐-🕛).</summary>
    public bool ShowEmoji { get; set; } = true;

    /// <summary>Show the local date (e.g. 6 Sep) before the time.</summary>
    public bool ShowDate { get; set; } = true;

    /// <summary>Append the local time zone abbreviation (e.g. CEST).</summary>
    public bool ShowTimeZone { get; set; } = true;

    /// <summary>Use 24-hour clock instead of 12-hour AM/PM.</summary>
    public bool Use24Hour { get; set; } = true;

    /// <summary>Show seconds.</summary>
    public bool ShowSeconds { get; set; } = false;
}
