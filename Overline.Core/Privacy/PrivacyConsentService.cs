using Overline.Core.Settings;

namespace Overline.Core.Privacy;

/// <summary>Well-known consent hooks. Modules may define their own via the string constructor.</summary>
public static class PrivacyHooks
{
    public const string InternetAccess = "internet";
    public const string HardwareSensors = "hardware";
    public const string WindowTitles = "window-titles";
    public const string MediaSession = "media-session";
    public const string VrChatLog = "vrc-log";
}

/// <summary>
/// Tracks the user's explicit consent for each data-access hook. Modules must
/// check consent before touching any sensitive resource.
/// </summary>
public interface IPrivacyConsentService
{
    /// <summary>True when the user approved (or previously approved) the hook.</summary>
    bool IsApproved(string hook);

    /// <summary>Record the user's decision for a hook.</summary>
    void SetApproved(string hook, bool approved);

    /// <summary>All hooks with their current decision, for the Privacy page.</summary>
    IReadOnlyDictionary<string, bool> Snapshot();
}

/// <summary>Consent store persisted via the settings store (survives restarts).</summary>
public sealed class PrivacyConsentService : IPrivacyConsentService
{
    private const string StoreKey = "privacy";

    private sealed class ConsentState
    {
        public Dictionary<string, bool> Decisions { get; set; } = [];
    }

    private readonly ISettingsStore _settings;
    private readonly object _gate = new();
    private ConsentState _state;

    public PrivacyConsentService(ISettingsStore settings)
    {
        _settings = settings;
        _state = settings.Get<ConsentState>(StoreKey);
    }

    public bool IsApproved(string hook)
    {
        lock (_gate)
        {
            return _state.Decisions.TryGetValue(hook, out var approved) && approved;
        }
    }

    public void SetApproved(string hook, bool approved)
    {
        lock (_gate)
        {
            _state.Decisions[hook] = approved;
            _settings.Save(StoreKey, _state);
        }
    }

    public IReadOnlyDictionary<string, bool> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, bool>(_state.Decisions);
        }
    }
}
