// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller - Cursor', part 1)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Not ported here: 'issue #144041' and 'issue #140195' (word wrap / injected text: phase 6) and the 6 column selection
// tests (P3-28).
public partial class CursorMovementTests
{
    private const string Line1 = "    \tMy First Line\t ";
    private const string Line2 = "\tMy Second Line";
    private const string Line3 = "    Third Line🐶";
    private const string Line4 = "";
    private const string Line5 = "1";

    private const string Text = Line1 + "\r\n" + Line2 + "\n" + Line3 + "\n" + Line4 + "\r\n" + Line5;

    private static void RunTest(Action<TestCodeEditor, ViewModel> callback) =>
        TestCodeEditor.WithTestCodeEditor(Text, editor => callback(editor, editor.ViewModel));

    // Monaco: 'cursor initialized'
    [Fact]
    public void CursorInitialized() => RunTest((_, vm) => AssertCursor(vm, new Position(1, 1)));

    // --------- absolute move

    // Monaco: 'no move'
    [Fact]
    public void NoMove() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 1);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move'
    [Fact]
    public void Move() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 2);
        AssertCursor(vm, new Position(1, 2));
    });

    // Monaco: 'move in selection mode'
    [Fact]
    public void MoveInSelectionMode() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 2, true);
        AssertCursor(vm, new Selection(1, 1, 1, 2));
    });

    // Monaco: 'move beyond line end'
    [Fact]
    public void MoveBeyondLineEnd() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 25);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
    });

    // Monaco: 'move empty line'
    [Fact]
    public void MoveEmptyLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 4, 20);
        AssertCursor(vm, new Position(4, 1));
    });

    // Monaco: 'move one char line'
    [Fact]
    public void MoveOneCharLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 5, 20);
        AssertCursor(vm, new Position(5, 2));
    });

    // Monaco: 'selection down'
    [Fact]
    public void SelectionDown() => RunTest((_, vm) =>
    {
        MoveTo(vm, 2, 1, true);
        AssertCursor(vm, new Selection(1, 1, 2, 1));
    });

    // Monaco: 'move and then select'
    [Fact]
    public void MoveAndThenSelect() => RunTest((_, vm) =>
    {
        MoveTo(vm, 2, 3);
        AssertCursor(vm, new Position(2, 3));

        MoveTo(vm, 2, 15, true);
        AssertCursor(vm, new Selection(2, 3, 2, 15));

        MoveTo(vm, 1, 2, true);
        AssertCursor(vm, new Selection(2, 3, 1, 2));
    });

    // --------- move left

    // Monaco: 'move left on top left position'
    [Fact]
    public void MoveLeftOnTopLeftPosition() => RunTest((_, vm) =>
    {
        MoveLeft(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move left'
    [Fact]
    public void MoveLeftTest() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 3);
        AssertCursor(vm, new Position(1, 3));
        MoveLeft(vm);
        AssertCursor(vm, new Position(1, 2));
    });

    // Monaco: 'move left with surrogate pair'
    [Fact]
    public void MoveLeftWithSurrogatePair() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 17);
        AssertCursor(vm, new Position(3, 17));
        MoveLeft(vm);
        AssertCursor(vm, new Position(3, 15));
    });

    // Monaco: 'move left goes to previous row'
    [Fact]
    public void MoveLeftGoesToPreviousRow() => RunTest((_, vm) =>
    {
        MoveTo(vm, 2, 1);
        AssertCursor(vm, new Position(2, 1));
        MoveLeft(vm);
        AssertCursor(vm, new Position(1, 21));
    });

    // Monaco: 'move left selection'
    [Fact]
    public void MoveLeftSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 2, 1);
        AssertCursor(vm, new Position(2, 1));
        MoveLeft(vm, true);
        AssertCursor(vm, new Selection(2, 1, 1, 21));
    });

    // --------- move right

    // Monaco: 'move right on bottom right position'
    [Fact]
    public void MoveRightOnBottomRightPosition() => RunTest((_, vm) =>
    {
        MoveTo(vm, 5, 2);
        AssertCursor(vm, new Position(5, 2));
        MoveRight(vm);
        AssertCursor(vm, new Position(5, 2));
    });

    // Monaco: 'move right'
    [Fact]
    public void MoveRightTest() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 3);
        AssertCursor(vm, new Position(1, 3));
        MoveRight(vm);
        AssertCursor(vm, new Position(1, 4));
    });

    // Monaco: 'move right with surrogate pair'
    [Fact]
    public void MoveRightWithSurrogatePair() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 15);
        AssertCursor(vm, new Position(3, 15));
        MoveRight(vm);
        AssertCursor(vm, new Position(3, 17));
    });

    // Monaco: 'move right goes to next row'
    [Fact]
    public void MoveRightGoesToNextRow() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 21);
        AssertCursor(vm, new Position(1, 21));
        MoveRight(vm);
        AssertCursor(vm, new Position(2, 1));
    });

    // Monaco: 'move right selection'
    [Fact]
    public void MoveRightSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 21);
        AssertCursor(vm, new Position(1, 21));
        MoveRight(vm, true);
        AssertCursor(vm, new Selection(1, 21, 2, 1));
    });

    // --------- move down

    // Monaco: 'move down'
    [Fact]
    public void MoveDownTest() => RunTest((_, vm) =>
    {
        MoveDown(vm);
        AssertCursor(vm, new Position(2, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(3, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(4, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(5, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(5, 2));
    });

    // Monaco: 'move down with selection'
    [Fact]
    public void MoveDownWithSelection() => RunTest((_, vm) =>
    {
        MoveDown(vm, true);
        AssertCursor(vm, new Selection(1, 1, 2, 1));
        MoveDown(vm, true);
        AssertCursor(vm, new Selection(1, 1, 3, 1));
        MoveDown(vm, true);
        AssertCursor(vm, new Selection(1, 1, 4, 1));
        MoveDown(vm, true);
        AssertCursor(vm, new Selection(1, 1, 5, 1));
        MoveDown(vm, true);
        AssertCursor(vm, new Selection(1, 1, 5, 2));
    });

    // Monaco: 'move down with tabs'
    [Fact]
    public void MoveDownWithTabs() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 5);
        AssertCursor(vm, new Position(1, 5));
        MoveDown(vm);
        AssertCursor(vm, new Position(2, 2));
        MoveDown(vm);
        AssertCursor(vm, new Position(3, 5));
        MoveDown(vm);
        AssertCursor(vm, new Position(4, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(5, 2));
    });

    // --------- move up

    // Monaco: 'move up'
    [Fact]
    public void MoveUpTest() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 5);
        AssertCursor(vm, new Position(3, 5));

        MoveUp(vm);
        AssertCursor(vm, new Position(2, 2));

        MoveUp(vm);
        AssertCursor(vm, new Position(1, 5));
    });

    // Monaco: 'move up with selection'
    [Fact]
    public void MoveUpWithSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 5);
        AssertCursor(vm, new Position(3, 5));

        MoveUp(vm, true);
        AssertCursor(vm, new Selection(3, 5, 2, 2));

        MoveUp(vm, true);
        AssertCursor(vm, new Selection(3, 5, 1, 5));
    });

    // Monaco: 'move up and down with tabs'
    [Fact]
    public void MoveUpAndDownWithTabs() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 5);
        AssertCursor(vm, new Position(1, 5));
        MoveDown(vm);
        MoveDown(vm);
        MoveDown(vm);
        MoveDown(vm);
        AssertCursor(vm, new Position(5, 2));
        MoveUp(vm);
        AssertCursor(vm, new Position(4, 1));
        MoveUp(vm);
        AssertCursor(vm, new Position(3, 5));
        MoveUp(vm);
        AssertCursor(vm, new Position(2, 2));
        MoveUp(vm);
        AssertCursor(vm, new Position(1, 5));
    });

    // Monaco: 'move up and down with end of lines starting from a long one'
    [Fact]
    public void MoveUpAndDownWithEndOfLinesStartingFromALongOne() => RunTest((_, vm) =>
    {
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(2, Line2.Length + 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(3, Line3.Length + 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(4, Line4.Length + 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(5, Line5.Length + 1));
        MoveUp(vm);
        MoveUp(vm);
        MoveUp(vm);
        MoveUp(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
    });

    // Monaco: 'issue #44465: cursor position not correct when move'
    [Fact]
    public void Issue44465_CursorPositionNotCorrectWhenMove() => RunTest((_, vm) =>
    {
        vm.SetSelections("test", [new Selection(1, 5, 1, 5)]);

        // going once up on the first line remembers the offset visual columns
        MoveUp(vm);
        AssertCursor(vm, new Position(1, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(2, 2));
        MoveUp(vm);
        AssertCursor(vm, new Position(1, 5));

        // going twice up on the first line discards the offset visual columns
        MoveUp(vm);
        AssertCursor(vm, new Position(1, 1));
        MoveUp(vm);
        AssertCursor(vm, new Position(1, 1));
        MoveDown(vm);
        AssertCursor(vm, new Position(2, 1));
    });
}
