namespace Overline.Core.Time;

/// <summary>
/// Clock abstraction so modules and services are testable around time
/// (schedules, cooldowns, session durations).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset LocalNow => UtcNow.ToLocalTime();
}

/// <summary>System clock.</summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>Frozen clock for deterministic tests.</summary>
public sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public void Advance(TimeSpan by) => UtcNow += by;
}
