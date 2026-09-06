using Microsoft.Extensions.Logging;
using Windows.Media.Control;
using WindowsMediaController;
using MediaSession = WindowsMediaController.MediaManager.MediaSession;

namespace Overline.Modules.Media;

/// <summary>
/// Reads the currently playing media on this PC using the Global System Media
/// Transport Controls (the same data the taskbar volume flyout shows). Picks the
/// focused session and, failing that, the first playing one.
/// </summary>
public sealed class WindowsMediaSessionMonitor : IMediaSessionMonitor
{
    private readonly ILogger<WindowsMediaSessionMonitor> _logger;
    private readonly object _gate = new();
    private readonly Dictionary<string, SessionTrack> _sessions = new(StringComparer.Ordinal);

    private MediaManager? _manager;
    private MediaSnapshot? _current;
    private bool _disposed;

    public WindowsMediaSessionMonitor(ILogger<WindowsMediaSessionMonitor> logger)
    {
        _logger = logger;
    }

    public event EventHandler? Changed;

    public MediaSnapshot? Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_manager != null)
            {
                return;
            }

            var manager = new MediaManager();
            manager.OnAnySessionOpened += OnSessionOpened;
            manager.OnAnySessionClosed += OnSessionClosed;
            manager.OnAnyMediaPropertyChanged += OnMediaPropertyChanged;
            manager.OnAnyPlaybackStateChanged += OnPlaybackStateChanged;
            manager.OnAnyTimelinePropertyChanged += OnTimelinePropertyChanged;
            manager.OnFocusedSessionChanged += OnFocusedSessionChanged;
            manager.Start();
            _manager = manager;
        }

        _logger.LogInformation("Media session monitor started");
    }

    public void Stop()
    {
        MediaManager? manager;
        lock (_gate)
        {
            manager = _manager;
            _manager = null;
            _sessions.Clear();
            _current = null;
        }

        if (manager != null)
        {
            manager.OnAnySessionOpened -= OnSessionOpened;
            manager.OnAnySessionClosed -= OnSessionClosed;
            manager.OnAnyMediaPropertyChanged -= OnMediaPropertyChanged;
            manager.OnAnyPlaybackStateChanged -= OnPlaybackStateChanged;
            manager.OnAnyTimelinePropertyChanged -= OnTimelinePropertyChanged;
            manager.OnFocusedSessionChanged -= OnFocusedSessionChanged;
            manager.Dispose();
        }

        _logger.LogInformation("Media session monitor stopped");
    }

    private void OnFocusedSessionChanged(MediaSession? session)
    {
        // Prefer the focused session, but keep any playing session visible.
        if (session == null)
        {
            return;
        }

        TryUpdate(session);
    }

    private void OnSessionOpened(MediaSession session)
    {
        _ = RefreshSessionAsync(session);
    }

    private void OnSessionClosed(MediaSession session)
    {
        lock (_gate)
        {
            _sessions.Remove(session.Id);
            RecomputeLocked();
        }

        RaiseChanged();
    }

    private void OnMediaPropertyChanged(MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(sender.Id, out var track))
            {
                track.Title = args.Title;
                track.Artist = args.Artist;
                track.Album = args.AlbumTitle;
            }
        }

        Recompute();
    }

    private void OnPlaybackStateChanged(MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(sender.Id, out var track))
            {
                track.IsPlaying = args.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            }
        }

        Recompute();
    }

    private void OnTimelinePropertyChanged(MediaSession sender, GlobalSystemMediaTransportControlsSessionTimelineProperties args)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(sender.Id, out var track))
            {
                track.Position = args.Position - args.StartTime;
                track.Duration = args.EndTime - args.StartTime;
                track.PositionSampleUtc = DateTime.UtcNow;
            }
        }

        Recompute();
    }

    private async Task RefreshSessionAsync(MediaSession session)
    {
        try
        {
            var props = await session.ControlSession.TryGetMediaPropertiesAsync();
            var playback = session.ControlSession.GetPlaybackInfo();
            var timeline = session.ControlSession.GetTimelineProperties();

            lock (_gate)
            {
                var track = _sessions.GetValueOrDefault(session.Id);
                if (track == null)
                {
                    track = new SessionTrack();
                    _sessions[session.Id] = track;
                }

                track.Title = props.Title;
                track.Artist = props.Artist;
                track.Album = props.AlbumTitle;
                track.IsPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                track.Position = timeline.Position - timeline.StartTime;
                track.Duration = timeline.EndTime - timeline.StartTime;
                track.PositionSampleUtc = DateTime.UtcNow;
            }

            Recompute();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read media session {Id}", session.Id);
        }
    }

    private void TryUpdate(MediaSession session)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(session.Id, out var track))
            {
                RecomputeLocked();
            }
        }

        RaiseChanged();
    }

    private void Recompute()
    {
        lock (_gate)
        {
            RecomputeLocked();
        }

        RaiseChanged();
    }

    private void RecomputeLocked()
    {
        // Pick the first actively playing session, else keep the current one.
        SessionTrack? best = null;
        foreach (var track in _sessions.Values)
        {
            if (track.IsPlaying)
            {
                best = track;
                break;
            }
        }

        best ??= _sessions.Values.FirstOrDefault();

        MediaSnapshot? snapshot = null;
        if (best != null && !string.IsNullOrWhiteSpace(best.Title))
        {
            var position = best.Position;
            if (best.IsPlaying)
            {
                position += DateTime.UtcNow - best.PositionSampleUtc;
            }

            snapshot = new MediaSnapshot(best.Title, best.Artist, best.Album, best.IsPlaying, position, best.Duration);
        }

        if (!Equals(_current, snapshot))
        {
            _current = snapshot;
        }
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private sealed class SessionTrack
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string? Album { get; set; }
        public bool IsPlaying { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime PositionSampleUtc { get; set; }
    }
}