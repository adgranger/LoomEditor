using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.CaretOperations;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.CaretOperations;

// Monaco: suite 'Editor Contrib - Move Caret Command'
public class MoveCaretCommandTests
{
    private static void TestLeft(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, sel => new MoveCaretCommand(sel, true), expectedLines, expectedSelection);

    private static void TestRight(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, sel => new MoveCaretCommand(sel, false), expectedLines, expectedSelection);

    // Monaco: 'move selection to left'
    [Fact]
    public void MoveSelectionToLeft() => TestLeft(["012345"], new Selection(1, 3, 1, 5), ["023145"], new Selection(1, 2, 1, 4));

    // Monaco: 'move selection to right'
    [Fact]
    public void MoveSelectionToRight() => TestRight(["012345"], new Selection(1, 3, 1, 5), ["014235"], new Selection(1, 4, 1, 6));

    // Monaco: 'move selection to left - from first column - no change'
    [Fact]
    public void MoveSelectionToLeft_FromFirstColumn_NoChange() => TestLeft(["012345"], new Selection(1, 1, 1, 1), ["012345"], new Selection(1, 1, 1, 1));

    // Monaco: 'move selection to right - from last column - no change'
    [Fact]
    public void MoveSelectionToRight_FromLastColumn_NoChange() => TestRight(["012345"], new Selection(1, 5, 1, 7), ["012345"], new Selection(1, 5, 1, 7));

    [Fact]
    public void Left_AtColumn1_DoesNothing() => TestLeft(["012345"], new Selection(1, 1, 1, 3), ["012345"], new Selection(1, 1, 1, 3));

    [Fact]
    public void Right_AtLineEnd_DoesNothing() => TestRight(["012345"], new Selection(1, 5, 1, 7), ["012345"], new Selection(1, 5, 1, 7));

    [Fact]
    public void EmptySelection_DoesNothing() => TestRight(["012345"], new Selection(1, 3, 1, 3), ["012345"], new Selection(1, 3, 1, 3));

    [Fact]
    public void MultiLineSelection_DoesNothing() =>
        TestLeft(["012345", "abcdef"], new Selection(1, 3, 2, 3), ["012345", "abcdef"], new Selection(1, 3, 2, 3));

    private static EditorContext CreateContext(string text, out EditorFeatureHost host)
    {
        var context = new EditorContext();
        context.SetText(text);
        host = new EditorFeatureHost(context, [() => new CaretOperationsFeature()]);
        return context;
    }

    [Fact]
    public void Left_MovesSelectionAndKeepsItSelected()
    {
        using EditorContext context = CreateContext("012345", out EditorFeatureHost host);
        using (host)
        {
            context.SetSelections([new Selection(1, 3, 1, 5)]);

            Assert.True(context.Commands.Execute(CaretOperationsCommands.MoveCaretLeft));

            Assert.Equal("023145", context.Model.GetValue());
            Assert.Equal([new Selection(1, 2, 1, 4)], context.ViewModel.GetSelections());
        }
    }

    [Fact]
    public void MultiCursor_EachMovesIndependently()
    {
        using EditorContext context = CreateContext("012345\nabcdef", out EditorFeatureHost host);
        using (host)
        {
            context.SetSelections([new Selection(1, 3, 1, 5), new Selection(2, 2, 2, 4)]);

            context.Commands.Execute(CaretOperationsCommands.MoveCaretRight);

            Assert.Equal("014235\nadbcef", context.Model.GetValue());
            Assert.Equal([new Selection(1, 4, 1, 6), new Selection(2, 3, 2, 5)], context.ViewModel.GetSelections());
        }
    }

    [Fact]
    public void Undo_RestoresInOneStep()
    {
        using EditorContext context = CreateContext("012345\nabcdef", out EditorFeatureHost host);
        using (host)
        {
            context.SetSelections([new Selection(1, 3, 1, 5), new Selection(2, 2, 2, 4)]);
            context.Commands.Execute(CaretOperationsCommands.MoveCaretRight);

            context.Model.Undo();

            Assert.Equal("012345\nabcdef", context.Model.GetValue());
        }
    }

    [Fact]
    public void ReadOnly_DoesNothing()
    {
        using EditorContext context = CreateContext("012345", out EditorFeatureHost host);
        using (host)
        {
            context.SetSelections([new Selection(1, 3, 1, 5)]);
            context.ViewModel.SetCursorOptions(new EditorCursorOptions { ReadOnly = true });

            Assert.False(context.Commands.Execute(CaretOperationsCommands.MoveCaretLeft));
            Assert.Equal("012345", context.Model.GetValue());
        }
    }
}
