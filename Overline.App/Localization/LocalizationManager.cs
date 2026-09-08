using System.ComponentModel;
using System.Globalization;

namespace Overline.App.Localization;

/// <summary>UI language selection. Auto resolves to the system UI language.</summary>
public enum AppLanguage
{
    Auto = 0,
    English = 1,
    French = 2,
}

/// <summary>
/// Lightweight FR/EN localization for the app. Strings are resolved through an
/// indexer (<c>Loc["Key"]</c>) so WPF bindings of the form
/// <c>{Binding Loc[Key]}</c> refresh automatically when the language changes
/// (a <c>PropertyChanged(null)</c> is raised, which WPF treats as "everything
/// changed").
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private AppLanguage _language = AppLanguage.Auto;

    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }

    /// <summary>True when the effective language is French (Auto resolves to the system language).</summary>
    public bool IsFrench => Language switch
    {
        AppLanguage.French => true,
        AppLanguage.English => false,
        _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase),
    };

    /// <summary>Resolve a key to the effective language; unknown keys return the key itself.</summary>
    public string this[string key] =>
        Table.TryGetValue(key, out var pair) ? (IsFrench ? pair.Fr : pair.En) : key;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly Dictionary<string, (string En, string Fr)> Table = new(StringComparer.Ordinal)
    {
        ["HeaderSubtitle"] = ("v0.1.0 · preparing v1", "v0.1.0 · préparation de la v1"),

        ["PreviewTitle"] = ("💬 Live preview", "💬 Aperçu en direct"),
        ["PreviewHint"] = ("Sent over OSC to VRChat at 127.0.0.1:9000 when modules are running. Enable OSC in VRChat to see it in-game.",
                           "Envoyé par OSC vers VRChat à 127.0.0.1:9000 quand les modules tournent. Active l'OSC dans VRChat pour le voir en jeu."),
        ["EmptyPreview"] = ("— nothing to show —", "— rien à afficher —"),
        ["CharCount"] = ("{0} / 144", "{0} / 144"),

        ["ModulesTitle"] = ("🧩 Modules", "🧩 Modules"),

        ["TimeName"] = ("Time", "Heure"),
        ["TimeDescription"] = ("Your local time and time zone, with clock-face emoji.",
                               "Ton heure locale et ton fuseau horaire, avec emoji horloge."),

        ["MediaName"] = ("Media", "Média"),
        ["MediaDescription"] = ("Currently playing media on this PC — title, artist and playback time.",
                                "Le média en cours sur ce PC — titre, artiste et temps de lecture."),

        ["SettingsShowDate"] = ("Show date", "Afficher la date"),
        ["SettingsShowClockEmoji"] = ("Show clock emoji", "Afficher l'emoji horloge"),
        ["SettingsShowTimeZone"] = ("Show time zone", "Afficher le fuseau horaire"),
        ["SettingsUse24Hour"] = ("24-hour clock", "Horloge 24 h"),
        ["SettingsShowSeconds"] = ("Show seconds", "Afficher les secondes"),

        ["SettingsShowMusicEmoji"] = ("Show music emoji", "Afficher l'emoji musique"),
        ["SettingsShowTitle"] = ("Show title", "Afficher le titre"),
        ["SettingsShowArtist"] = ("Show artist", "Afficher l'artiste"),
        ["SettingsShowTime"] = ("Show playback time", "Afficher le temps de lecture"),
        ["SettingsShowProgressBar"] = ("Show progress bar", "Afficher la barre de progression"),

        ["StartAll"] = ("▶ Start modules", "▶ Démarrer les modules"),
        ["StopAll"] = ("■ Stop modules", "■ Arrêter les modules"),

        ["StatusReady"] = ("Ready.", "Prêt."),
        ["StatusRunning"] = ("Modules running.", "Modules en marche."),
        ["StatusStopped"] = ("Modules stopped.", "Modules arrêtés."),
        ["StatusError"] = ("Error: {0}", "Erreur : {0}"),

        ["StateRunning"] = ("Running", "En marche"),
        ["StateStopped"] = ("Stopped", "Arrêté"),
        ["StateStarting"] = ("Starting…", "Démarrage…"),
        ["StateStopping"] = ("Stopping…", "Arrêt…"),
        ["StateFaulted"] = ("Error", "Erreur"),

        ["OscReady"] = ("OSC ready · 127.0.0.1:9000", "OSC prêt · 127.0.0.1:9000"),
        ["OscSent"] = ("OSC sent ✓ {0}", "OSC envoyé ✓ {0}"),
        ["OscFailed"] = ("OSC send failed ✗", "Échec d'envoi OSC ✗"),

        ["WindowMinimize"] = ("Minimize", "Réduire"),
        ["WindowMaximize"] = ("Maximize", "Agrandir"),
        ["WindowClose"] = ("Close", "Fermer"),

        ["PageDashboard"] = ("Dashboard", "Tableau de bord"),
        ["PageModules"] = ("Modules", "Modules"),
        ["PagePrivacy"] = ("Privacy", "Confidentialité"),
        ["PageSettings"] = ("Settings", "Paramètres"),

        ["PrivacyTitle"] = ("🔒 Privacy", "🔒 Confidentialité"),
        ["PrivacyDescription"] = ("Modules must ask before touching your data. Revoke a consent at any time — the affected module stops using it immediately.",
                                   "Les modules doivent demander ton accord avant d'accéder à tes données. Tu peux retirer un accord à tout moment — le module concerné cesse de l'utiliser immédiatement."),
        ["MediaConsentTitle"] = ("🎵 Media session", "🎵 Session média"),
        ["MediaConsentDescription"] = ("Let the Media module read the title, artist and playback time of the currently playing track.",
                                        "Autoriser le module Média à lire le titre, l'artiste et le temps de lecture de la piste en cours."),
        ["ConsentMissingHint"] = ("⚠️ Consent required — the Media module stays silent until you approve it on the Privacy page.",
                                   "⚠️ Accord requis — le module Média reste muet tant que tu ne l'approuves pas dans Confidentialité."),

        ["SettingsTitle"] = ("⚙️ Settings", "⚙️ Paramètres"),
        ["LanguageLabel"] = ("Language", "Langue"),
        ["LanguageHint"] = ("Auto follows your system language.", "Auto suit la langue de ton système."),
        ["AboutTitle"] = ("About", "À propos"),
        ["AboutDescription"] = ("Your VRChat chatbox, rebuilt — one line above your head, assembled from independent modules.",
                                 "Ta chatbox VRChat, reconstruite — une ligne au-dessus de ta tête, assemblée à partir de modules indépendants."),
    };
}