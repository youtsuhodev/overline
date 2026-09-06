namespace Overline.Core.Osc;

/// <summary>
/// Priority tiers used when the assembled line overflows the 144-character budget.
/// Lower value = more important = kept longer; <see cref="Optional"/> is dropped first.
/// </summary>
public enum SegmentPriority
{
    /// <summary>Never dropped; if it alone exceeds the budget it gets truncated last.</summary>
    Critical = 0,

    /// <summary>Kept while there is room; dropped only after Low and Optional.</summary>
    High = 1,

    /// <summary>Default importance.</summary>
    Normal = 2,

    /// <summary>Dropped early when space runs out.</summary>
    Low = 3,

    /// <summary>Dropped first when space runs out.</summary>
    Optional = 4,
}

/// <summary>One piece of the chatbox line contributed by a module.</summary>
public sealed record OscSegment(
    string Text,
    SegmentPriority Priority = SegmentPriority.Normal);

/// <summary>
/// Assembles module segments into one chatbox line, dropping the least
/// important segments first when the 144-character budget is exceeded.
/// </summary>
public static class ChatboxLineBuilder
{
    /// <summary>Separator inserted between segments.</summary>
    public const string Separator = "     ";

    public static ChatboxLineResult Build(IReadOnlyList<OscSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var kept = segments
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .OrderBy(s => s.Priority)
            .ToList();

        // Drop from the lowest importance (highest numeric priority) until it fits.
        for (var i = kept.Count - 1; i >= 0; i--)
        {
            var candidate = Join(kept.Take(i + 1));
            if (candidate.Length <= OscChatboxEncoder.MaxChatboxCharacters)
            {
                return new ChatboxLineResult(candidate, Dropped: kept.Skip(i + 1).ToList());
            }
        }

        // Nothing fits: hard-truncate the single most critical segment.
        var critical = kept.Count > 0 ? kept[0] : new OscSegment(string.Empty);
        var truncated = OscChatboxEncoder.TruncateToLimit(critical.Text);
        return new ChatboxLineResult(truncated, Dropped: kept.Skip(1).ToList());
    }

    private static string Join(IEnumerable<OscSegment> segments)
        => string.Join(Separator, segments.Select(s => s.Text.Trim()));

    public sealed record ChatboxLineResult(string Line, IReadOnlyList<OscSegment> Dropped)
    {
        public bool HasDrops => Dropped.Count > 0;
    }
}
