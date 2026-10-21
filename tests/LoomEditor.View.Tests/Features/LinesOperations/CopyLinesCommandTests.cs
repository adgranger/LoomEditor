using LoomEditor.Core.Text;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suite 'Editor Contrib - Copy Lines Command'
public class CopyLinesCommandTests
{
    private static readonly string[] Lines = ["first", "second line", "third line", "fourth line", "fifth"];

    private static void TestCopyLinesDownCommand(Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(Lines, selection, sel => new CopyLinesCommand(sel, true), expectedLines, expectedSelection);

    private static void TestCopyLinesUpCommand(Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(Lines, selection, sel => new CopyLinesCommand(sel, false), expectedLines, expectedSelection);

    // Monaco: 'copy first line down'
    [Fact]
    public void CopyFirstLineDown() =>
        TestCopyLinesDownCommand(
            new Selection(1, 3, 1, 1),
            ["first", "first", "second line", "third line", "fourth line", "fifth"],
            new Selection(2, 3, 2, 1));

    // Monaco: 'copy first line up'
    [Fact]
    public void CopyFirstLineUp() =>
        TestCopyLinesUpCommand(
            new Selection(1, 3, 1, 1),
            ["first", "first", "second line", "third line", "fourth line", "fifth"],
            new Selection(1, 3, 1, 1));

    // Monaco: 'copy last line down'
    [Fact]
    public void CopyLastLineDown() =>
        TestCopyLinesDownCommand(
            new Selection(5, 3, 5, 1),
            ["first", "second line", "third line", "fourth line", "fifth", "fifth"],
            new Selection(6, 3, 6, 1));

    // Monaco: 'copy last line up'
    [Fact]
    public void CopyLastLineUp() =>
        TestCopyLinesUpCommand(
            new Selection(5, 3, 5, 1),
            ["first", "second line", "third line", "fourth line", "fifth", "fifth"],
            new Selection(5, 3, 5, 1));

    // Monaco: 'issue #1322: copy line up'
    [Fact]
    public void Issue1322_CopyLineUp() =>
        TestCopyLinesUpCommand(
            new Selection(3, 11, 3, 11),
            ["first", "second line", "third line", "third line", "fourth line", "fifth"],
            new Selection(3, 11, 3, 11));

    // Monaco: 'issue #1322: copy last line up'
    [Fact]
    public void Issue1322_CopyLastLineUp() =>
        TestCopyLinesUpCommand(
            new Selection(5, 6, 5, 6),
            ["first", "second line", "third line", "fourth line", "fifth", "fifth"],
            new Selection(5, 6, 5, 6));

    // Monaco: 'copy many lines up'
    [Fact]
    public void CopyManyLinesUp() =>
        TestCopyLinesUpCommand(
            new Selection(4, 3, 2, 1),
            ["first", "second line", "third line", "fourth line", "second line", "third line", "fourth line", "fifth"],
            new Selection(4, 3, 2, 1));

    // Monaco: 'ignore empty selection'
    [Fact]
    public void IgnoreEmptySelection() =>
        TestCopyLinesUpCommand(
            new Selection(2, 1, 1, 1),
            ["first", "first", "second line", "third line", "fourth line", "fifth"],
            new Selection(2, 1, 1, 1));
}
