// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts (suite 'Multicursor')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.MultiCursor;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Features.MultiCursor;

// Not ported: the suites 'Multicursor selection' and
// below (73 tests: they need the Find feature, phase 5).
public class MultiCursorTests
{
    private static EditorContext CreateContext(string text)
    {
        var context = new EditorContext();
        context.SetText(text);
        return context;
    }

    // Monaco: 'issue #26393: Multiple cursors + Word wrap'
    [Fact]
    public void Issue26393MultipleCursorsAndWordWrap()
    {
        EditorContext context = CreateContext(new string('a', 20) + "\n" + new string('a', 20));
        context.ViewModel.SetOptions(
            new EditorLayoutOptions { Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 } },
            context.ViewModel.ViewLayoutOptions);
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Commands.Execute(MultiCursorCommands.InsertCursorBelow);

        Assert.Equal(2, context.ViewModel.GetCursorStates().Count);
        Assert.Equal(1, context.ViewModel.GetCursorStates()[0].ViewState.Position.LineNumber);
        Assert.Equal(3, context.ViewModel.GetCursorStates()[1].ViewState.Position.LineNumber);

        // Monaco: editor.setPosition({ lineNumber: 4, column: 1 }): a model position, validated to the end of model line 2 (view line 4)
        context.Selection = new Selection(2, 21, 2, 21);
        context.Commands.Execute(MultiCursorCommands.InsertCursorAbove);
        Assert.Equal(2, context.ViewModel.GetCursorStates().Count);
        Assert.Equal(4, context.ViewModel.GetCursorStates()[0].ViewState.Position.LineNumber);
        Assert.Equal(2, context.ViewModel.GetCursorStates()[1].ViewState.Position.LineNumber);
    }

    // Monaco: 'issue #2205: Multi-cursor pastes in reverse order'
    [Fact]
    public void Issue2205MultiCursorPastesInReverseOrder()
    {
        EditorContext context = CreateContext("abc\ndef");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Selection = new Selection(2, 1, 2, 1);
        context.Commands.Execute(MultiCursorCommands.InsertCursorAbove);
        Assert.Equal(2, context.Selections.Count);

        context.ViewModel.Paste("1\n2", false, ["1", "2"], "test");

        Assert.Equal("1abc", context.Model.GetLineContent(1));
        Assert.Equal("2def", context.Model.GetLineContent(2));
    }

    // Monaco: 'issue #1336: Insert cursor below on last line adds a cursor to the end of the current line'
    [Fact]
    public void Issue1336InsertCursorBelowOnLastLineAddsACursorToTheEndOfTheCurrentLine()
    {
        EditorContext context = CreateContext("abc");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Commands.Execute(MultiCursorCommands.InsertCursorBelow);

        Assert.Single(context.Selections);
    }

    [Fact]
    public void InsertCursorAbove_OnFirstLine_AddsNothing_AndOnLine3AddsACursorOnLine2AtTheSameColumn()
    {
        EditorContext context = CreateContext("aaaa\nbbbb\ncccc");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Selection = new Selection(1, 3, 1, 3);
        context.Commands.Execute(MultiCursorCommands.InsertCursorAbove);
        Assert.Single(context.Selections);

        context.Selection = new Selection(3, 3, 3, 3);
        context.Commands.Execute(MultiCursorCommands.InsertCursorAbove);
        Assert.Equal(2, context.Selections.Count);
        Assert.Contains(new Selection(2, 3, 2, 3), context.Selections);
        Assert.Contains(new Selection(3, 3, 3, 3), context.Selections);
    }

    [Fact]
    public void InsertCursorAtEndOfEachLineSelected_SelectionOverThreeLines_PutsThreeCursorsAtTheLineEnds()
    {
        EditorContext context = CreateContext("aa\nbbb\ncccc");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Selection = new Selection(1, 1, 3, 5);
        context.Commands.Execute(MultiCursorCommands.InsertCursorAtEndOfEachLineSelected);

        Assert.Equal([new Selection(1, 3, 1, 3), new Selection(2, 4, 2, 4), new Selection(3, 5, 3, 5)], context.Selections);
    }

    [Fact]
    public void AddCursorsToBottomAndTop_FromTheFirstSelection_AddACursorOnEveryLine()
    {
        EditorContext context = CreateContext("a\nb\nc");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        context.Selection = new Selection(2, 1, 2, 1);
        context.Commands.Execute(MultiCursorCommands.AddCursorsToBottom);
        Assert.Equal(2, context.Selections.Count);

        context.Selection = new Selection(2, 1, 2, 1);
        context.Commands.Execute(MultiCursorCommands.AddCursorsToTop);
        Assert.Equal(2, context.Selections.Count);
    }

    [Fact]
    public void FocusNextCursor_AndPrevious_CycleThePrimaryCursor()
    {
        EditorContext context = CreateContext("a\nb\nc");
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);
        context.SetSelections([new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1), new Selection(3, 1, 3, 1)]);
        Position primaryBefore = context.Selection.Position;

        context.Commands.Execute(MultiCursorCommands.FocusNextCursor);
        Position afterNext = context.Selection.Position;
        context.Commands.Execute(MultiCursorCommands.FocusPreviousCursor);

        Assert.NotEqual(primaryBefore, afterNext);
        Assert.Equal(primaryBefore, context.Selection.Position);
        Assert.Equal(3, context.Selections.Count);
    }

    [Fact]
    public void MultiCursorFeature_IsInTheDefaultFeatures()
    {
        Assert.Contains(EditorFeatures.Default, factory => factory().Id == "loom.multiCursor");
    }
}
