namespace Overline.Core.Osc;

/// <summary>
/// Anything that can contribute a segment to the chatbox line. Modules implement
/// this so the composer stays generic instead of knowing each module type.
/// </summary>
public interface ISegmentSource
{
    /// <summary>
    /// The current segment, or null/empty when there is nothing to show
    /// (module stopped, no media, etc.).
    /// </summary>
    OscSegment? BuildSegment();
}