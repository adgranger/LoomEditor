using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.WordHighlighter;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using Highlighter = LoomEditor.View.Features.WordHighlighter.WordHighlighter;

namespace LoomEditor.View.Tests.Features.WordHighlighter;

// New tests: the screen reader alert of wordHighlighter.ts moveNext / moveBack has no upstream test.
public class WordHighlighterAnnouncementTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(50);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, Highlighter Highlighter) Create(string text, RecordingAnnouncer? announcer)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(text);
        context.ViewModel.SetHasFocus(true);
        var highlighter = new Highlighter(context.ViewModel, _scheduler, EditorWordHighlightOptions.Default, announcer);
        context.SetSelections([Selection.Collapsed(new Position(1, 2))]);
        _scheduler.AdvanceBy(Delay);
        return (context, highlighter);
    }

    [Fact]
    public void MoveNext_AnnouncesLineAndPosition()
    {
        var recorder = new RecordingAnnouncer();
        (EditorContext context, Highlighter highlighter) = Create("foo bar\nfoo baz", recorder);

        highlighter.MoveNext();

        Assert.Equal(2, context.Selection.Position.LineNumber);
        Assert.Equal(["foo baz, 2 of 2 for 'foo'"], recorder.Messages);
    }

    [Fact]
    public void MoveBack_Wraps_AnnouncesTheLastOccurrence()
    {
        var recorder = new RecordingAnnouncer();
        (_, Highlighter highlighter) = Create("foo\nfoo\nfoo", recorder);

        highlighter.MoveBack();

        Assert.Equal(["foo, 3 of 3 for 'foo'"], recorder.Messages);
    }

    [Fact]
    public void MoveNext_WithoutAnnouncer_Works()
    {
        (EditorContext context, Highlighter highlighter) = Create("foo bar\nfoo baz", null);

        highlighter.MoveNext();

        Assert.Equal(new Position(2, 1), context.Selection.Position);
    }

    [Fact]
    public void Feature_AnnouncesThroughTheContextAnnouncer()
    {
        var context = new EditorContext(_scheduler);
        context.SetText("foo bar\nfoo baz");
        context.ViewModel.SetHasFocus(true);
        new WordHighlighterFeature().Attach(context);
        var recorder = new RecordingAnnouncer();
        context.Announcer = recorder;
        context.SetSelections([Selection.Collapsed(new Position(1, 2))]);
        _scheduler.AdvanceBy(Delay);

        Assert.True(context.Commands.Execute(WordHighlighterCommands.Next));

        Assert.Equal(["foo baz, 2 of 2 for 'foo'"], recorder.Messages);
    }
}
