using Overline.Core.Modules;
using Overline.Core.Osc;
using Overline.Core.Settings;
using Overline.Core.Time;

namespace Overline.Modules.Time;

/// <summary>
/// Reference module: contributes the local time to the chatbox line.
/// Demonstrates the full module contract — settings, formatter, segment.
/// </summary>
public sealed class TimeModule : IModule, ISegmentSource
{
    private readonly ISettingsHolder<TimeSettings> _settings;
    private readonly IClock _clock;
    private readonly object _gate = new();

    private System.Threading.Timer? _timer;
    private ModuleState _state = ModuleState.Stopped;

    public TimeModule(ISettingsHolder<TimeSettings> settings, IClock clock)
    {
        _settings = settings;
        _clock = clock;
    }

    public string Name => "Time";
    public string Description => "Your local time and time zone.";
    public string Icon => "🕐";

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

    /// <summary>Latest formatted segment; empty when stopped.</summary>
    public OscSegment CurrentSegment
    {
        get
        {
            if (State != ModuleState.Running)
            {
                return new OscSegment(string.Empty);
            }

            return new OscSegment(
                TimeTextFormatter.Format(_clock.LocalNow, _settings.Value),
                SegmentPriority.High);
        }
    }

    public OscSegment? BuildSegment()
        => State == ModuleState.Running && !string.IsNullOrEmpty(CurrentSegment.Text)
            ? CurrentSegment
            : null;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_state is ModuleState.Running or ModuleState.Starting)
            {
                return Task.CompletedTask;
            }

            SetState(ModuleState.Starting);
            // Align the tick on the minute so the text changes exactly when the minute does.
            var now = _clock.UtcNow;
            var delayToNextMinute = TimeSpan.FromSeconds(60 - now.Second) + TimeSpan.FromMilliseconds(1000 - now.Millisecond);
            _timer = new System.Threading.Timer(Tick, null, delayToNextMinute, TimeSpan.FromMinutes(1));
        }

        // Emit immediately once, then publish the state change.
        Tick(null);
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
            _timer?.Dispose();
            _timer = null;
            SetState(ModuleState.Stopped);
        }

        return Task.CompletedTask;
    }

    private void Tick(object? state)
    {
        // The assembler pulls CurrentSegment; a segment-change notification hook
        // can be added later without changing this module's core logic.
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
        _timer?.Dispose();
        _timer = null;
    }
}
