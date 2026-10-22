using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.WordHighlighter;
using LoomEditor.View.Tests.Features.TestHelpers;
using Highlighter = LoomEditor.View.Features.WordHighlighter.WordHighlighter;

namespace LoomEditor.View.Tests.Features.WordHighlighter;

// New tests: wordHighlighter.test.ts needs the provider registries (phase 8); these tests cover the textual path of the class.
public class WordHighlighterTests
{
    // "word" occurs at (1,1-5), (1,7-11) and (2,1-5); column 6 is between two spaces.
    private const string Text = "word  word\nword";

    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(50);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, Highlighter Highlighter) Create(EditorWordHighlightOptions? options = null)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(Text);
        context.ViewModel.SetHasFocus(true);
        return (context, new Highlighter(context.ViewModel, _scheduler, options ?? EditorWordHighlightOptions.Default));
    }

    private static Range[] Highlights(EditorContext context) =>
        [.. context.Model.GetAllDecorations()
            .Where(d => d.OwnerId == context.Id && d.Options.ClassName == "wordHighlightText")
            .Select(d => d.Range)
            .Distinct()
            .OrderBy(r => r.StartLineNumber)
            .ThenBy(r => r.StartColumn)];

    private static void MoveTo(EditorContext context, int line, int column) =>
        context.SetSelections([Selection.Collapsed(new Position(line, column))]);

    [Fact]
    public void CursorOnWord_After50Ms_HighlightsOccurrences()
    {
        (EditorContext context, _) = Create();

        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Empty(Highlights(context));

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal([new Range(1, 1, 1, 5), new Range(1, 7, 1, 11), new Range(2, 1, 2, 5)], Highlights(context));
    }

    [Fact]
    public void Typing_RemovesHighlights()
    {
        (EditorContext context, _) = Create();
        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);
        Assert.Equal(3, Highlights(context).Length);

        context.ViewModel.Type("x", "keyboard");

        Assert.Empty(Highlights(context));
    }

    [Fact]
    public void CursorLeavesWord_RemovesHighlights()
    {
        (EditorContext context, var highlighter) = Create();
        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);

        MoveTo(context, 1, 6);
        _scheduler.AdvanceBy(Delay);

        Assert.Empty(Highlights(context));
        Assert.False(highlighter.HasDecorations);
    }

    [Fact]
    public void MultiLineSelection_RemovesHighlights()
    {
        (EditorContext context, _) = Create();
        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);

        context.SetSelections([new Selection(1, 2, 2, 2)]);
        _scheduler.AdvanceBy(Delay);

        Assert.Empty(Highlights(context));
    }

    [Fact]
    public void CursorMovesToAnotherOccurrence_KeepsHighlights()
    {
        (EditorContext context, var highlighter) = Create();
        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);
        string[] idsBefore = [.. context.Model.GetAllDecorations().Where(d => d.OwnerId == context.Id).Select(d => d.Id)];

        MoveTo(context, 2, 3);
        _scheduler.AdvanceBy(Delay);

        // The word at the cursor is inside a stored decoration: nothing is computed again.
        Assert.True(highlighter.HasDecorations);
        string[] idsAfter = [.. context.Model.GetAllDecorations().Where(d => d.OwnerId == context.Id).Select(d => d.Id)];
        Assert.Equal(idsBefore, idsAfter);
        Assert.Equal(3, Highlights(context).Length);
    }

    [Fact]
    public void FocusLost_RemovesHighlights()
    {
        (EditorContext context, _) = Create();
        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);

        context.ViewModel.SetHasFocus(false);

        Assert.Empty(Highlights(context));
    }

    [Fact]
    public void OccurrencesHighlightOff_NeverHighlights()
    {
        (EditorContext context, _) = Create(new EditorWordHighlightOptions(OccurrencesHighlight.Off));

        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1));

        Assert.Empty(Highlights(context));
    }

    [Fact]
    public void Restore250_RendersAfter250Ms()
    {
        (EditorContext context, var highlighter) = Create();
        MoveTo(context, 1, 2);
        highlighter.Stop();
        Assert.Empty(Highlights(context));

        highlighter.Restore(TimeSpan.FromMilliseconds(250));
        _scheduler.AdvanceBy(Delay + TimeSpan.FromMilliseconds(249));
        Assert.Empty(Highlights(context));

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(3, Highlights(context).Length);
    }

    [Fact]
    public void OccurrencesHighlightDelay_RendersAfterTheDelay()
    {
        (EditorContext context, _) = Create(new EditorWordHighlightOptions(OccurrencesHighlightDelay: 100));

        MoveTo(context, 1, 2);
        _scheduler.AdvanceBy(Delay);
        Assert.Empty(Highlights(context));

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100));
        Assert.Equal(3, Highlights(context).Length);
    }
}
