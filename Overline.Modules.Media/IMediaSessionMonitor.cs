namespace Overline.Modules.Media;

/// <summary>
/// Provides the currently playing media on this PC. Windows-only implementation
/// wraps the Global System Media Transport Controls via MediaManager.
/// </summary>
public interface IMediaSessionMonitor : IDisposable
{
    /// <summary>Latest known media snapshot; null when nothing is playing.</summary>
    MediaSnapshot? Current { get; }

    /// <summary>Raised whenever title/artist/playback state changes.</summary>
    event EventHandler? Changed;

    void Start();

    void Stop();
}