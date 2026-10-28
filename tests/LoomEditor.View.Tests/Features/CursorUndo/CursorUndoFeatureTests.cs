// Ported from microsoft/vscode: src/vs/editor/contrib/cursorUndo/test/browser/cursorUndo.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.CursorUndo;
using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Features.CursorUndo;

public class CursorUndoFeatureTests
{
    private static EditorContext Create(string text = "")
    {
        var context = new EditorContext();
        context.SetText(text);
        new CursorUndoFeature().Attach(context);
        return context;
    }

    private static void Run(EditorContext context, string id) => Assert.True(context.Commands.Execute(id), id);

    private static void MoveTo(EditorContext context, int line, int column) => LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers.MoveTo(context.ViewModel, line, column);

    private static string Lines(int n) => string.Join('\n', Enumerable.Range(1, n).Select(i => "line" + i));

    // Monaco: 'issue #82535: Edge case with cursorUndo'
    [Fact]
    public void Issue82535_EdgeCaseWithCursorUndo()
    {
        EditorContext context = Create();
        ViewModel viewModel = context.ViewModel;
        viewModel.Type("hello", "test");
        MoveLeft(viewModel);
        CoreEditingCommands.DeleteRight.RunCoreEditingCommand(viewModel);
        Assert.Equal("hell", context.Model.GetValue());
        Assert.Equal([new Selection(1, 5, 1, 5)], viewModel.GetSelections());

        MoveLeft(viewModel);
        Assert.Equal([new Selection(1, 4, 1, 4)], viewModel.GetSelections());

        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal([new Selection(1, 5, 1, 5)], viewModel.GetSelections());
    }

    // Monaco: 'issue #82535: Edge case with cursorUndo (reverse)'
    [Fact]
    public void Issue82535_EdgeCaseWithCursorUndo_Reverse()
    {
        EditorContext context = Create();
        context.ViewModel.Type("hell", "test");
        context.ViewModel.Type("o", "test");
        Assert.Equal("hello", context.Model.GetValue());
        Assert.Equal([new Selection(1, 6, 1, 6)], context.ViewModel.GetSelections());

        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal([new Selection(1, 6, 1, 6)], context.ViewModel.GetSelections());
    }

    [Fact]
    public void Undo_AfterTwoMoves_RestoresFirstPosition()
    {
        EditorContext context = Create("abc\ndef\nghi");
        MoveTo(context, 1, 2);
        MoveTo(context, 2, 3);
        MoveTo(context, 3, 1);

        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal(new Position(2, 3), context.Selection.Position);
        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal(new Position(1, 2), context.Selection.Position);
    }

    [Fact]
    public void Redo_AfterUndo_RestoresSecond()
    {
        EditorContext context = Create("abc\ndef\nghi");
        MoveTo(context, 1, 2);
        MoveTo(context, 2, 3);
        Run(context, CursorUndoCommands.CursorUndo);

        Run(context, CursorUndoCommands.CursorRedo);

        Assert.Equal(new Position(2, 3), context.Selection.Position);
    }

    [Fact]
    public void NewMove_AfterUndo_ClearsRedoStack()
    {
        EditorContext context = Create("abc\ndef\nghi");
        MoveTo(context, 1, 2);
        MoveTo(context, 2, 3);
        Run(context, CursorUndoCommands.CursorUndo);
        MoveTo(context, 3, 3);

        Run(context, CursorUndoCommands.CursorRedo);

        Assert.Equal(new Position(3, 3), context.Selection.Position);
    }

    [Fact]
    public void TypingText_ClearsBothStacks()
    {
        EditorContext context = Create("abc\ndef\nghi");
        MoveTo(context, 1, 2);
        MoveTo(context, 2, 3);
        Run(context, CursorUndoCommands.CursorUndo);
        context.Model.ApplyEdits([new(new Range(3, 1, 3, 1), "x")]);
        Position before = context.Selection.Position;

        Run(context, CursorUndoCommands.CursorRedo);
        Run(context, CursorUndoCommands.CursorUndo);

        Assert.Equal(before, context.Selection.Position);
    }

    [Fact]
    public void SameSelectionTwice_NotPushedTwice()
    {
        EditorContext context = Create("abc\ndef\nghi");
        MoveTo(context, 1, 2);
        MoveTo(context, 2, 3);
        MoveTo(context, 2, 3);
        MoveTo(context, 3, 1);
        // the second move to (2,3) is a no-op: the stack holds (1,1), (1,2), (2,3)
        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal(new Position(2, 3), context.Selection.Position);
        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal(new Position(1, 2), context.Selection.Position);
        Run(context, CursorUndoCommands.CursorUndo);
        Assert.Equal(new Position(1, 1), context.Selection.Position);
    }

    [Fact]
    public void Stack_BoundedTo50()
    {
        EditorContext context = Create(Lines(100));
        for (int line = 2; line <= 80; line++)
        {
            MoveTo(context, line, 1);
        }

        for (int i = 0; i < 70; i++)
        {
            context.Commands.Execute(CursorUndoCommands.CursorUndo);
        }

        // 79 moves, 50 kept: the oldest reachable position is the one before the 30th move.
        Assert.Equal(new Position(30, 1), context.Selection.Position);
    }

    [Fact]
    public void Undo_RestoresScrollPosition()
    {
        EditorContext context = Create(Lines(200));
        ViewLayout layout = context.ViewModel.ViewLayout;
        layout.SetScrollPosition(new NewScrollPosition(null, 100), ScrollType.Immediate);
        double top = layout.GetCurrentScrollTop();
        Assert.True(top > 0, "the test needs a scrollable view");
        MoveTo(context, 5, 1);
        layout.SetScrollPosition(new NewScrollPosition(null, 300), ScrollType.Immediate);
        MoveTo(context, 150, 1);

        Run(context, CursorUndoCommands.CursorUndo);

        Assert.Equal(new Position(5, 1), context.Selection.Position);
        Assert.Equal(300, layout.GetCurrentScrollTop());
    }

    [Fact]
    public void Undo_WithEmptyStack_DoesNothing()
    {
        EditorContext context = Create("abc");
        MoveTo(context, 1, 3);
        Run(context, CursorUndoCommands.CursorUndo);
        Position restored = context.Selection.Position;

        Run(context, CursorUndoCommands.CursorUndo);

        Assert.Equal(restored, context.Selection.Position);
        Run(context, CursorUndoCommands.CursorRedo);
        Run(context, CursorUndoCommands.CursorRedo);
        Assert.Equal(new Position(1, 3), context.Selection.Position);
    }

    [Fact]
    public void SelectionChangeCausedByEdit_IsNotRecorded()
    {
        EditorContext context = Create("abc");
        MoveTo(context, 1, 2);
        context.ViewModel.Type("x", "test");

        // typing cleared the stacks and its own selection change was not recorded
        Run(context, CursorUndoCommands.CursorUndo);

        Assert.Equal(new Position(1, 3), context.Selection.Position);
    }

    [Fact]
    public void Ctrl_U_ResolvesToCursorUndo() =>
        Assert.Equal(
            CursorUndoCommands.CursorUndo,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyU), new KeybindingContext(TextInputFocus: true)));
}
