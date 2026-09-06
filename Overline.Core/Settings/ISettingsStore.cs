using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Overline.Core.Settings;

/// <summary>
/// Typed, per-section settings persistence. Sections are small POCOs identified by
/// a stable key; the store serializes them into one JSON file per profile.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Load the settings section of type <typeparamref name="T"/> under <paramref name="key"/>, creating defaults on first use.</summary>
    T Get<T>(string key) where T : class, new();

    /// <summary>Persist the given section immediately.</summary>
    void Save<T>(string key, T value) where T : class;

    /// <summary>Raised after a section is saved, so observers can react without polling.</summary>
    event EventHandler<SettingsSavedEventArgs>? Saved;
}

public sealed class SettingsSavedEventArgs(string key) : EventArgs
{
    public string Key { get; } = key;
}

/// <summary>JSON file backed store. One file per profile directory.</summary>
public sealed partial class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _directory;
    private readonly ConcurrentDictionary<string, object> _cache = new(StringComparer.Ordinal);

    public JsonSettingsStore(string? directory = null)
    {
        _directory = directory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Overline",
                "settings");
        Directory.CreateDirectory(_directory);
    }

    public event EventHandler<SettingsSavedEventArgs>? Saved;

    public T Get<T>(string key) where T : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_cache.TryGetValue(key, out var cached) && cached is T typed)
        {
            return typed;
        }

        var value = LoadFromDisk<T>(key);
        _cache[key] = value;
        return value;
    }

    public void Save<T>(string key, T value) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _cache[key] = value;
        WriteToDisk(key, value);
        Saved?.Invoke(this, new SettingsSavedEventArgs(key));
    }

    private string PathFor(string key)
    {
        var safe = string.Concat(key.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        return Path.Combine(_directory, safe + ".json");
    }

    private T LoadFromDisk<T>(string key) where T : class, new()
    {
        try
        {
            var path = PathFor(key);
            if (!File.Exists(path))
            {
                return new T();
            }

            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, SerializerOptions) ?? new T();
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings fall back to defaults rather than crash the app.
            return new T();
        }
    }

    private void WriteToDisk<T>(string key, T value) where T : class
    {
        try
        {
            var path = PathFor(key);
            using var stream = File.Create(path);
            JsonSerializer.Serialize(stream, value, SerializerOptions);
        }
        catch (Exception)
        {
            // Persisting settings must never take the app down; the in-memory value stays authoritative.
        }
    }
}
