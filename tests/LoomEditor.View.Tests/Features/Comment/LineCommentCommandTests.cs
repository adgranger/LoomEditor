// Ported from microsoft/vscode: src/vs/editor/contrib/comment/test/browser/lineCommentCommand.test.ts (suite 'Editor Contrib - Line Comment Command')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Comment;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.Comment;

public partial class LineCommentCommandTests
{
    private const string CommentModeId = "commentMode";

    private static readonly CommentRule DefaultRule = new(new LineCommentConfig("!@#"), new CharacterPair("<!@#", "#@!>"));

    // Monaco: createTestCommandHelper
    private static void Test(
        CommentRule rule,
        LineCommentType type,
        bool insertSpace,
        string[] lines,
        Selection selection,
        string[] expectedLines,
        Selection expectedSelection,
        bool ignoreEmptyLines = true) =>
        CommandTester.TestCommand(
            lines,
            selection,
            (services, sel) => new LineCommentCommand(services.LanguageConfigurationRegistry, sel, 4, type, insertSpace, ignoreEmptyLines),
            expectedLines,
            expectedSelection,
            languageId: CommentModeId,
            prepare: services =>
            {
                services.RegisterLanguage(CommentModeId);
                services.Add(services.LanguageConfigurationRegistry.Register(CommentModeId, new LanguageConfiguration { Comments = rule }));
            });

