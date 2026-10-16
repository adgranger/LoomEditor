using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// The Monaco tests of these functions run through the core commands (P3-26, P3-27): these are direct tests.
public class CursorMoveCommandsTests
{
    private const int MaxSafeSmallInteger = 1 << 30;

    private static void Run(string text, Action<ViewModel> test) =>
        TestCodeEditor.WithTestCodeEditor(text, editor => test(editor.ViewModel));

    private static void Place(ViewModel viewModel, int lineNumber, int column) =>
        viewModel.SetCursorStates(
            "test",
            CursorChangeReason.NotSet,
            [CursorState.FromModelSelection(new Selection(lineNumber, column, lineNumber, column))]);

    private static CursorState Primary(ViewModel viewModel) => viewModel.GetPrimaryCursorState();

    [Fact]
    public void MoveToBeginningOfLine_FromEnd_TogglesBetweenFirstNonBlankAndStart()
    {
        Run("   abc", viewModel =>
        {
            Place(viewModel, 1, 7);

            PartialCursorState first = CursorMoveCommands.MoveToBeginningOfLine(viewModel, [Primary(viewModel)], false)[0];
            Assert.Equal(new Position(1, 4), first.ModelState!.Position);

            Place(viewModel, 1, 4);
            PartialCursorState second = CursorMoveCommands.MoveToBeginningOfLine(viewModel, [Primary(viewModel)], false)[0];
            Assert.Equal(new Position(1, 1), second.ModelState!.Position);
        });
    }

    [Fact]
    public void MoveToEndOfLine_Sticky_SetsLeftoverColumns()
    {
        Run("abc", viewModel =>
        {
            Place(viewModel, 1, 1);

            PartialCursorState plain = CursorMoveCommands.MoveToEndOfLine(viewModel, [Primary(viewModel)], false, false)[0];
            PartialCursorState sticky = CursorMoveCommands.MoveToEndOfLine(viewModel, [Primary(viewModel)], false, true)[0];

            Assert.Equal(new Position(1, 4), plain.ModelState!.Position);
            Assert.Equal(0, plain.ModelState.LeftoverVisibleColumns);
            Assert.Equal(MaxSafeSmallInteger - 4, sticky.ModelState!.LeftoverVisibleColumns);
        });
    }

    [Fact]
    public void AddCursorDown_WithOneCursor_AddsACursorOnTheNextLine()
    {
        Run("abc\ndef\nghi", viewModel =>
        {
            Place(viewModel, 1, 3);

            List<PartialCursorState> result = CursorMoveCommands.AddCursorDown(viewModel, [Primary(viewModel)], true);

            Assert.Equal(2, result.Count);
            Assert.Equal(new Position(1, 3), result[0].ModelState!.Position);
            Assert.Equal(new Position(2, 3), result[1].ModelState!.Position);
        });
    }

    [Fact]
    public void AddCursorUp_ByView_AddsACursorOnThePreviousLine()
    {
        Run("abc\ndef\nghi", viewModel =>
        {
            Place(viewModel, 3, 2);

            List<PartialCursorState> result = CursorMoveCommands.AddCursorUp(viewModel, [Primary(viewModel)], false);

            Assert.Equal(2, result.Count);
            Assert.Equal(new Position(2, 2), result[1].ViewState!.Position);
        });
    }

    [Fact]
    public void Line_OnLine2_SelectsTheWholeLine()
    {
        Run("abc\ndef\nghi", viewModel =>
        {
            Place(viewModel, 1, 1);

            PartialCursorState result = CursorMoveCommands.Line(viewModel, Primary(viewModel), false, new Position(2, 2), null);

            Assert.Equal(new Selection(2, 1, 3, 1), result.ModelState!.Selection);
            Assert.Equal(SelectionStartKind.Line, result.ModelState.SelectionStartKind);
        });
    }

    [Fact]
    public void ExpandLineSelection_Twice_ExpandsLineByLine()
    {
        Run("abc\ndef\nghi", viewModel =>
        {
            Place(viewModel, 1, 2);

            PartialCursorState first = CursorMoveCommands.ExpandLineSelection(viewModel, [Primary(viewModel)])[0];
            Assert.Equal(new Selection(1, 1, 2, 1), first.ModelState!.Selection);

            viewModel.SetCursorStates("test", CursorChangeReason.NotSet, [first]);
            PartialCursorState second = CursorMoveCommands.ExpandLineSelection(viewModel, [Primary(viewModel)])[0];
            Assert.Equal(new Selection(1, 1, 3, 1), second.ModelState!.Selection);
        });
    }

    [Fact]
    public void SelectAll_SelectsTheWholeBuffer_AndCancelSelectionCollapses()
    {
        Run("abc\ndef", viewModel =>
        {
            Place(viewModel, 1, 2);

            PartialCursorState all = CursorMoveCommands.SelectAll(viewModel, Primary(viewModel));
            Assert.Equal(new Selection(1, 1, 2, 4), all.ModelState!.Selection);

            viewModel.SetCursorStates("test", CursorChangeReason.NotSet, [all]);
            PartialCursorState cancelled = CursorMoveCommands.CancelSelection(viewModel, Primary(viewModel));
            Assert.Equal(new Selection(2, 4, 2, 4), cancelled.ViewState!.Selection);
        });
    }

    [Fact]
    public void MoveTo_InSelectionMode_KeepsTheSelectionStart()
    {
        Run("abc\ndef", viewModel =>
        {
            Place(viewModel, 1, 2);

            PartialCursorState result = CursorMoveCommands.MoveTo(viewModel, Primary(viewModel), true, new Position(2, 3), null);

            Assert.Equal(new Selection(1, 2, 2, 3), result.ViewState!.Selection);
        });
    }
}
