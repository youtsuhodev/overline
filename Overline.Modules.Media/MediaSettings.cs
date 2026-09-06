namespace Overline.Modules.Media;

/// <summary>Settings of the Media module.</summary>
public sealed class MediaSettings
{
    /// <summary>Show a music emoji before the title.</summary>
    public bool ShowEmoji { get; set; } = true;

    /// <summary>Show the track title.</summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>Show the artist after the title.</summary>
    public bool ShowArtist { get; set; } = true;

    /// <summary>Show elapsed / total playback time.</summary>
    public bool ShowTime { get; set; } = true;

    /// <summary>Show a small filled progress bar.</summary>
    public bool ShowProgressBar { get; set; } = true;
}