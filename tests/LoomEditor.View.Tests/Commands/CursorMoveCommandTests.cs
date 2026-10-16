// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursorMoveCommand.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Commands;

// Suite 'Cursor move command test'. The 'foldedLine unit' suite (8 tests) is not ported: it needs setHiddenAreas (folding, phase 5).
public class CursorMoveCommandTests
{
    private static readonly string Text = string.Join(
        '\n',
        "    \tMy First Line\t ",
        "\tMy Second Line",
        "    Third Line🐶",
        string.Empty,
        "1");

    private static void ExecuteTest(Action<ViewModel> callback) =>
        TestCodeEditor.WithTestCodeEditor(Text, editor => callback(editor.ViewModel));

    // Monaco: 'move left should move to left character'
    [Fact]
    public void MoveLeft_MovesToLeftCharacter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveLeft(viewModel);
        CursorEqual(viewModel, 1, 7);
    });

    // Monaco: 'move left should move to left by n characters'
    [Fact]
    public void MoveLeft_ByNCharacters() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveLeft(viewModel, 3);
        CursorEqual(viewModel, 1, 5);
    });

    // Monaco: 'move left should move to left by half line'
    [Fact]
    public void MoveLeft_ByHalfLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveLeft(viewModel, 1, CursorMoveRawUnit.HalfLine);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move left moves to previous line'
    [Fact]
    public void MoveLeft_MovesToPreviousLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 2, 3);
        MoveLeft(viewModel, 10);
        CursorEqual(viewModel, 1, 21);
    });

    // Monaco: 'move right should move to right character'
    [Fact]
    public void MoveRight_MovesToRightCharacter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 5);
        MoveRight(viewModel);
        CursorEqual(viewModel, 1, 6);
    });

    // Monaco: 'move right should move to right by n characters'
    [Fact]
    public void MoveRight_ByNCharacters() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 2);
        MoveRight(viewModel, 6);
        CursorEqual(viewModel, 1, 8);
    });

    // Monaco: 'move right should move to right by half line'
    [Fact]
    public void MoveRight_ByHalfLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 4);
        MoveRight(viewModel, 1, CursorMoveRawUnit.HalfLine);
        CursorEqual(viewModel, 1, 14);
    });

    // Monaco: 'move right moves to next line'
    [Fact]
    public void MoveRight_MovesToNextLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveRight(viewModel, 100);
        CursorEqual(viewModel, 2, 1);
    });

    // Monaco: 'move to first character of line from middle'
    [Fact]
    public void MoveToLineStart_FromMiddle() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveToLineStart(viewModel);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move to first character of line from first non white space character'
    [Fact]
    public void MoveToLineStart_FromFirstNonWhiteSpaceCharacter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 6);
        MoveToLineStart(viewModel);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move to first character of line from first character'
    [Fact]
    public void MoveToLineStart_FromFirstCharacter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 1);
        MoveToLineStart(viewModel);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move to first non white space character of line from middle'
    [Fact]
    public void MoveToFirstNonWhitespace_FromMiddle() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveToLineFirstNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 6);
    });

    // Monaco: 'move to first non white space character of line from first non white space character'
    [Fact]
    public void MoveToFirstNonWhitespace_FromFirstNonWhitespace() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 6);
        MoveToLineFirstNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 6);
    });

    // Monaco: 'move to first non white space character of line from first character'
    [Fact]
    public void MoveToFirstNonWhitespace_FromFirstCharacter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 1);
        MoveToLineFirstNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 6);
    });

    // Monaco: 'move to end of line from middle'
    [Fact]
    public void MoveToLineEnd_FromMiddle() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveToLineEnd(viewModel);
        CursorEqual(viewModel, 1, 21);
    });

    // Monaco: 'move to end of line from last non white space character'
    [Fact]
    public void MoveToLineEnd_FromLastNonWhitespace() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 19);
        MoveToLineEnd(viewModel);
        CursorEqual(viewModel, 1, 21);
    });

    // Monaco: 'move to end of line from line end'
    [Fact]
    public void MoveToLineEnd_FromLineEnd() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 21);
        MoveToLineEnd(viewModel);
        CursorEqual(viewModel, 1, 21);
    });

    // Monaco: 'move to last non white space character from middle'
    [Fact]
    public void MoveToLastNonWhitespace_FromMiddle() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveToLineLastNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 19);
    });

    // Monaco: 'move to last non white space character from last non white space character'
    [Fact]
    public void MoveToLastNonWhitespace_FromLastNonWhitespace() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 19);
        MoveToLineLastNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 19);
    });

    // Monaco: 'move to last non white space character from line end'
    [Fact]
    public void MoveToLastNonWhitespace_FromLineEnd() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 21);
        MoveToLineLastNonWhitespaceCharacter(viewModel);
        CursorEqual(viewModel, 1, 19);
    });

    // Monaco: 'move to center of line not from center'
    [Fact]
    public void MoveToLineCenter_NotFromCenter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 8);
        MoveToLineCenter(viewModel);
        CursorEqual(viewModel, 1, 11);
    });

    // Monaco: 'move to center of line from center'
    [Fact]
    public void MoveToLineCenter_FromCenter() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 11);
        MoveToLineCenter(viewModel);
        CursorEqual(viewModel, 1, 11);
    });

    // Monaco: 'move to center of line from start'
    [Fact]
    public void MoveToLineCenter_FromStart() => ExecuteTest(viewModel =>
    {
        MoveToLineStart(viewModel);
        MoveToLineCenter(viewModel);
        CursorEqual(viewModel, 1, 11);
    });

    // Monaco: 'move to center of line from end'
    [Fact]
    public void MoveToLineCenter_FromEnd() => ExecuteTest(viewModel =>
    {
        MoveToLineEnd(viewModel);
        MoveToLineCenter(viewModel);
        CursorEqual(viewModel, 1, 11);
    });

    // Monaco: 'move up by cursor move command'
    [Fact]
    public void MoveUp_ByCursorMoveCommand() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 3, 5);
        CursorEqual(viewModel, 3, 5);

        MoveUp(viewModel, 2);
        CursorEqual(viewModel, 1, 5);

        MoveUp(viewModel, 1);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move up by model line cursor move command'
    [Fact]
    public void MoveUp_ByModelLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 3, 5);
        CursorEqual(viewModel, 3, 5);

        MoveUpByModelLine(viewModel, 2);
        CursorEqual(viewModel, 1, 5);

        MoveUpByModelLine(viewModel, 1);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move down by model line cursor move command'
    [Fact]
    public void MoveDown_ByModelLine() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 3, 5);
        CursorEqual(viewModel, 3, 5);

        MoveDownByModelLine(viewModel, 2);
        CursorEqual(viewModel, 5, 2);

        MoveDownByModelLine(viewModel, 1);
        CursorEqual(viewModel, 5, 2);
    });

    // Monaco: 'move up with selection by cursor move command'
    [Fact]
    public void MoveUp_WithSelection() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 3, 5);
        CursorEqual(viewModel, 3, 5);

        MoveUp(viewModel, 1, true);
        CursorEqual(viewModel, 2, 2, 3, 5);

        MoveUp(viewModel, 1, true);
        CursorEqual(viewModel, 1, 5, 3, 5);
    });

    // Monaco: 'move up and down with tabs by cursor move command'
    [Fact]
    public void MoveUpAndDown_WithTabs() => ExecuteTest(viewModel =>
    {
        MoveTo(viewModel, 1, 5);
        CursorEqual(viewModel, 1, 5);

        MoveDown(viewModel, 4);
        CursorEqual(viewModel, 5, 2);

        MoveUp(viewModel, 1);
        CursorEqual(viewModel, 4, 1);

        MoveUp(viewModel, 1);
        CursorEqual(viewModel, 3, 5);

        MoveUp(viewModel, 1);
        CursorEqual(viewModel, 2, 2);

        MoveUp(viewModel, 1);
        CursorEqual(viewModel, 1, 5);
    });

    // Monaco: 'move up and down with end of lines starting from a long one by cursor move command'
    [Fact]
    public void MoveUpAndDown_WithEndOfLinesStartingFromALongOne() => ExecuteTest(viewModel =>
    {
        MoveToEndOfLine(viewModel);
        CursorEqual(viewModel, 1, 21);

        MoveToEndOfLine(viewModel);
        CursorEqual(viewModel, 1, 21);

        MoveDown(viewModel, 2);
        CursorEqual(viewModel, 3, 17);

        MoveDown(viewModel, 1);
        CursorEqual(viewModel, 4, 1);

        MoveDown(viewModel, 1);
        CursorEqual(viewModel, 5, 2);

        MoveUp(viewModel, 4);
        CursorEqual(viewModel, 1, 21);
    });

    // Monaco: 'move to view top line moves to first visible line if it is first line'
    [Fact]
    public void MoveToTop_FirstVisibleLineIsFirstLine() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(1, 1, 10, 1);

        MoveTo(viewModel, 2, 2);
        MoveToTop(viewModel);

        CursorEqual(viewModel, 1, 6);
    });

    // Monaco: 'move to view top line moves to top visible line when first line is not visible'
    [Fact]
    public void MoveToTop_FirstLineNotVisible() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(2, 1, 10, 1);

        MoveTo(viewModel, 4, 1);
        MoveToTop(viewModel);

        CursorEqual(viewModel, 2, 2);
    });

    // Monaco: 'move to view top line moves to nth line from top'
    [Fact]
    public void MoveToTop_NthLineFromTop() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(1, 1, 10, 1);

        MoveTo(viewModel, 4, 1);
        MoveToTop(viewModel, 3);

        CursorEqual(viewModel, 3, 5);
    });

    // Monaco: 'move to view top line moves to last line if n is greater than last visible line number'
    [Fact]
    public void MoveToTop_NGreaterThanLastVisibleLine() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(1, 1, 3, 1);

        MoveTo(viewModel, 2, 2);
        MoveToTop(viewModel, 4);

        CursorEqual(viewModel, 3, 5);
    });

    // Monaco: 'move to view center line moves to the center line'
    [Fact]
    public void MoveToCenter_MovesToTheCenterLine() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(3, 1, 3, 1);

        MoveTo(viewModel, 2, 2);
        MoveToCenter(viewModel);

        CursorEqual(viewModel, 3, 5);
    });

    // Monaco: 'move to view bottom line moves to last visible line if it is last line'
    [Fact]
    public void MoveToBottom_LastVisibleLineIsLastLine() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(1, 1, 5, 1);

        MoveTo(viewModel, 2, 2);
        MoveToBottom(viewModel);

        CursorEqual(viewModel, 5, 1);
    });

    // Monaco: 'move to view bottom line moves to last visible line when last line is not visible'
    [Fact]
    public void MoveToBottom_LastLineNotVisible() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(2, 1, 3, 1);

        MoveTo(viewModel, 2, 2);
        MoveToBottom(viewModel);

        CursorEqual(viewModel, 3, 5);
    });

    // Monaco: 'move to view bottom line moves to nth line from bottom'
    [Fact]
    public void MoveToBottom_NthLineFromBottom() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(1, 1, 5, 1);

        MoveTo(viewModel, 4, 1);
        MoveToBottom(viewModel, 3);

        CursorEqual(viewModel, 3, 5);
    });

    // Monaco: 'move to view bottom line moves to first line if n is lesser than first visible line number'
    [Fact]
    public void MoveToBottom_NLesserThanFirstVisibleLine() => ExecuteTest(viewModel =>
    {
        viewModel.CompletelyVisibleViewRangeOverride = () => new Range(2, 1, 5, 1);

        MoveTo(viewModel, 4, 1);
        MoveToBottom(viewModel, 5);

        CursorEqual(viewModel, 2, 2);
    });

    // ----- Cursor move by blankline test

    private static readonly string BlankLineText = string.Join(
        '\n',
        "    \tMy First Line\t ",
        "\tMy Second Line",
        "    Third Line🐶",
        string.Empty,
        "1",
        "2",
        "3",
        string.Empty,
        "         ",
        "a",
        "b");

    private static void ExecuteBlankLineTest(Action<ViewModel> callback) =>
        TestCodeEditor.WithTestCodeEditor(BlankLineText, editor => callback(editor.ViewModel));

    // Monaco: 'move down should move to start of next blank line'
    [Fact]
    public void MoveDownByBlankLine_MovesToStartOfNextBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveDownByBlankLine(viewModel, false);
        CursorEqual(viewModel, 4, 1);
    });

    // Monaco: 'move up should move to start of previous blank line'
    [Fact]
    public void MoveUpByBlankLine_MovesToStartOfPreviousBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 7, 1);
        MoveUpByBlankLine(viewModel, false);
        CursorEqual(viewModel, 4, 1);
    });

    // Monaco: 'move down should skip over whitespace if already on blank line'
    [Fact]
    public void MoveDownByBlankLine_SkipsOverWhitespaceIfAlreadyOnBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 8, 1);
        MoveDownByBlankLine(viewModel, false);
        CursorEqual(viewModel, 11, 1);
    });

    // Monaco: 'move up should skip over whitespace if already on blank line'
    [Fact]
    public void MoveUpByBlankLine_SkipsOverWhitespaceIfAlreadyOnBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 9, 1);
        MoveUpByBlankLine(viewModel, false);
        CursorEqual(viewModel, 4, 1);
    });

    // Monaco: 'move up should go to first column of first line if not empty'
    [Fact]
    public void MoveUpByBlankLine_GoesToFirstColumnOfFirstLineIfNotEmpty() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 2, 1);
        MoveUpByBlankLine(viewModel, false);
        CursorEqual(viewModel, 1, 1);
    });

    // Monaco: 'move down should go to first column of last line if not empty'
    [Fact]
    public void MoveDownByBlankLine_GoesToFirstColumnOfLastLineIfNotEmpty() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 10, 1);
        MoveDownByBlankLine(viewModel, false);
        CursorEqual(viewModel, 11, 1);
    });

    // Monaco: 'select down should select to start of next blank line'
    [Fact]
    public void SelectDownByBlankLine_SelectsToStartOfNextBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveDownByBlankLine(viewModel, true);
        SelectionEqual(viewModel.GetSelection(), 4, 1, 1, 1);
    });

    // Monaco: 'select up should select to start of previous blank line'
    [Fact]
    public void SelectUpByBlankLine_SelectsToStartOfPreviousBlankLine() => ExecuteBlankLineTest(viewModel =>
    {
        MoveTo(viewModel, 7, 1);
        MoveUpByBlankLine(viewModel, true);
        SelectionEqual(viewModel.GetSelection(), 4, 1, 7, 1);
    });

    // ----- Move command helpers (Monaco: lines 619–736)

    private static void Move(ViewModel viewModel, CursorMoveArguments args) =>
        CoreNavigationCommands.CursorMove.RunCoreEditorCommand(viewModel, new CursorMoveRunArgs(args));

    private static void MoveToLineStart(ViewModel viewModel) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.WrappedLineStart));

    private static void MoveToLineFirstNonWhitespaceCharacter(ViewModel viewModel) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.WrappedLineFirstNonWhitespaceCharacter));

    private static void MoveToLineCenter(ViewModel viewModel) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.WrappedLineColumnCenter));

    private static void MoveToLineEnd(ViewModel viewModel) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.WrappedLineEnd));

    private static void MoveToLineLastNonWhitespaceCharacter(ViewModel viewModel) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.WrappedLineLastNonWhitespaceCharacter));

    private static void MoveLeft(ViewModel viewModel, int value = 1, string? by = null, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Left, select, by, value));

    private static void MoveRight(ViewModel viewModel, int value = 1, string? by = null, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Right, select, by, value));

    private static void MoveUp(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Up, select, CursorMoveRawUnit.WrappedLine, noOfLines));

    private static void MoveUpByBlankLine(ViewModel viewModel, bool select) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.PrevBlankLine, select, CursorMoveRawUnit.WrappedLine));

    private static void MoveUpByModelLine(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Up, select, null, noOfLines));

    private static void MoveDown(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Down, select, CursorMoveRawUnit.WrappedLine, noOfLines));

    private static void MoveDownByBlankLine(ViewModel viewModel, bool select) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.NextBlankLine, select, CursorMoveRawUnit.WrappedLine));

    private static void MoveDownByModelLine(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.Down, select, null, noOfLines));

    private static void MoveToTop(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.ViewPortTop, select, null, noOfLines));

    private static void MoveToCenter(ViewModel viewModel, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.ViewPortCenter, select));

    private static void MoveToBottom(ViewModel viewModel, int noOfLines = 1, bool select = false) =>
        Move(viewModel, new CursorMoveArguments(CursorMoveRawDirection.ViewPortBottom, select, null, noOfLines));

    private static void CursorEqual(ViewModel viewModel, int posLineNumber, int posColumn, int? selLineNumber = null, int? selColumn = null)
    {
        Assert.Equal(new Position(posLineNumber, posColumn), viewModel.GetPosition());
        SelectionEqual(viewModel.GetSelection(), posLineNumber, posColumn, selLineNumber ?? posLineNumber, selColumn ?? posColumn);
    }

    private static void SelectionEqual(Selection selection, int posLineNumber, int posColumn, int selLineNumber, int selColumn)
    {
        Assert.Equal(
            (selLineNumber, selColumn, posLineNumber, posColumn),
            (selection.SelectionStartLineNumber, selection.SelectionStartColumn, selection.PositionLineNumber, selection.PositionColumn));
    }

    private static void MoveTo(ViewModel viewModel, int lineNumber, int column, bool inSelectionMode = false)
    {
        var args = new MoveCommandArgs(new Position(lineNumber, column));
        if (inSelectionMode)
        {
            CoreNavigationCommands.MoveToSelect.RunCoreEditorCommand(viewModel, args);
        }
        else
        {
            CoreNavigationCommands.MoveTo.RunCoreEditorCommand(viewModel, args);
        }
    }

    private static void MoveToEndOfLine(ViewModel viewModel, bool inSelectionMode = false)
    {
        if (inSelectionMode)
        {
            CoreNavigationCommands.CursorEndSelect.RunCoreEditorCommand(viewModel, new EndCommandArgs());
        }
        else
        {
            CoreNavigationCommands.CursorEnd.RunCoreEditorCommand(viewModel, new EndCommandArgs());
        }
    }
}
