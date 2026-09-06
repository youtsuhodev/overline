namespace Overline.Modules.Media;

/// <summary>Immutable snapshot of the currently playing media.</summary>
public sealed record MediaSnapshot(
    string Title,
    string Artist,
    string? Album,
    bool IsPlaying,
    TimeSpan Position,
    TimeSpan Duration);