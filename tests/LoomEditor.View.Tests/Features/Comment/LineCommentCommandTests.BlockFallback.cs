// Ported from microsoft/vscode: src/vs/editor/contrib/comment/test/browser/lineCommentCommand.test.ts (suites 'ignoreEmptyLines false', 'Editor Contrib - Line Comment As Block Comment' and '… 2')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Comment;

namespace LoomEditor.View.Tests.Features.Comment;

public partial class LineCommentCommandTests
{
    // Monaco: the helper of the suite 'ignoreEmptyLines false'
    private static void TestIgnoreEmptyLinesFalse(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        Test(DefaultRule, LineCommentType.Toggle, true, lines, selection, expectedLines, expectedSelection, ignoreEmptyLines: false);

    // Monaco: the helper of the suite 'Editor Contrib - Line Comment As Block Comment'
    private static void TestAsBlockComment(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        Test(
            new CommentRule(new LineCommentConfig(string.Empty), new CharacterPair("(", ")")),
            LineCommentType.Toggle,
            true,
            lines,
            selection,
            expectedLines,
            expectedSelection);

    // Monaco: the helper of the suite 'Editor Contrib - Line Comment As Block Comment 2'
    private static void TestAsBlockComment2(string[] lines, Selection selection, string[] expectedLines, Selection expectedSelection) =>
        Test(
            new CommentRule(null, new CharacterPair("<!@#", "#@!>")),
            LineCommentType.Toggle,
            true,
            lines,
            selection,
            expectedLines,
            expectedSelection);

    // Monaco: 'ignoreEmptyLines false' › 'does not ignore whitespace lines'
    [Fact]
    public void IgnoreEmptyLinesFalse_DoesNotIgnoreWhitespaceLines() =>
        TestIgnoreEmptyLinesFalse(
            ["\tsome text", "\t   ", string.Empty, "\tsome more text"],
            new Selection(4, 2, 1, 1),
            ["!@# \tsome text", "!@# \t   ", "!@# ", "!@# \tsome more text"],
            new Selection(4, 6, 1, 5));

    // Monaco: 'ignoreEmptyLines false' › 'removes its own'
    [Fact]
    public void IgnoreEmptyLinesFalse_RemovesItsOwn() =>
        TestIgnoreEmptyLinesFalse(
            ["\t!@# some text", "\t   ", "\t\t!@# some more text"],
            new Selection(3, 2, 1, 1),
            ["\tsome text", "\t   ", "\t\tsome more text"],
            new Selection(3, 2, 1, 1));

    // Monaco: 'ignoreEmptyLines false' › 'works in only whitespace'
    [Fact]
    public void IgnoreEmptyLinesFalse_WorksInOnlyWhitespace() =>
        TestIgnoreEmptyLinesFalse(
            ["\t    ", "\t", "\t\tsome more text"],
            new Selection(3, 1, 1, 1),
            ["\t!@#     ", "\t!@# ", "\t\tsome more text"],
            new Selection(3, 1, 1, 1));

    // Monaco: 'ignoreEmptyLines false' › 'comments single line'
    [Fact]
    public void IgnoreEmptyLinesFalse_CommentsSingleLine() =>
        TestIgnoreEmptyLinesFalse(["some text", "\tsome more text"], new Selection(1, 1, 1, 1), ["!@# some text", "\tsome more text"], new Selection(1, 5, 1, 5));

    // Monaco: 'ignoreEmptyLines false' › 'detects indentation'
    [Fact]
    public void IgnoreEmptyLinesFalse_DetectsIndentation() =>
        TestIgnoreEmptyLinesFalse(["\tsome text", "\tsome more text"], new Selection(2, 2, 1, 1), ["\t!@# some text", "\t!@# some more text"], new Selection(2, 2, 1, 1));

    // Monaco: 'Line Comment As Block Comment' › 'fall back to block comment command'
    [Fact]
    public void AsBlockComment_FallBackToBlockCommentCommand() =>
        TestAsBlockComment(Doc("first", "\tsecond line"), new Selection(1, 1, 1, 1), Doc("( first )", "\tsecond line"), new Selection(1, 3, 1, 3));

    // Monaco: 'Line Comment As Block Comment' › 'fall back to block comment command - toggle'
    [Fact]
    public void AsBlockComment_FallBackToBlockCommentCommandToggle() =>
        TestAsBlockComment(Doc("(first)", "\tsecond line"), new Selection(1, 7, 1, 2), Doc("first", "\tsecond line"), new Selection(1, 6, 1, 1));

    // Monaco: 'Line Comment As Block Comment' › 'bug 9513 - expand single line to uncomment auto block'
    [Fact]
    public void AsBlockComment_Bug9513_ExpandSingleLineToUncommentAutoBlock() =>
        TestAsBlockComment(Doc("first", "\tsecond line"), new Selection(1, 1, 1, 1), Doc("( first )", "\tsecond line"), new Selection(1, 3, 1, 3));

    // Monaco: 'Line Comment As Block Comment' › 'bug 9691 - always expand selection to line boundaries'
    [Fact]
    public void AsBlockComment_Bug9691_AlwaysExpandSelectionToLineBoundaries()
    {
        TestAsBlockComment(
            Doc("first", "\tsecond line"),
            new Selection(3, 2, 1, 3),
            ["( first", "\tsecond line", "third line )", "fourth line", "fifth"],
            new Selection(3, 2, 1, 5));

        TestAsBlockComment(
            ["(first", "\tsecond line", "third line)", "fourth line", "fifth"],
            new Selection(3, 11, 1, 2),
            Doc("first", "\tsecond line"),
            new Selection(3, 11, 1, 1));
    }

    // Monaco: 'Line Comment As Block Comment 2' › 'no selection => uses indentation'
    [Fact]
    public void AsBlockComment2_NoSelectionUsesIndentation()
    {
        TestAsBlockComment2(
            ["\t\tfirst\t    ", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"],
            new Selection(1, 1, 1, 1),
            ["\t\t<!@# first\t     #@!>", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"],
            new Selection(1, 1, 1, 1));

        TestAsBlockComment2(
            ["\t\t<!@#first\t    #@!>", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"],
            new Selection(1, 1, 1, 1),
            ["\t\tfirst\t   ", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"],
            new Selection(1, 1, 1, 1));
    }

    // Monaco: 'Line Comment As Block Comment 2' › 'can remove'
    [Fact]
    public void AsBlockComment2_CanRemove()
    {
        string[] input = ["\t\tfirst\t    ", "\t\tsecond line", "\tthird line", "fourth line", "\t\t<!@#fifth#@!>\t\t"];
        string[] expected = ["\t\tfirst\t    ", "\t\tsecond line", "\tthird line", "fourth line", "\t\tfifth\t\t"];

        TestAsBlockComment2(input, new Selection(5, 1, 5, 1), expected, new Selection(5, 1, 5, 1));
        TestAsBlockComment2(input, new Selection(5, 3, 5, 3), expected, new Selection(5, 3, 5, 3));
        TestAsBlockComment2(input, new Selection(5, 4, 5, 4), expected, new Selection(5, 3, 5, 3));
        TestAsBlockComment2(input, new Selection(5, 16, 5, 3), expected, new Selection(5, 8, 5, 3));
        TestAsBlockComment2(input, new Selection(5, 12, 5, 7), expected, new Selection(5, 8, 5, 3));
        TestAsBlockComment2(input, new Selection(5, 18, 5, 18), expected, new Selection(5, 10, 5, 10));
    }

    // Monaco: 'Line Comment As Block Comment 2' › 'issue #993: Remove comment does not work consistently in HTML'
    [Fact]
    public void AsBlockComment2_Issue993_RemoveCommentDoesNotWorkConsistentlyInHtml()
    {
        TestAsBlockComment2(
            ["     asd qwe", "     asd qwe", string.Empty],
            new Selection(1, 1, 3, 1),
            ["     <!@# asd qwe", "     asd qwe #@!>", string.Empty],
            new Selection(1, 1, 3, 1));

        TestAsBlockComment2(
            ["     <!@#asd qwe", "     asd qwe#@!>", string.Empty],
            new Selection(1, 1, 3, 1),
            ["     asd qwe", "     asd qwe", string.Empty],
            new Selection(1, 1, 3, 1));
    }
}
