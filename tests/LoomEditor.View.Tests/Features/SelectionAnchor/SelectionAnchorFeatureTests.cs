using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.SelectionAnchor;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Features.SelectionAnchor;

// New tests: anchorSelect.ts has no test in Monaco.
public class SelectionAnchorFeatureTests
{
    private static readonly KeybindingContext Focused = new(TextInputFocus: true, EditorTextFocus: true, EditorFocus: true);

    private static EditorContext Create(RecordingAnnouncer? announcer = null)
    {
        var context = new EditorContext();
        context.SetText("one two\nthree four\nfive");
        if (announcer is not null)
        {
            context.Announcer = announcer;
        }

        new SelectionAnchorFeature().Attach(context);
        return context;
    }

    private static void Run(EditorContext context, string id) => Assert.True(context.Commands.Execute(id), id);

    private static void MoveTo(EditorContext context, int line, int column) => context.Selection = Selection.Collapsed(new Position(line, column));

    private static List<Range> Anchors(EditorContext context) =>
        [.. context.Model.GetAllDecorations().Where(d => d.Options.ClassName == "selection-anchor").Select(d => d.Range)];

    [Fact]
    public void SetAnchor_CreatesDecorationAtCaret()
    {
        EditorContext context = Create();
        MoveTo(context, 2, 4);

        Run(context, SelectionAnchorCommands.SetSelectionAnchor);

        Assert.Equal([new Range(2, 4, 2, 4)], Anchors(context));
    }

    [Fact]
    public void SetAnchor_Twice_ReplacesTheFirst()
    {
        EditorContext context = Create();
        MoveTo(context, 1, 2);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        MoveTo(context, 3, 1);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);

        Assert.Equal([new Range(3, 1, 3, 1)], Anchors(context));
    }

    [Fact]
    public void SelectFromAnchorToCursor_SelectsAndCancels()
    {
        EditorContext context = Create();
        MoveTo(context, 1, 2);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        MoveTo(context, 2, 3);

        Run(context, SelectionAnchorCommands.SelectFromAnchorToCursor);

        Assert.Equal(new Selection(1, 2, 2, 3), context.Selection);
        Assert.Empty(Anchors(context));
        Assert.False(context.SelectionAnchorSet);
    }

    [Fact]
    public void SelectFromAnchorToCursor_AnchorBeforeCaret_Forward()
    {
        EditorContext context = Create();
        MoveTo(context, 1, 2);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        MoveTo(context, 1, 6);
        Run(context, SelectionAnchorCommands.SelectFromAnchorToCursor);

        Assert.Equal(SelectionDirection.LeftToRight, context.Selection.Direction);
    }

    [Fact]
    public void SelectFromAnchorToCursor_AnchorAfterCaret_Backward()
    {
        EditorContext context = Create();
        MoveTo(context, 1, 6);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        MoveTo(context, 1, 2);
        Run(context, SelectionAnchorCommands.SelectFromAnchorToCursor);

        Assert.Equal(new Selection(1, 6, 1, 2), context.Selection);
        Assert.Equal(SelectionDirection.RightToLeft, context.Selection.Direction);
    }

    [Fact]
    public void GoToAnchor_MovesCaret()
    {
        EditorContext context = Create();
        MoveTo(context, 2, 5);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        MoveTo(context, 3, 2);

        Run(context, SelectionAnchorCommands.GoToSelectionAnchor);

        Assert.Equal(Selection.Collapsed(new Position(2, 5)), context.Selection);
        Assert.Single(Anchors(context));
    }

    [Fact]
    public void Anchor_FollowsEditsBeforeIt()
    {
        EditorContext context = Create();
        MoveTo(context, 2, 5);
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);

        context.Model.ApplyEdits([new(new Range(1, 1, 1, 1), "ab\n")]);

        Run(context, SelectionAnchorCommands.GoToSelectionAnchor);
        Assert.Equal(new Position(3, 5), context.Selection.Position);
    }

    [Fact]
    public void Cancel_RemovesDecoration_AndContextKey()
    {
        EditorContext context = Create();
        Run(context, SelectionAnchorCommands.SetSelectionAnchor);

        Run(context, SelectionAnchorCommands.CancelSelectionAnchor);

        Assert.Empty(Anchors(context));
        Assert.False(context.SelectionAnchorSet);
    }

    [Fact]
    public void ContextKey_TrueOnlyWhileAnchorSet()
    {
        EditorContext context = Create();
        Assert.False(context.SelectionAnchorSet);
        Assert.False(context.Commands.Execute(SelectionAnchorCommands.CancelSelectionAnchor));
        Assert.False(context.Commands.Execute(SelectionAnchorCommands.GoToSelectionAnchor));

        Run(context, SelectionAnchorCommands.SetSelectionAnchor);
        Assert.True(context.SelectionAnchorSet);

        Run(context, SelectionAnchorCommands.CancelSelectionAnchor);
        Assert.False(context.SelectionAnchorSet);
    }

    [Fact]
    public void Escape_WithAnchor_ResolvesToCancel() =>
        Assert.Equal(
            SelectionAnchorCommands.CancelSelectionAnchor,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Escape), Focused with { SelectionAnchorSet = true }));

    [Fact]
    public void Escape_WithoutAnchor_DoesNotResolveToCancel() =>
        Assert.NotEqual(
            SelectionAnchorCommands.CancelSelectionAnchor,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Escape), Focused));

    [Fact]
    public void Escape_WithAnchorAndSuggestVisible_HidesSuggestFirst() =>
        Assert.NotEqual(
            SelectionAnchorCommands.CancelSelectionAnchor,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Escape), Focused with { SelectionAnchorSet = true, SuggestWidgetVisible = true }));

    [Fact]
    public void Announces_AnchorSet()
    {
        var announcer = new RecordingAnnouncer();
        EditorContext context = Create(announcer);
        MoveTo(context, 2, 4);

        Run(context, SelectionAnchorCommands.SetSelectionAnchor);

        Assert.Equal(["Anchor set at 2:4"], announcer.Messages);
    }
}
