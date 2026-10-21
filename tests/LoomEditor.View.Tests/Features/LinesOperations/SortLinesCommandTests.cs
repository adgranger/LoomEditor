using LoomEditor.Core.Text;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Tests.Cursors.TestHelpers;

namespace LoomEditor.View.Tests.Features.LinesOperations;

// Monaco: suite 'Editor Contrib - Sort Lines Command'
public class SortLinesCommandTests
{
    private static readonly string[] Lines = ["first", "second line", "third line", "fourth line", "fifth"];

    private static void TestSortLinesAscendingCommand(Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(Lines, selection, sel => new SortLinesCommand(sel, false), expectedLines, expectedSelection);

    private static void TestSortLinesDescendingCommand(Selection selection, string[] expectedLines, Selection expectedSelection) =>
        CommandTester.TestCommand(Lines, selection, sel => new SortLinesCommand(sel, true), expectedLines, expectedSelection);

    // Monaco: 'no op unless at least two lines selected 1'
    [Fact]
    public void NoOpUnlessAtLeastTwoLinesSelected1() =>
        TestSortLinesAscendingCommand(new Selection(1, 3, 1, 1), Lines, new Selection(1, 3, 1, 1));

    // Monaco: 'no op unless at least two lines selected 2'
    [Fact]
    public void NoOpUnlessAtLeastTwoLinesSelected2() =>
        TestSortLinesAscendingCommand(new Selection(1, 3, 2, 1), Lines, new Selection(1, 3, 2, 1));

    // Monaco: 'sorting two lines ascending'
    [Fact]
    public void SortingTwoLinesAscending() =>
        TestSortLinesAscendingCommand(
            new Selection(3, 3, 4, 2),
            ["first", "second line", "fourth line", "third line", "fifth"],
            new Selection(3, 3, 4, 1));

    // Monaco: 'sorting first 4 lines ascending'
    [Fact]
    public void SortingFirst4LinesAscending() =>
        TestSortLinesAscendingCommand(
            new Selection(1, 1, 5, 1),
            ["first", "fourth line", "second line", "third line", "fifth"],
            new Selection(1, 1, 5, 1));

    // Monaco: 'sorting all lines ascending'
    [Fact]
    public void SortingAllLinesAscending() =>
        TestSortLinesAscendingCommand(
            new Selection(1, 1, 5, 6),
            ["fifth", "first", "fourth line", "second line", "third line"],
            new Selection(1, 1, 5, 11));

    // Monaco: 'sorting first 4 lines descending'
    [Fact]
    public void SortingFirst4LinesDescending() =>
        TestSortLinesDescendingCommand(
            new Selection(1, 1, 5, 1),
            ["third line", "second line", "fourth line", "first", "fifth"],
            new Selection(1, 1, 5, 1));

    // Monaco: 'sorting all lines descending'
    [Fact]
    public void SortingAllLinesDescending() =>
        TestSortLinesDescendingCommand(
            new Selection(1, 1, 5, 6),
            ["third line", "second line", "fourth line", "first", "fifth"],
            new Selection(1, 1, 5, 6));

    [Fact]
    public void CanRun_SortedSelection_ReturnsFalse()
    {
        using var services = new LoomEditor.View.Tests.TestHelpers.TestEditorServices();
        var model = services.CreateTextModel("a\nb\nc");

        Assert.False(SortLinesCommand.CanRun(model, new Selection(1, 1, 3, 2), false));
        Assert.True(SortLinesCommand.CanRun(model, new Selection(1, 1, 3, 2), true));
        Assert.False(SortLinesCommand.CanRun(null, new Selection(1, 1, 3, 2), false));
    }
}
