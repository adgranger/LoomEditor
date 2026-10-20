// Ported from microsoft/vscode: src/vs/editor/test/browser/commands/shiftCommand.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.Commands;

// Not ported (they need a language with doc-block on-enter rules, phases 4–5):
// 'issue #348: indenting around doc block comments' and 'issue #1609: Wrong indentation of block comments'.
public class ShiftCommandTests
{
    private static Func<Selection, IEditCommand> Cmd(bool isUnshift, int tabSize, int indentSize, bool insertSpaces, bool useTabStops) =>
        sel => new ShiftCommand(sel, new ShiftCommandOptions(isUnshift, tabSize, indentSize, insertSpaces, useTabStops, EditorAutoIndentStrategy.Full));

    private static void TestShift(string[] lines, bool useTabStops, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, Cmd(false, 4, 4, false, useTabStops), expectedLines, expectedSelection);

    private static void TestUnshift(string[] lines, bool useTabStops, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(lines, selection, Cmd(true, 4, 4, false, useTabStops), expectedLines, expectedSelection);

    // Monaco: 'Bug 9503: Shifting without any selection'
    [Fact]
    public void Bug9503ShiftingWithoutAnySelection()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 1, 1),
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 2, 1, 2)
        );
    }

    // Monaco: 'shift on single line selection 1'
    [Fact]
    public void ShiftOnSingleLineSelection1()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 3, 1, 1),
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 4, 1, 1)
        );
    }

    // Monaco: 'shift on single line selection 2'
    [Fact]
    public void ShiftOnSingleLineSelection2()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 1, 3),
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 1, 4)
        );
    }

    // Monaco: 'simple shift'
    [Fact]
    public void SimpleShift()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 2, 1),
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 2, 1)
        );
    }

    // Monaco: 'shifting on two separate lines'
    [Fact]
    public void ShiftingOnTwoSeparateLines()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 2, 1),
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 2, 1)
        );
        
        TestShift(
            [
                "\tMy First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 1, 3, 1),
            [
                "\tMy First Line",
                "\t\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 1, 3, 1)
        );
    }

    // Monaco: 'shifting on two lines'
    [Fact]
    public void ShiftingOnTwoLines()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 2, 2, 2),
            [
                "\tMy First Line",
                "\t\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 3, 2, 2)
        );
    }

    // Monaco: 'shifting on two lines again'
    [Fact]
    public void ShiftingOnTwoLinesAgain()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 2, 1, 2),
            [
                "\tMy First Line",
                "\t\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 2, 1, 3)
        );
    }

    // Monaco: 'shifting at end of file'
    [Fact]
    public void ShiftingAtEndOfFile()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(4, 1, 5, 2),
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "\t123"
            ],
            new Selection(4, 1, 5, 3)
        );
    }

    // Monaco: 'issue #1120 TAB should not indent empty lines in a multi-line selection'
    [Fact]
    public void Issue1120TABShouldNotIndentEmptyLinesInAMultiLineSelection()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 5, 2),
            [
                "\tMy First Line",
                "\t\t\tMy Second Line",
                "\t\tThird Line",
                "",
                "\t123"
            ],
            new Selection(1, 1, 5, 3)
        );
        
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(4, 1, 5, 1),
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "\t",
                "123"
            ],
            new Selection(4, 1, 5, 1)
        );
    }

    // Monaco: 'unshift on single line selection 1'
    [Fact]
    public void UnshiftOnSingleLineSelection1()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 3, 2, 1),
            [
                "My First Line",
                "\t\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 3, 2, 1)
        );
    }

    // Monaco: 'unshift on single line selection 2'
    [Fact]
    public void UnshiftOnSingleLineSelection2()
    {
        TestShift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 1, 2, 3),
            [
                "My First Line",
                "\t\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 1, 2, 3)
        );
    }

    // Monaco: 'simple unshift'
    [Fact]
    public void SimpleUnshift()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 2, 1),
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 2, 1)
        );
    }

    // Monaco: 'unshifting on two lines 1'
    [Fact]
    public void UnshiftingOnTwoLines1()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 2, 2, 2),
            [
                "My First Line",
                "\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(1, 2, 2, 2)
        );
    }

    // Monaco: 'unshifting on two lines 2'
    [Fact]
    public void UnshiftingOnTwoLines2()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 3, 2, 1),
            [
                "My First Line",
                "\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 2, 2, 1)
        );
    }

    // Monaco: 'unshifting at the end of the file'
    [Fact]
    public void UnshiftingAtTheEndOfTheFile()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(4, 1, 5, 2),
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(4, 1, 5, 2)
        );
    }

    // Monaco: 'unshift many times + shift'
    [Fact]
    public void UnshiftManyTimesShift()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 5, 4),
            [
                "My First Line",
                "\tMy Second Line",
                "Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 5, 4)
        );
        
        TestUnshift(
            [
                "My First Line",
                "\tMy Second Line",
                "Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 5, 4),
            [
                "My First Line",
                "My Second Line",
                "Third Line",
                "",
                "123"
            ],
            new Selection(1, 1, 5, 4)
        );
        
        TestShift(
            [
                "My First Line",
                "My Second Line",
                "Third Line",
                "",
                "123"
            ],
            true,
            new Selection(1, 1, 5, 4),
            [
                "\tMy First Line",
                "\tMy Second Line",
                "\tThird Line",
                "",
                "\t123"
            ],
            new Selection(1, 1, 5, 5)
        );
    }

    // Monaco: 'Bug 9119: Unshift from first column doesn't work'
    [Fact]
    public void Bug9119UnshiftFromFirstColumnDoesnTWork()
    {
        TestUnshift(
            [
                "My First Line",
                "\t\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            true,
            new Selection(2, 1, 2, 1),
            [
                "My First Line",
                "\tMy Second Line",
                "    Third Line",
                "",
                "123"
            ],
            new Selection(2, 1, 2, 1)
        );
    }

    // Monaco: 'issue #1620: a) Line indent doesn't handle leading whitespace properly'
    [Fact]
    public void Issue1620ALineIndentDoesnTHandleLeadingWhitespaceProperly()
    {
        CommandTester.TestCommand(
            [
                "   Written | Numeric",
                "       one | 1",
                "       two | 2",
                "     three | 3",
                "      four | 4",
                "      five | 5",
                "       six | 6",
                "     seven | 7",
                "     eight | 8",
                "      nine | 9",
                "       ten | 10",
                "    eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1),
            Cmd(false, 4, 4, true, false),
            [
                "       Written | Numeric",
                "           one | 1",
                "           two | 2",
                "         three | 3",
                "          four | 4",
                "          five | 5",
                "           six | 6",
                "         seven | 7",
                "         eight | 8",
                "          nine | 9",
                "           ten | 10",
                "        eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1)
        );
    }

    // Monaco: 'issue #1620: b) Line indent doesn't handle leading whitespace properly'
    [Fact]
    public void Issue1620BLineIndentDoesnTHandleLeadingWhitespaceProperly()
    {
        CommandTester.TestCommand(
            [
                "       Written | Numeric",
                "           one | 1",
                "           two | 2",
                "         three | 3",
                "          four | 4",
                "          five | 5",
                "           six | 6",
                "         seven | 7",
                "         eight | 8",
                "          nine | 9",
                "           ten | 10",
                "        eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1),
            Cmd(true, 4, 4, true, false),
            [
                "   Written | Numeric",
                "       one | 1",
                "       two | 2",
                "     three | 3",
                "      four | 4",
                "      five | 5",
                "       six | 6",
                "     seven | 7",
                "     eight | 8",
                "      nine | 9",
                "       ten | 10",
                "    eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1)
        );
    }

    // Monaco: 'issue #1620: c) Line indent doesn't handle leading whitespace properly'
    [Fact]
    public void Issue1620CLineIndentDoesnTHandleLeadingWhitespaceProperly()
    {
        CommandTester.TestCommand(
            [
                "       Written | Numeric",
                "           one | 1",
                "           two | 2",
                "         three | 3",
                "          four | 4",
                "          five | 5",
                "           six | 6",
                "         seven | 7",
                "         eight | 8",
                "          nine | 9",
                "           ten | 10",
                "        eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1),
            Cmd(true, 4, 4, false, false),
            [
                "   Written | Numeric",
                "       one | 1",
                "       two | 2",
                "     three | 3",
                "      four | 4",
                "      five | 5",
                "       six | 6",
                "     seven | 7",
                "     eight | 8",
                "      nine | 9",
                "       ten | 10",
                "    eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1)
        );
    }

    // Monaco: 'issue #1620: d) Line indent doesn't handle leading whitespace properly'
    [Fact]
    public void Issue1620DLineIndentDoesnTHandleLeadingWhitespaceProperly()
    {
        CommandTester.TestCommand(
            [
                "\t   Written | Numeric",
                "\t       one | 1",
                "\t       two | 2",
                "\t     three | 3",
                "\t      four | 4",
                "\t      five | 5",
                "\t       six | 6",
                "\t     seven | 7",
                "\t     eight | 8",
                "\t      nine | 9",
                "\t       ten | 10",
                "\t    eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1),
            Cmd(true, 4, 4, true, false),
            [
                "   Written | Numeric",
                "       one | 1",
                "       two | 2",
                "     three | 3",
                "      four | 4",
                "      five | 5",
                "       six | 6",
                "     seven | 7",
                "     eight | 8",
                "      nine | 9",
                "       ten | 10",
                "    eleven | 11",
                "",
            ],
            new Selection(1, 1, 13, 1)
        );
    }

    // Monaco: 'issue microsoft/monaco-editor#443: Indentation of a single row deletes selected text in some cases'
    [Fact]
    public void IssueMicrosoftMonacoEditor443IndentationOfASingleRowDeletesSelectedTextInSomeCases()
    {
        CommandTester.TestCommand(
            [
                "Hello world!",
                "another line"
            ],
            new Selection(1, 1, 1, 13),
            Cmd(false, 4, 4, false, true),
            [
                "\tHello world!",
                "another line"
            ],
            new Selection(1, 1, 1, 14)
        );
    }

    // Monaco: 'bug #16815:Shift+Tab doesn't go back to tabstop'
    [Fact]
    public void Bug16815ShiftTabDoesntGoBackToTabstop()
    {
        // insertSpaces: true
        // 0 => 0
        TestIndentation(4, 4, "", 0, 1);

        // 1 => 0
        TestIndentation(4, 4, "\t", 0, 2);
        TestIndentation(4, 4, " ", 0, 1);
        TestIndentation(4, 4, " \t", 0, 2);
        TestIndentation(4, 4, "  ", 0, 1);
        TestIndentation(4, 4, "  \t", 0, 2);
        TestIndentation(4, 4, "   ", 0, 1);
        TestIndentation(4, 4, "   \t", 0, 2);
        TestIndentation(4, 4, "    ", 0, 2);

        // 2 => 1
        TestIndentation(4, 4, "\t\t", 1, 3);
        TestIndentation(4, 4, "\t ", 1, 2);
        TestIndentation(4, 4, "\t \t", 1, 3);
        TestIndentation(4, 4, "\t  ", 1, 2);
        TestIndentation(4, 4, "\t  \t", 1, 3);
        TestIndentation(4, 4, "\t   ", 1, 2);
        TestIndentation(4, 4, "\t   \t", 1, 3);
        TestIndentation(4, 4, "\t    ", 1, 3);
        TestIndentation(4, 4, " \t\t", 1, 3);
        TestIndentation(4, 4, " \t ", 1, 2);
        TestIndentation(4, 4, " \t \t", 1, 3);
        TestIndentation(4, 4, " \t  ", 1, 2);
        TestIndentation(4, 4, " \t  \t", 1, 3);
        TestIndentation(4, 4, " \t   ", 1, 2);
        TestIndentation(4, 4, " \t   \t", 1, 3);
        TestIndentation(4, 4, " \t    ", 1, 3);
        TestIndentation(4, 4, "  \t\t", 1, 3);
        TestIndentation(4, 4, "  \t ", 1, 2);
        TestIndentation(4, 4, "  \t \t", 1, 3);
        TestIndentation(4, 4, "  \t  ", 1, 2);
        TestIndentation(4, 4, "  \t  \t", 1, 3);
        TestIndentation(4, 4, "  \t   ", 1, 2);
        TestIndentation(4, 4, "  \t   \t", 1, 3);
        TestIndentation(4, 4, "  \t    ", 1, 3);
        TestIndentation(4, 4, "   \t\t", 1, 3);
        TestIndentation(4, 4, "   \t ", 1, 2);
        TestIndentation(4, 4, "   \t \t", 1, 3);
        TestIndentation(4, 4, "   \t  ", 1, 2);
        TestIndentation(4, 4, "   \t  \t", 1, 3);
        TestIndentation(4, 4, "   \t   ", 1, 2);
        TestIndentation(4, 4, "   \t   \t", 1, 3);
        TestIndentation(4, 4, "   \t    ", 1, 3);
        TestIndentation(4, 4, "    \t", 1, 3);
        TestIndentation(4, 4, "     ", 1, 2);
        TestIndentation(4, 4, "     \t", 1, 3);
        TestIndentation(4, 4, "      ", 1, 2);
        TestIndentation(4, 4, "      \t", 1, 3);
        TestIndentation(4, 4, "       ", 1, 2);
        TestIndentation(4, 4, "       \t", 1, 3);
        TestIndentation(4, 4, "        ", 1, 3);

        // 3 => 2
        TestIndentation(4, 4, "         ", 2, 3);
    }

    private static void TestOutdent(int tabSize, int indentSize, bool insertSpaces, string lineText, int expectedIndents)
    {
        string oneIndent = insertSpaces ? new string(' ', indentSize) : "\t";
        string expectedIndent = string.Concat(Enumerable.Repeat(oneIndent, expectedIndents));
        if (lineText.Length > 0)
        {
            AssertEditOperations(true, tabSize, indentSize, insertSpaces, [lineText + "aaa"], [(new Range(1, 1, 1, lineText.Length + 1), expectedIndent, false)]);
        }
        else
        {
            AssertEditOperations(true, tabSize, indentSize, insertSpaces, [lineText + "aaa"], []);
        }
    }

    private static void TestIndent(int tabSize, int indentSize, bool insertSpaces, string lineText, int expectedIndents)
    {
        string oneIndent = insertSpaces ? new string(' ', indentSize) : "\t";
        string expectedIndent = string.Concat(Enumerable.Repeat(oneIndent, expectedIndents));
        AssertEditOperations(false, tabSize, indentSize, insertSpaces, [lineText + "aaa"], [(new Range(1, 1, 1, lineText.Length + 1), expectedIndent, false)]);
    }

    private static void TestIndentation(int tabSize, int indentSize, string lineText, int expectedOnOutdent, int expectedOnIndent)
    {
        TestOutdent(tabSize, indentSize, true, lineText, expectedOnOutdent);
        TestOutdent(tabSize, indentSize, false, lineText, expectedOnOutdent);

        TestIndent(tabSize, indentSize, true, lineText, expectedOnIndent);
        TestIndent(tabSize, indentSize, false, lineText, expectedOnIndent);
    }

    // Monaco: _assertUnshiftCommand / _assertShiftCommand
    private static void AssertEditOperations(
        bool isUnshift,
        int tabSize,
        int indentSize,
        bool insertSpaces,
        string[] text,
        List<(Range Range, string? Text, bool ForceMoveMarkers)> expected)
    {
        var model = new TextModel(string.Join('\n', text));
        var op = new ShiftCommand(
            new Selection(1, 1, text.Length + 1, 1),
            new ShiftCommandOptions(isUnshift, tabSize, indentSize, insertSpaces, true, EditorAutoIndentStrategy.Full));
        Assert.Equal(expected, CommandTester.GetEditOperation(model, op));
    }
}
