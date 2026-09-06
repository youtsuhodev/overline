using System;

namespace Overline.Core.Modules;

/// <summary>
/// One integration (music, hardware, social, ...) that can start, stop and contribute
/// segments to the chatbox line. Implementations must be safe to stop at any point.
/// </summary>
public interface IModule : IDisposable
{
    /// <summary>Human-readable, stable name used in logs and UI.</summary>
    string Name { get; }

    /// <summary>Short description shown in the integrations catalog.</summary>
    string Description { get; }

    /// <summary>Icon identifier resolved by the UI (emoji for now).</summary>
    string Icon { get; }

    /// <summary>Current lifecycle state.</summary>
    ModuleState State { get; }

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

    /// <summary>Start the module. Must be idempotent.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stop the module. Must be idempotent and quick.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>Lifecycle states of an <see cref="IModule"/>.</summary>
public enum ModuleState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Faulted,
}

public sealed class ModuleStateChangedEventArgs : EventArgs
{
    public ModuleStateChangedEventArgs(ModuleState previous, ModuleState current, string? detail = null)
    {
        Previous = previous;
        Current = current;
        Detail = detail;
    }

    public ModuleState Previous { get; }
    public ModuleState Current { get; }
    public string? Detail { get; }
}
