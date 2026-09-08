using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overline.App.Localization;
using Overline.App.Osc;
using Overline.App.Services;
using Overline.Core.Modules;
using Overline.Core.Settings;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Overline.Core.Privacy;
using Overline.Modules.Media;
using Overline.Modules.Time;

namespace Overline.App;

/// <summary>Pages reachable from the sidebar.</summary>
public enum AppPage
{
    Dashboard,
    Modules,
    Privacy,
    Settings,
}

/// <summary>One sidebar entry: icon, target page and localized label.</summary>
public sealed record NavItem(string Icon, AppPage Page, string Label);

/// <summary>
/// View-model of the main window: assembles the live preview line, exposes
/// module toggles and per-module settings, and drives the OSC / character
/// budget status indicators. All user-facing text goes through
/// <see cref="LocalizationManager"/> so the UI switches language live.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private const int ChatboxLimit = 144;
    private const int NearLimitThreshold = 120;

    private readonly IChatboxComposer _composer;
    private readonly IModuleRegistry _registry;
    private readonly TimeModule _timeModule;
    private readonly MediaModule _mediaModule;
    private readonly ISettingsHolder<TimeSettings> _timeSettings;
    private readonly ISettingsHolder<MediaSettings> _mediaSettings;
    private readonly ISettingsHolder<UiSettings> _uiSettings;
    private readonly IOscSender _oscSender;
    private readonly IPrivacyConsentService _consent;
    private readonly System.Timers.Timer _previewTimer;
    private readonly Dispatcher _dispatcher;
    private bool _lastOscSuccess;
    private DateTimeOffset? _lastOscTime;

    public MainWindowViewModel(
        IChatboxComposer composer,
        IModuleRegistry registry,
        TimeModule timeModule,
        MediaModule mediaModule,
        ISettingsHolder<TimeSettings> timeSettings,
        ISettingsHolder<MediaSettings> mediaSettings,
        ISettingsHolder<UiSettings> uiSettings,
        IOscSender oscSender,
        IPrivacyConsentService consent)
    {
        _composer = composer;
        _registry = registry;
        _timeModule = timeModule;
        _mediaModule = mediaModule;
        _timeSettings = timeSettings;
        _mediaSettings = mediaSettings;
        _uiSettings = uiSettings;
        _oscSender = oscSender;
        _consent = consent;
        _dispatcher = System.Windows.Application.Current!.Dispatcher;

        // Apply the persisted language before the first bindings resolve.
        LocalizationManager.Instance.Language = _uiSettings.Value.Language;
        LocalizationManager.Instance.PropertyChanged += (_, _) => UpdateLocalizedTexts();

        _timeModule.StateChanged += (_, _) => _dispatcher.Invoke(UpdateModuleStates);
        _mediaModule.StateChanged += (_, _) => _dispatcher.Invoke(UpdateModuleStates);
        _oscSender.SendCompleted += OnOscSendCompleted;

        _previewTimer = new System.Timers.Timer(1000)
        {
            AutoReset = true,
        };
        _previewTimer.Elapsed += (_, _) => RefreshPreview();
        _previewTimer.Start();

        StartStopCommand = new AsyncRelayCommand(StartStopAsync);

        UpdateLocalizedTexts();
        UpdateModuleStates();
        RefreshPreview();
    }

    public LocalizationManager Loc => LocalizationManager.Instance;

    public string AppTitle => "Overline";

    [ObservableProperty]
    private string _headerSubtitle = string.Empty;

    [ObservableProperty]
    private string _previewLine = string.Empty;

    [ObservableProperty]
    private string _previewCounterText = string.Empty;

    [ObservableProperty]
    private int _previewLength;

    [ObservableProperty]
    private bool _previewNearLimit;

    [ObservableProperty]
    private bool _previewOverBudget;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _oscStatusText = string.Empty;

    [ObservableProperty]
    private bool _hasOscActivity;

    [ObservableProperty]
    private bool _oscLastOk;

    [ObservableProperty]
    private bool _timeModuleEnabled;

    [ObservableProperty]
    private bool _mediaModuleEnabled;

    [ObservableProperty]
    private string _timeModulePillState = "stopped";

    [ObservableProperty]
    private string _timeModuleStateText = string.Empty;

    [ObservableProperty]
    private string _mediaModulePillState = "stopped";

    [ObservableProperty]
    private string _mediaModuleStateText = string.Empty;

    [ObservableProperty]
    private bool _anyModuleRunning;

    [ObservableProperty]
    private string _startStopText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<NavItem> _navItems = [];

    [ObservableProperty]
    private AppPage _currentPage = AppPage.Dashboard;

    [ObservableProperty]
    private bool _mediaConsentMissing;

    /// <summary>Index of the selected language in the header ComboBox (Auto/English/French).</summary>
    public int LanguageIndex
    {
        get => (int)LocalizationManager.Instance.Language;
        set
        {
            var language = (AppLanguage)value;
            if (LocalizationManager.Instance.Language == language)
            {
                return;
            }

            LocalizationManager.Instance.Language = language;
            _uiSettings.Save();
            OnPropertyChanged();
        }
    }

    partial void OnTimeModuleEnabledChanged(bool value)
        => _ = value ? _timeModule.StartAsync() : _timeModule.StopAsync();

    partial void OnMediaModuleEnabledChanged(bool value)
        => _ = value ? _mediaModule.StartAsync() : _mediaModule.StopAsync();

    public TimeSettings TimeSettings => _timeSettings.Value;
    public MediaSettings MediaSettings => _mediaSettings.Value;

    /// <summary>Media-session consent switch, persisted by the consent service.</summary>
    public bool MediaConsent
    {
        get => _consent.IsApproved(PrivacyHooks.MediaSession);
        set
        {
            _consent.SetApproved(PrivacyHooks.MediaSession, value);
            OnPropertyChanged();
            UpdateModuleStates();
        }
    }

    public IAsyncRelayCommand StartStopCommand { get; }

    /// <summary>Persist a settings section after a UI edit ("time" or "media").</summary>
    public void SaveSettings(string section)
    {
        switch (section)
        {
            case "time":
                _timeSettings.Save();
                break;
            case "media":
                _mediaSettings.Save();
                break;
        }
    }

    private async Task StartStopAsync()
    {
        try
        {
            var anyRunning = _registry.Modules.Any(m => m.State == ModuleState.Running);
            foreach (var module in _registry.Modules)
            {
                if (anyRunning)
                {
                    await module.StopAsync();
                }
                else
                {
                    await module.StartAsync();
                }
            }

            UpdateModuleStates();
            if (anyRunning)
            {
                StatusText = Loc["StatusStopped"];
                OscStatusText = Loc["OscReady"];
                HasOscActivity = false;
            }
            else
            {
                StatusText = Loc["StatusRunning"];
            }

            RefreshPreview();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(Loc["StatusError"], ex.Message);
        }
    }

    private void UpdateLocalizedTexts()
    {
        HeaderSubtitle = Loc["HeaderSubtitle"];
        NavItems =
        [
            new NavItem("🏠", AppPage.Dashboard, Loc["PageDashboard"]),
            new NavItem("🧩", AppPage.Modules, Loc["PageModules"]),
            new NavItem("🔒", AppPage.Privacy, Loc["PagePrivacy"]),
            new NavItem("⚙️", AppPage.Settings, Loc["PageSettings"]),
        ];
        OscStatusText = HasOscActivity
            ? string.Format(Loc[_lastOscSuccess ? "OscSent" : "OscFailed"], _lastOscTime?.ToLocalTime().ToString("HH:mm:ss") ?? string.Empty)
            : Loc["OscReady"];
        StartStopText = Loc[AnyModuleRunning ? "StopAll" : "StartAll"];
        StatusText = AnyModuleRunning ? Loc["StatusRunning"] : Loc["StatusReady"];
        RefreshPreview();
    }

    private void UpdateModuleStates()
    {
        TimeModulePillState = MapState(_timeModule.State);
        TimeModuleStateText = StateText(_timeModule.State);
        MediaModulePillState = MapState(_mediaModule.State);
        MediaModuleStateText = StateText(_mediaModule.State);
        MediaConsentMissing = !_consent.IsApproved(PrivacyHooks.MediaSession);

        AnyModuleRunning = _registry.Modules.Any(m => m.State == ModuleState.Running);
        StartStopText = Loc[AnyModuleRunning ? "StopAll" : "StartAll"];
    }

    private void OnOscSendCompleted(object? sender, OscSendResultEventArgs e)
        => _dispatcher.Invoke(() =>
        {
            HasOscActivity = true;
            OscLastOk = e.Success;
            _lastOscSuccess = e.Success;
            _lastOscTime = e.Timestamp;
            OscStatusText = string.Format(Loc[e.Success ? "OscSent" : "OscFailed"], e.Timestamp.ToLocalTime().ToString("HH:mm:ss"));
        });

    private void RefreshPreview()
    {
        var line = _composer.ComposeAndSend();
        _dispatcher.Invoke(() =>
        {
            PreviewLine = string.IsNullOrEmpty(line) ? Loc["EmptyPreview"] : line;
            PreviewLength = line.Length;
            PreviewNearLimit = line.Length >= NearLimitThreshold && line.Length <= ChatboxLimit;
            PreviewOverBudget = line.Length > ChatboxLimit;
            PreviewCounterText = string.Format(Loc["CharCount"], line.Length);
        });
    }

    private static string MapState(ModuleState state) => state switch
    {
        ModuleState.Running => "running",
        ModuleState.Faulted => "faulted",
        ModuleState.Starting or ModuleState.Stopping => "busy",
        _ => "stopped",
    };

    private string StateText(ModuleState state) => state switch
    {
        ModuleState.Running => Loc["StateRunning"],
        ModuleState.Faulted => Loc["StateFaulted"],
        ModuleState.Starting => Loc["StateStarting"],
        ModuleState.Stopping => Loc["StateStopping"],
        _ => Loc["StateStopped"],
    };

    public void Dispose()
    {
        _previewTimer.Dispose();
        _oscSender.SendCompleted -= OnOscSendCompleted;
    }
}