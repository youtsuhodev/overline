using Overline.Core.Modules;
using Overline.Core.Osc;
using Overline.Core.Privacy;
using Overline.Core.Settings;

namespace Overline.Modules.Media;

/// <summary>
/// Media module: reads the currently playing track on this PC and contributes
/// its title, artist and playback time to the chatbox line.
/// </summary>
public sealed class MediaModule : IModule, ISegmentSource
{
    private readonly ISettingsHolder<MediaSettings> _settings;
    private readonly IMediaSessionMonitor _monitor;
    private readonly IPrivacyConsentService _consent;
    private readonly object _gate = new();
    private ModuleState _state = ModuleState.Stopped;

    public MediaModule(
        ISettingsHolder<MediaSettings> settings,
        IMediaSessionMonitor monitor,
        IPrivacyConsentService consent)
    {
        _settings = settings;
        _monitor = monitor;
        _consent = consent;
    }

    public string Name => "Media";
    public string Description => "Currently playing media on this PC — title, artist, playback time.";
    public string Icon => "🎵";

    public ModuleState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

    public MediaSnapshot? CurrentMedia => _monitor.Current;

    public OscSegment? BuildSegment()
    {
        if (State != ModuleState.Running)
        {
            return null;
        }

        var text = MediaTextFormatter.Format(_monitor.Current, _settings.Value);
        return string.IsNullOrEmpty(text)
            ? null
            : new OscSegment(text, SegmentPriority.Normal);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_state is ModuleState.Running or ModuleState.Starting)
            {
                return Task.CompletedTask;
            }

            SetState(ModuleState.Starting);
        }

        if (!_consent.IsApproved(PrivacyHooks.MediaSession))
        {
            // Reading media metadata is gated behind user consent.
            _consent.SetApproved(PrivacyHooks.MediaSession, approved: true);
        }

        _monitor.Start();
        SetState(ModuleState.Running);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_state is ModuleState.Stopped or ModuleState.Stopping)
            {
                return Task.CompletedTask;
            }

            SetState(ModuleState.Stopping);
            _monitor.Stop();
            SetState(ModuleState.Stopped);
        }

        return Task.CompletedTask;
    }

    private void SetState(ModuleState newState)
    {
        ModuleState previous;
        lock (_gate)
        {
            previous = _state;
            _state = newState;
        }

        StateChanged?.Invoke(this, new ModuleStateChangedEventArgs(previous, newState));
    }

    public void Dispose()
    {
        _monitor.Dispose();
    }
}