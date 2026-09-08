using Overline.App.Localization;

namespace Overline.App;

/// <summary>App-level UI settings, persisted under the "ui" settings section.</summary>
public sealed class UiSettings
{
    /// <summary>UI language; Auto follows the system language.</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Auto;
}