    // Monaco: testLineCommentCommand
    private static void TestLineComment(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        Test(DefaultRule, LineCommentType.Toggle, true, lines, selection, expectedLines, expectedSelection);

    // Monaco: testAddLineCommentCommand
    private static void TestAddLineComment(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        Test(DefaultRule, LineCommentType.ForceAdd, true, lines, selection, expectedLines, expectedSelection);

    private static string[] Doc(params string[] head) => [.. head, "third line", "fourth line", "fifth"];

    // Monaco: 'comment single line'
    [Fact]
    public void CommentSingleLine() =>
        TestLineComment(["some text", "\tsome more text"], new Selection(1, 1, 1, 1), ["!@# some text", "\tsome more text"], new Selection(1, 5, 1, 5));

    // Monaco: 'case insensitive'
    [Fact]
    public void CaseInsensitive() =>
        Test(
            new CommentRule(new LineCommentConfig("rem")),
            LineCommentType.Toggle,
            true,
            ["REM some text"],
            new Selection(1, 1, 1, 1),
            ["some text"],
            new Selection(1, 1, 1, 1));

    // Monaco: 'comment with token column fixed'
    [Fact]
    public void CommentWithTokenColumnFixed() =>
        Test(
            new CommentRule(new LineCommentConfig("!@#", NoIndent: true), new CharacterPair("<!@#", "#@!>")),
            LineCommentType.Toggle,
            true,
            ["some text", "\tsome more text"],
            new Selection(2, 1, 2, 1),
            ["some text", "!@# \tsome more text"],
            new Selection(2, 5, 2, 5));

    private static List<LinePreflightData> BasicLinePreflightData(params string[] commentTokens) =>
        [.. commentTokens.Select(c => new LinePreflightData { Ignore = false, CommentStr = c, CommentStrOffset = 0, CommentStrLength = c.Length })];

    // Monaco: '_analyzeLines'
    [Fact]
    public void AnalyzeLines()
    {
        var registry = new LanguageConfigurationRegistry();

        string[] first = ["\t\t", "    ", "    c", "\t\td"];
        PreflightData r = LineCommentCommand.AnalyzeLines(
            LineCommentType.Toggle, true, n => first[n - 1], BasicLinePreflightData("//", "rem", "!@#", "!@#"), 1, true, false, registry, "plaintext");

        Assert.True(r.Supported);
        Assert.False(r.ShouldRemoveComments);

        // Does not change `commentStr`
        Assert.Equal(["//", "rem", "!@#", "!@#"], r.Lines.Select(l => l.CommentStr));

        // Fills in `isWhitespace`
        Assert.Equal([true, true, false, false], r.Lines.Select(l => l.Ignore));

        // Fills in `commentStrOffset`
        Assert.Equal([2, 4, 4, 2], r.Lines.Select(l => l.CommentStrOffset));

        string[] second = ["\t\t", "    rem ", "    !@# c", "\t\t!@#d"];
        r = LineCommentCommand.AnalyzeLines(
            LineCommentType.Toggle, true, n => second[n - 1], BasicLinePreflightData("//", "rem", "!@#", "!@#"), 1, true, false, registry, "plaintext");

        Assert.True(r.Supported);
        Assert.True(r.ShouldRemoveComments);

        // Does not change `commentStr`
        Assert.Equal(["//", "rem", "!@#", "!@#"], r.Lines.Select(l => l.CommentStr));

        // Fills in `isWhitespace`
        Assert.Equal([true, false, false, false], r.Lines.Select(l => l.Ignore));

        // Fills in `commentStrOffset`
        Assert.Equal([2, 4, 4, 2], r.Lines.Select(l => l.CommentStrOffset));

        // Fills in `commentStrLength`
        Assert.Equal([2, 4, 4, 3], r.Lines.Select(l => l.CommentStrLength));
    }

    // Monaco: '_normalizeInsertionPoint'
    [Fact]
    public void NormalizeInsertionPoint()
    {
        static void RunTest((string Line, int Offset)[] mixed, int tabSize, int[] expected, string testName)
        {
            string[] model = [.. mixed.Select(m => m.Line)];
            List<LinePreflightData> offsets = [.. mixed.Select(m => new LinePreflightData { Ignore = false, CommentStr = string.Empty, CommentStrOffset = m.Offset })];
            LineCommentCommand.NormalizeInsertionPoint(n => model[n - 1], offsets, 1, tabSize);
            Assert.True(expected.SequenceEqual(offsets.Select(o => o.CommentStrOffset)), testName);
        }

        // Bug 16696:[comment] comments not aligned in this case
        RunTest([("  XX", 2), ("    YY", 4)], 4, [0, 0], "Bug 16696");

        RunTest([("\t\t\tXX", 3), ("    \tYY", 5), ("        ZZ", 8), ("\t\tTT", 2)], 4, [2, 5, 8, 2], "Test1");

        RunTest([("\t\t\t   XX", 6), ("    \t\t\t\tYY", 8), ("        ZZ", 8), ("\t\t    TT", 6)], 4, [2, 5, 8, 2], "Test2");

        RunTest([("\t\t", 2), ("\t\t\t", 3), ("\t\t\t\t", 4), ("\t\t\t", 3)], 4, [2, 2, 2, 2], "Test3");

        RunTest([("\t\t", 2), ("\t\t\t", 3), ("\t\t\t\t", 4), ("\t\t\t", 3), ("    ", 4)], 2, [2, 2, 2, 2, 4], "Test4");

        RunTest([("\t\t", 2), ("\t\t\t", 3), ("\t\t\t\t", 4), ("\t\t\t", 3), ("    ", 4)], 4, [1, 1, 1, 1, 4], "Test5");

        RunTest([(" \t", 2), ("  \t", 3), ("   \t", 4), ("    ", 4), ("\t", 1)], 4, [2, 3, 4, 4, 1], "Test6");

        RunTest([(" \t\t", 3), ("  \t\t", 4), ("   \t\t", 5), ("    \t", 5), ("\t", 1)], 4, [2, 3, 4, 4, 1], "Test7");

        RunTest([("\t", 1), ("    ", 4)], 4, [1, 4], "Test8:4");
        RunTest([("\t", 1), ("   ", 3)], 4, [0, 0], "Test8:3");
        RunTest([("\t", 1), ("  ", 2)], 4, [0, 0], "Test8:2");
        RunTest([("\t", 1), (" ", 1)], 4, [0, 0], "Test8:1");
        RunTest([("\t", 1), (string.Empty, 0)], 4, [0, 0], "Test8:0");
    }

    // Monaco: 'detects indentation'
    [Fact]
    public void DetectsIndentation() =>
        TestLineComment(["\tsome text", "\tsome more text"], new Selection(2, 2, 1, 1), ["\t!@# some text", "\t!@# some more text"], new Selection(2, 2, 1, 1));

    // Monaco: 'detects mixed indentation'
    [Fact]
    public void DetectsMixedIndentation() =>
        TestLineComment(["\tsome text", "    some more text"], new Selection(2, 2, 1, 1), ["\t!@# some text", "    !@# some more text"], new Selection(2, 2, 1, 1));

    // Monaco: 'ignores whitespace lines'
    [Fact]
    public void IgnoresWhitespaceLines() =>
        TestLineComment(
            ["\tsome text", "\t   ", string.Empty, "\tsome more text"],
            new Selection(4, 2, 1, 1),
            ["\t!@# some text", "\t   ", string.Empty, "\t!@# some more text"],
            new Selection(4, 2, 1, 1));

    // Monaco: 'removes its own'
    [Fact]
    public void RemovesItsOwn() =>
        TestLineComment(
            ["\t!@# some text", "\t   ", "\t\t!@# some more text"],
            new Selection(3, 2, 1, 1),
            ["\tsome text", "\t   ", "\t\tsome more text"],
            new Selection(3, 2, 1, 1));

    // Monaco: 'works in only whitespace'
    [Fact]
    public void WorksInOnlyWhitespace() =>
        TestLineComment(
            ["\t    ", "\t", "\t\tsome more text"],
            new Selection(3, 1, 1, 1),
            ["\t!@#     ", "\t!@# ", "\t\tsome more text"],
            new Selection(3, 1, 1, 1));

    // Monaco: 'bug 9697 - whitespace before comment token'
    [Fact]
    public void Bug9697_WhitespaceBeforeCommentToken() =>
        TestLineComment(["\t !@#first", "\tsecond line"], new Selection(1, 1, 1, 1), ["\t first", "\tsecond line"], new Selection(1, 1, 1, 1));

    // Monaco: 'bug 10162 - line comment before caret'
    [Fact]
    public void Bug10162_LineCommentBeforeCaret() =>
        TestLineComment(["first!@#", "\tsecond line"], new Selection(1, 1, 1, 1), ["!@# first!@#", "\tsecond line"], new Selection(1, 5, 1, 5));

    // Monaco: 'comment single line - leading whitespace'
    [Fact]
    public void CommentSingleLine_LeadingWhitespace() =>
        TestLineComment(["first!@#", "\tsecond line"], new Selection(2, 3, 2, 1), ["first!@#", "\t!@# second line"], new Selection(2, 7, 2, 1));

    // Monaco: 'ignores invisible selection'
    [Fact]
    public void IgnoresInvisibleSelection() =>
        TestLineComment(Doc("first", "\tsecond line"), new Selection(2, 1, 1, 1), Doc("!@# first", "\tsecond line"), new Selection(2, 1, 1, 5));

    // Monaco: 'multiple lines'
    [Fact]
    public void MultipleLines() =>
        TestLineComment(Doc("first", "\tsecond line"), new Selection(2, 4, 1, 1), Doc("!@# first", "!@# \tsecond line"), new Selection(2, 8, 1, 5));

    // Monaco: 'multiple modes on multiple lines'
    [Fact]
    public void MultipleModesOnMultipleLines() =>
        TestLineComment(
            Doc("first", "\tsecond line"),
            new Selection(4, 4, 3, 1),
            ["first", "\tsecond line", "!@# third line", "!@# fourth line", "fifth"],
            new Selection(4, 8, 3, 5));

    // Monaco: 'toggle single line'
    [Fact]
    public void ToggleSingleLine()
    {
        TestLineComment(Doc("first", "\tsecond line"), new Selection(1, 1, 1, 1), Doc("!@# first", "\tsecond line"), new Selection(1, 5, 1, 5));
        TestLineComment(Doc("!@# first", "\tsecond line"), new Selection(1, 4, 1, 4), Doc("first", "\tsecond line"), new Selection(1, 1, 1, 1));
    }

    // Monaco: 'toggle multiple lines'
    [Fact]
    public void ToggleMultipleLines()
    {
        TestLineComment(Doc("first", "\tsecond line"), new Selection(2, 4, 1, 1), Doc("!@# first", "!@# \tsecond line"), new Selection(2, 8, 1, 5));
        TestLineComment(Doc("!@# first", "!@# \tsecond line"), new Selection(2, 7, 1, 4), Doc("first", "\tsecond line"), new Selection(2, 3, 1, 1));
    }

    // Monaco: 'issue #5964: Ctrl+/ to create comment when cursor is at the beginning of the line puts the cursor in a strange position'
    [Fact]
    public void Issue5964_CtrlSlashWhenCursorIsAtTheBeginningOfTheLine() =>
        TestLineComment(Doc("first", "\tsecond line"), new Selection(1, 1, 1, 1), Doc("!@# first", "\tsecond line"), new Selection(1, 5, 1, 5));

    // Monaco: 'issue #35673: Comment hotkeys throws the cursor before the comment'
    [Fact]
    public void Issue35673_CommentHotkeysThrowsTheCursorBeforeTheComment()
    {
        TestLineComment(
            ["first", string.Empty, "\tsecond line", "third line", "fourth line", "fifth"],
            new Selection(2, 1, 2, 1),
            ["first", "!@# ", "\tsecond line", "third line", "fourth line", "fifth"],
            new Selection(2, 5, 2, 5));

        TestLineComment(
            ["first", "\t", "\tsecond line", "third line", "fourth line", "fifth"],
            new Selection(2, 2, 2, 2),
            ["first", "\t!@# ", "\tsecond line", "third line", "fourth line", "fifth"],
            new Selection(2, 6, 2, 6));
    }

    // Monaco: 'issue #2837 "Add Line Comment" fault when blank lines involved'
    [Fact]
    public void Issue2837_AddLineCommentFaultWhenBlankLinesInvolved() =>
        TestAddLineComment(
            [
                "    if displayName == \"\":",
                "        displayName = groupName",
                "    description = getAttr(attributes, \"description\")",
                "    mailAddress = getAttr(attributes, \"mail\")",
                string.Empty,
                "    print \"||Group name|%s|\" % displayName",
                "    print \"||Description|%s|\" % description",
                "    print \"||Email address|[mailto:%s]|\" % mailAddress`",
            ],
            new Selection(1, 1, 8, 56),
            [
                "    !@# if displayName == \"\":",
                "    !@#     displayName = groupName",
                "    !@# description = getAttr(attributes, \"description\")",
                "    !@# mailAddress = getAttr(attributes, \"mail\")",
                string.Empty,
                "    !@# print \"||Group name|%s|\" % displayName",
                "    !@# print \"||Description|%s|\" % description",
                "    !@# print \"||Email address|[mailto:%s]|\" % mailAddress`",
            ],
            new Selection(1, 1, 8, 60));

    // Monaco: 'issue #47004: Toggle comments shouldn't move cursor'
    [Fact]
    public void Issue47004_ToggleCommentsShouldNotMoveCursor() =>
        TestAddLineComment(
            ["    A line", "    Another line"],
            new Selection(2, 7, 1, 1),
            ["    !@# A line", "    !@# Another line"],
            new Selection(2, 11, 1, 1));

    // Monaco: 'insertSpace false'
    [Fact]
    public void InsertSpaceFalse() =>
        Test(
            new CommentRule(new LineCommentConfig("!@#")),
            LineCommentType.Toggle,
            false,
            ["some text"],
            new Selection(1, 1, 1, 1),
            ["!@#some text"],
            new Selection(1, 4, 1, 4));

    // Monaco: 'insertSpace false does not remove space'
    [Fact]
    public void InsertSpaceFalseDoesNotRemoveSpace() =>
        Test(
            new CommentRule(new LineCommentConfig("!@#")),
            LineCommentType.Toggle,
            false,
            ["!@#    some text"],
            new Selection(1, 1, 1, 1),
            ["    some text"],
            new Selection(1, 1, 1, 1));
}
