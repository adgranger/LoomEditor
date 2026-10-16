// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller', paste tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class PasteTests
{
    // Monaco: 'issue #10212: Pasting entire line does not replace selection'
    [Fact]
    public void Issue10212PastingEntireLineDoesNotReplaceSelection()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line2"],
            editor =>
            {
                MoveTo(editor.ViewModel, 2, 1, false);
                MoveTo(editor.ViewModel, 2, 6, true);

                editor.ViewModel.Paste("line1\n", true);

                Assert.Equal("line1", editor.Model.GetLineContent(1));
                Assert.Equal("line1", editor.Model.GetLineContent(2));
                Assert.Equal(string.Empty, editor.Model.GetLineContent(3));
            });
    }

    // Monaco: 'issue #74722: Pasting whole line does not replace selection'
    [Fact]
    public void Issue74722PastingWholeLineDoesNotReplaceSelection()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line sel 2", "line3"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(2, 6, 2, 9)]);

                editor.ViewModel.Paste("line1\n", true);

                Assert.Equal("line1", editor.Model.GetLineContent(1));
                Assert.Equal("line line1", editor.Model.GetLineContent(2));
                Assert.Equal(" 2", editor.Model.GetLineContent(3));
                Assert.Equal("line3", editor.Model.GetLineContent(4));
            });
    }

    // Monaco: 'issue #4996: Multiple cursor paste pastes contents of all cursors'
    [Fact]
    public void Issue4996MultipleCursorPastePastesContentsOfAllCursors()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line2", "line3"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1)]);

                editor.ViewModel.Paste("a\nb\nc\nd", false, ["a\nb", "c\nd"]);

                Assert.Equal(string.Join('\n', "a", "bline1", "c", "dline2", "line3"), editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #16155: Paste into multiple cursors has edge case when number of lines equals number of cursors - 1'
    [Fact]
    public void Issue16155PasteIntoMultipleCursorsHasEdgeCaseWhenNumberOfLinesEqualsNumberOfCursorsMinus1()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["test", "test", "test", "test"],
            editor =>
            {
                editor.ViewModel.SetSelections(
                    "test",
                    [new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5), new Selection(4, 1, 4, 5)]);

                editor.ViewModel.Paste("aaa\nbbb\nccc\n", false, null);

                Assert.Equal(
                    string.Join('\n', "aaa", "bbb", "ccc", string.Empty, "aaa", "bbb", "ccc", string.Empty, "aaa", "bbb", "ccc", string.Empty, "aaa", "bbb", "ccc", string.Empty),
                    editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #43722: Multiline paste doesn't work anymore'
    [Fact]
    public void Issue43722MultilinePasteDoesntWorkAnymore()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["test", "test", "test", "test"],
            editor =>
            {
                editor.ViewModel.SetSelections(
                    "test",
                    [new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5), new Selection(4, 1, 4, 5)]);

                editor.ViewModel.Paste("aaa\r\nbbb\r\nccc\r\nddd\r\n", false, null);

                Assert.Equal(string.Join('\n', "aaa", "bbb", "ccc", "ddd"), editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #46440: (1) Pasting a multi-line selection pastes entire selection into every insertion point'
    [Fact]
    public void Issue464401PastingAMultiLineSelectionPastesEntireSelectionIntoEveryInsertionPoint()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line2", "line3"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1), new Selection(3, 1, 3, 1)]);

                editor.ViewModel.Paste("a\nb\nc", false, null);

                Assert.Equal(string.Join('\n', "aline1", "bline2", "cline3"), editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #46440: (2) Pasting a multi-line selection pastes entire selection into every insertion point'
    [Fact]
    public void Issue464402PastingAMultiLineSelectionPastesEntireSelectionIntoEveryInsertionPoint()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line2", "line3"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1), new Selection(3, 1, 3, 1)]);

                editor.ViewModel.Paste("a\nb\nc\n", false, null);

                Assert.Equal(string.Join('\n', "aline1", "bline2", "cline3"), editor.Model.GetValue());
            });
    }

    // Monaco: 'issue #256039: paste from multiple cursors with empty selections and multiCursorPaste full'
    [Fact]
    public void Issue256039PasteFromMultipleCursorsWithEmptySelectionsAndMultiCursorPasteFull()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["line1", "line2", "line3"],
            editor =>
            {
                // 2 cursors on lines 1 and 2
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1)]);

                editor.ViewModel.Paste("line1\nline2\n", true, ["line1\n", "line2\n"]);

                // Each cursor gets its respective line
                Assert.Equal(string.Join('\n', "line1", "line1", "line2", "line2", "line3"), editor.Model.GetValue());
            },
            new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { MultiCursorPaste = MultiCursorPasteMode.Full }));
    }

    // Monaco: 'issue #85712: Paste line moves cursor to start of current line rather than start of next line'
    [Fact]
    public void Issue85712PasteLineMovesCursorToStartOfCurrentLineRatherThanStartOfNextLine()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["abc123", string.Empty],
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(2, 1, 2, 1)]);
                editor.ViewModel.Paste("something\n", true);
                Assert.Equal(string.Join('\n', "abc123", "something", string.Empty), editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(3, 1));
            });
    }

    // Monaco: 'issue #15118: remove auto whitespace when pasting entire line'
    [Fact]
    public void Issue15118RemoveAutoWhitespaceWhenPastingEntireLine()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["    function f() {", "        // I'm gonna copy this line", "        return 3;", "    }"],
            editor =>
            {
                MoveTo(editor.ViewModel, 3, editor.Model.GetLineMaxColumn(3));
                editor.ViewModel.Type("\n", "keyboard");

                Assert.Equal(
                    string.Join('\n', "    function f() {", "        // I'm gonna copy this line", "        return 3;", "        ", "    }"),
                    editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(4, editor.Model.GetLineMaxColumn(4)));

                editor.ViewModel.Paste("        // I'm gonna copy this line\n", true);
                Assert.Equal(
                    string.Join('\n', "    function f() {", "        // I'm gonna copy this line", "        return 3;", "        // I'm gonna copy this line", string.Empty, "    }"),
                    editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(5, 1));
            });
    }

    // Monaco: 'issue #40695: maintain cursor position when copying lines using ctrl+c, ctrl+v'
    [Fact]
    public void Issue40695MaintainCursorPositionWhenCopyingLinesUsingCtrlCCtrlV()
    {
        TestCodeEditor.WithTestCodeEditor(
            ["    function f() {", "        // I'm gonna copy this line", "        // Another line", "        return 3;", "    }"],
            editor =>
            {
                editor.ViewModel.SetSelections(null, [new Selection(4, 10, 4, 10)]);
                editor.ViewModel.Paste("        // I'm gonna copy this line\n", true);

                Assert.Equal(
                    string.Join('\n', "    function f() {", "        // I'm gonna copy this line", "        // Another line", "        // I'm gonna copy this line", "        return 3;", "    }"),
                    editor.Model.GetValue());
                AssertCursor(editor.ViewModel, new Position(5, 10));
            });
    }
}
