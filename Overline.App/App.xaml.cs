using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Overline.App.Osc;
using Overline.App.Services;
using Overline.Core.Modules;
using Overline.Core.Osc;
using Overline.Core.Privacy;
using Overline.Core.Settings;
using Overline.Core.Time;
using Overline.Modules.Media;
using Overline.Modules.Time;

namespace Overline.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Information));

        // Time
        services.AddSingleton<IClock>(SystemClock.Instance);

        // Settings (one JSON file per section under %APPDATA%/Overline/settings)
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        // Privacy
        services.AddSingleton<IPrivacyConsentService, PrivacyConsentService>();

        // OSC
        services.AddSingleton<UdpOscSender>();
        services.AddSingleton<IOscSender>(sp => sp.GetRequiredService<UdpOscSender>());

        // Modules
        services.AddSingleton<TimeModule>();
        services.AddSingleton<MediaModule>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddSingleton<ISettingsHolder<TimeSettings>>(sp =>
            new SettingsHolder<TimeSettings>(sp.GetRequiredService<ISettingsStore>(), "time"));
        services.AddSingleton<ISettingsHolder<MediaSettings>>(sp =>
            new SettingsHolder<MediaSettings>(sp.GetRequiredService<ISettingsStore>(), "media"));

        // Media session monitor (Windows-only, gated behind MediaSession consent)
        services.AddSingleton<IMediaSessionMonitor, WindowsMediaSessionMonitor>();

        // Sources: every module that can contribute a chatbox segment
        services.AddSingleton<ISegmentSource>(sp => sp.GetRequiredService<TimeModule>());
        services.AddSingleton<ISegmentSource>(sp => sp.GetRequiredService<MediaModule>());

        // Composer
        services.AddSingleton<IChatboxComposer, ChatboxComposer>();

        // Main window view-model
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        // Register modules and start the ones the user enabled.
        var registry = Services.GetRequiredService<IModuleRegistry>();
        registry.Register(Services.GetRequiredService<TimeModule>());
        registry.Register(Services.GetRequiredService<MediaModule>());

        var window = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainWindowViewModel>(),
        };
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
