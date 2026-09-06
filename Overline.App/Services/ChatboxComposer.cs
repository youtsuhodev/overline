using Overline.App.Osc;
using Overline.Core.Osc;

namespace Overline.App.Services;

/// <summary>
/// Pulls the current segment from each registered source, assembles the
/// 144-character line and sends it over OSC.
/// </summary>
public interface IChatboxComposer : IDisposable
{
    /// <summary>Assemble and send one line now. Returns the sent text (for preview/tests).</summary>
    string ComposeAndSend();
}

public sealed class ChatboxComposer : IChatboxComposer
{
    private readonly IReadOnlyList<ISegmentSource> _sources;
    private readonly IOscSender _oscSender;

    public ChatboxComposer(IEnumerable<ISegmentSource> sources, IOscSender oscSender)
    {
        _sources = sources.ToList();
        _oscSender = oscSender;
    }

    public string ComposeAndSend()
    {
        var segments = _sources
            .Select(s => s.BuildSegment())
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();

        var result = ChatboxLineBuilder.Build(segments);
        if (result.Line.Length > 0)
        {
            _oscSender.SendChatbox(new OscChatboxMessage(result.Line));
        }

        return result.Line;
    }

    public void Dispose()
    {
        // Nothing to release; sender lifetime is owned by DI.
    }
}