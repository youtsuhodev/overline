using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overline.App.Services;
using Overline.Core.Modules;
using Overline.Core.Settings;
using Overline.Modules.Media;
using Overline.Modules.Time;

namespace Overline.App;

/// <summary>
/// View-model of the main window: assembles the live preview line and
/// exposes module toggles and per-module settings.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IChatboxComposer _composer;
    private readonly IModuleRegistry _registry;
    private readonly TimeModule _timeModule;
    private readonly MediaModule _mediaModule;
    private readonly ISettingsHolder<TimeSettings> _timeSettings;
    private readonly ISettingsHolder<MediaSettings> _mediaSettings;
    private readonly System.Timers.Timer _previewTimer;

    public MainWindowViewModel(
        IChatboxComposer composer,
        IModuleRegistry registry,
        TimeModule timeModule,
        MediaModule mediaModule,
        ISettingsHolder<TimeSettings> timeSettings,
        ISettingsHolder<MediaSettings> mediaSettings)
    {
        _composer = composer;
        _registry = registry;
        _timeModule = timeModule;
        _mediaModule = mediaModule;
        _timeSettings = timeSettings;
        _mediaSettings = mediaSettings;

        _previewTimer = new System.Timers.Timer(1000)
        {
            AutoReset = true,
        };
        _previewTimer.Elapsed += (_, _) => RefreshPreview();
        _previewTimer.Start();

        StartStopCommand = new AsyncRelayCommand(StartStopAsync);
    }

    public string AppTitle => "Overline";

    [ObservableProperty]
    private string _previewLine = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private bool _timeModuleEnabled;

    [ObservableProperty]
    private bool _mediaModuleEnabled;

    partial void OnTimeModuleEnabledChanged(bool value)
        => _ = value ? _timeModule.StartAsync() : _timeModule.StopAsync();

    partial void OnMediaModuleEnabledChanged(bool value)
        => _ = value ? _mediaModule.StartAsync() : _mediaModule.StopAsync();

    public TimeSettings TimeSettings => _timeSettings.Value;
    public MediaSettings MediaSettings => _mediaSettings.Value;

    public IAsyncRelayCommand StartStopCommand { get; }

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

            StatusText = anyRunning ? "Modules stopped." : "Modules running.";
            TimeModuleEnabled = _timeModule.State == ModuleState.Running;
            MediaModuleEnabled = _mediaModule.State == ModuleState.Running;
            RefreshPreview();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void RefreshPreview()
    {
        var line = _composer.ComposeAndSend();
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            PreviewLine = string.IsNullOrEmpty(line) ? "— nothing to show —" : line;
        });
    }

    public void Dispose()
    {
        _previewTimer.Dispose();
    }
}