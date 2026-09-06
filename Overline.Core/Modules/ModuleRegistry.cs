using System.Collections.ObjectModel;

namespace Overline.Core.Modules;

/// <summary>
/// Central registry of modules. Order of registration defines the default
/// assembly order of the chatbox line.
/// </summary>
public interface IModuleRegistry
{
    /// <summary>All registered modules in registration order.</summary>
    IReadOnlyList<IModule> Modules { get; }

    /// <summary>Raised after a module is registered.</summary>
    event EventHandler<ModuleRegisteredEventArgs>? ModuleRegistered;

    /// <summary>Register a module. Registering the same instance twice is a no-op.</summary>
    void Register(IModule module);

    /// <summary>Find a module by type, or null.</summary>
    T? Find<T>() where T : class, IModule;

    /// <summary>Stop and dispose every module, tolerating individual failures.</summary>
    Task StopAllAsync(CancellationToken cancellationToken = default);
}

public sealed class ModuleRegisteredEventArgs(IModule module) : EventArgs
{
    public IModule Module { get; } = module;
}

/// <summary>Default implementation: thread-safe list with stable ordering.</summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly object _gate = new();
    private readonly List<IModule> _modules = [];

    public event EventHandler<ModuleRegisteredEventArgs>? ModuleRegistered;

    public IReadOnlyList<IModule> Modules
    {
        get
        {
            lock (_gate)
            {
                return new ReadOnlyCollection<IModule>([.. _modules]);
            }
        }
    }

    public void Register(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        lock (_gate)
        {
            if (_modules.Contains(module))
            {
                return;
            }

            _modules.Add(module);
        }

        ModuleRegistered?.Invoke(this, new ModuleRegisteredEventArgs(module));
    }

    public T? Find<T>() where T : class, IModule
    {
        lock (_gate)
        {
            return _modules.OfType<T>().FirstOrDefault();
        }
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IModule> snapshot;
        lock (_gate)
        {
            snapshot = new ReadOnlyCollection<IModule>([.. _modules]);
        }

        foreach (var module in snapshot)
        {
            try
            {
                await module.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A misbehaving module must never prevent the app from shutting down.
            }
        }
    }
}
