using Overline.Core.Modules;
using Overline.Core.Settings;
using Overline.Core.Time;
using Overline.Modules.Time;
using Xunit;

namespace Overline.Tests.Modules;

public class TimeModuleTests
{
    private static DateTimeOffset TestTime() => new(2026, 9, 6, 21, 41, 0, TimeSpan.Zero);

    [Fact]
    public void Format_24h_with_emoji_and_zone()
    {
        var settings = new TimeSettings { ShowEmoji = true, ShowTimeZone = false, Use24Hour = true, ShowDate = false };
        var text = TimeTextFormatter.Format(TestTime(), settings);
        Assert.Equal("🕘 21:41", text);
    }

    [Fact]
    public void Format_12h_adds_am_pm()
    {
        var settings = new TimeSettings { ShowEmoji = false, ShowTimeZone = false, Use24Hour = false, ShowDate = false };
        var text = TimeTextFormatter.Format(TestTime(), settings);
        Assert.Equal("9:41 PM", text);
    }

    [Fact]
    public void Format_includes_date_when_enabled()
    {
        var settings = new TimeSettings { ShowEmoji = false, ShowTimeZone = false, Use24Hour = true, ShowDate = true };
        var text = TimeTextFormatter.Format(TestTime(), settings);
        Assert.Equal("6 Sep 21:41", text);
    }

    [Fact]
    public void Format_omits_date_when_disabled()
    {
        var settings = new TimeSettings { ShowEmoji = false, ShowTimeZone = false, Use24Hour = true, ShowDate = false };
        var text = TimeTextFormatter.Format(TestTime(), settings);
        Assert.Equal("21:41", text);
    }

    [Fact]
    public void Start_then_stop_is_idempotent()
    {
        using var store = new TestSettingsStore();
        var module = new TimeModule(
            new SettingsHolder<TimeSettings>(store, "time"),
            new FixedClock(TestTime()));

        module.StartAsync();
        Assert.Equal(ModuleState.Running, module.State);
        module.StartAsync(); // idempotent
        Assert.Equal(ModuleState.Running, module.State);

        module.StopAsync();
        Assert.Equal(ModuleState.Stopped, module.State);
        module.StopAsync(); // idempotent
        Assert.Equal(ModuleState.Stopped, module.State);
    }

    [Fact]
    public void Segment_empty_when_stopped()
    {
        using var store = new TestSettingsStore();
        var module = new TimeModule(
            new SettingsHolder<TimeSettings>(store, "time"),
            new FixedClock(TestTime()));

        Assert.Equal(string.Empty, module.CurrentSegment.Text);
    }

    [Fact]
    public void Segment_present_when_running()
    {
        using var store = new TestSettingsStore();
        var module = new TimeModule(
            new SettingsHolder<TimeSettings>(store, "time"),
            new FixedClock(TestTime()));

        module.StartAsync();
        Assert.False(string.IsNullOrEmpty(module.CurrentSegment.Text));
        module.StopAsync();
    }

    /// <summary>In-memory settings store for tests.</summary>
    private sealed class TestSettingsStore : ISettingsStore, IDisposable
    {
        private readonly Dictionary<string, object> _sections = [];

        public event EventHandler<SettingsSavedEventArgs>? Saved
        {
            add { }
            remove { }
        }

        public T Get<T>(string key) where T : class, new()
            => _sections.TryGetValue(key, out var value) && value is T typed ? typed : new T();

        public void Save<T>(string key, T value) where T : class
            => _sections[key] = value;

        public void Dispose() { }
    }
}
