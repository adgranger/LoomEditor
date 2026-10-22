using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.WordHighlighter;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.WordHighlighter;

// New tests: wordHighlighter.test.ts has no test of the navigation actions.
public class WordHighlighterFeatureTests
{
    // "word" occurs at (1,1-5), (1,7-11) and (2,1-5).
    private const string Text = "word  word\nword";

    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(50);

    private readonly ManualEditorScheduler _scheduler = new();

    private EditorContext Create()
    {
        var context = new EditorContext(_scheduler);
        context.SetText(Text);
        context.ViewModel.SetHasFocus(true);
        new WordHighlighterFeature().Attach(context);
        return context;
    }

    private EditorContext CreateHighlighted()
    {
        EditorContext context = Create();
        context.SetSelections([Selection.Collapsed(new Position(1, 2))]);
        _scheduler.AdvanceBy(Delay);
        return context;
    }

    private static int HighlightCount(EditorContext context) =>
        context.Model.GetAllDecorations().Count(d => d.OwnerId == context.Id && d.Options.ClassName == "wordHighlightText") / 2;

    private static void Run(EditorContext context, string id) => Assert.True(context.Commands.Execute(id), id);

    [Fact]
    public void Next_MovesToTheNextOccurrence_AndWraps()
    {
        EditorContext context = CreateHighlighted();

        Run(context, WordHighlighterCommands.Next);
        Assert.Equal(new Position(1, 7), context.Selection.Position);

        Run(context, WordHighlighterCommands.Next);
        Assert.Equal(new Position(2, 1), context.Selection.Position);

        Run(context, WordHighlighterCommands.Next);
        Assert.Equal(new Position(1, 1), context.Selection.Position);
    }

    [Fact]
    public void Prev_MovesToThePreviousOccurrence_AndWraps()
    {
        EditorContext context = CreateHighlighted();

        Run(context, WordHighlighterCommands.Prev);
        Assert.Equal(new Position(2, 1), context.Selection.Position);

        Run(context, WordHighlighterCommands.Prev);
        Assert.Equal(new Position(1, 7), context.Selection.Position);

        Run(context, WordHighlighterCommands.Prev);
        Assert.Equal(new Position(1, 1), context.Selection.Position);
    }

    [Fact]
    public void Next_WithoutHighlights_DoesNothing()
    {
        EditorContext context = Create();

        Run(context, WordHighlighterCommands.Next);

        Assert.Equal(new Position(1, 1), context.Selection.Position);
    }

    [Fact]
    public void Trigger_AfterTyping_HighlightsAfter250Ms()
    {
        EditorContext context = CreateHighlighted();
        context.Model.ApplyEdits([TextEdit.Insert(new Position(2, 5), " x")]);
        Assert.Equal(0, HighlightCount(context));

        Run(context, WordHighlighterCommands.Trigger);

        _scheduler.AdvanceBy(Delay + TimeSpan.FromMilliseconds(249));
        Assert.Equal(0, HighlightCount(context));

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(3, HighlightCount(context));
    }

    [Fact]
    public void Next_DoesNotClearTheHighlights()
    {
        EditorContext context = CreateHighlighted();

        Run(context, WordHighlighterCommands.Next);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1));

        Assert.Equal(3, HighlightCount(context));
    }

    [Fact]
    public void Windows_F7_Next_And_ShiftF7_Prev()
    {
        KeybindingRule next = DefaultKeybindings.Windows.Single(r => r.CommandId == WordHighlighterCommands.Next);
        KeybindingRule prev = DefaultKeybindings.Windows.Single(r => r.CommandId == WordHighlighterCommands.Prev);

        Assert.Equal(new Keybinding(KeyModifiers.None, KeyCode.F7), next.Keybinding);
        Assert.Equal(new Keybinding(KeyModifiers.Shift, KeyCode.F7), prev.Keybinding);
    }

    [Fact]
    public void EditorFeatures_Default_ContainsWordHighlighter()
    {
        Assert.Contains(EditorFeatures.Default, factory => factory() is WordHighlighterFeature);
    }
}
