namespace Overline.Core.Settings;

/// <summary>
/// Typed accessor over the settings store for one settings section.
/// Modules depend only on Core.
/// </summary>
public interface ISettingsHolder<T> where T : class, new()
{
    T Value { get; }

    /// <summary>Persist the current value.</summary>
    void Save();

    /// <summary>Raised when the underlying section is saved.</summary>
    event EventHandler? Changed;
}

/// <summary>Default implementation over ISettingsStore with change propagation.</summary>
public sealed class SettingsHolder<T> : ISettingsHolder<T>, IDisposable where T : class, new()
{
    private readonly ISettingsStore _store;
    private readonly string _key;
    private readonly T _value;

    public SettingsHolder(ISettingsStore store, string key)
    {
        _store = store;
        _key = key;
        _value = store.Get<T>(key);
        store.Saved += OnStoreSaved;
    }

    public T Value => _value;

    public event EventHandler? Changed;

    public void Save() => _store.Save(_key, _value);

    private void OnStoreSaved(object? sender, SettingsSavedEventArgs e)
    {
        if (string.Equals(e.Key, _key, StringComparison.Ordinal))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => _store.Saved -= OnStoreSaved;
}
