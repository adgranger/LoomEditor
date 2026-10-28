using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class RenderWhitespaceComputerTests
{
    private static int[] Run(string s, RenderWhitespace mode, int faux = 0, bool wrapped = false, OffsetRange[]? sel = null, int? length = null) =>
        [.. RenderWhitespaceComputer.Compute(s, length ?? s.Length, faux, mode, wrapped, sel)];

    private static int[] Seq(int from, int to) => [.. Enumerable.Range(from, to - from + 1)];

    // Monaco: 'ws-2-tabs'
    [Fact]
    public void Boundary_LeadingAndTrailingTabs() =>
        Assert.Equal([0, 1, 14], Run("\t\tHello world!\t", RenderWhitespace.Boundary));

    // Monaco: 'ws-mixed'
    [Fact]
    public void Boundary_MixedLeadingAndTrailing() =>
        Assert.Equal([.. Seq(0, 5), .. Seq(18, 30)], Run("  \t\t  Hello world! \t  \t   \t    ", RenderWhitespace.Boundary));

    // Monaco: 'ws-faux-indent'
    [Fact]
    public void FauxIndent_NeverRendered() =>
        Assert.Equal([2, 3, .. Seq(16, 28)], Run("\t\t  Hello world! \t  \t   \t    ", RenderWhitespace.Boundary, faux: 2));

    // Monaco: 'ws-middle'
    [Fact]
    public void Boundary_InnerSingleSpaceNotRendered_RunsRendered() =>
        Assert.Equal([2, 3, 9, 10], Run("it  it it  it", RenderWhitespace.Boundary));

    // Monaco: 'ws-all-middle'
    [Fact]
    public void All_RendersEverySpaceAndTab() =>
        Assert.Equal([0, 6, 13], Run(" Hello world!\t", RenderWhitespace.All));

    // Monaco: 'ws-sel-none'
    [Fact]
    public void Selection_WithoutSelections_ReturnsEmpty() =>
        Assert.Empty(Run(" Hello world!\t", RenderWhitespace.Selection));

    // Monaco: 'ws-sel-whole'
    [Fact]
    public void Selection_WholeLine() =>
        Assert.Equal([0, 6, 13], Run(" Hello world!\t", RenderWhitespace.Selection, sel: [new(0, 14)]));

    // Monaco: 'ws-sel-partial'
    [Fact]
    public void Selection_Partial() =>
        Assert.Equal([0], Run(" Hello world!\t", RenderWhitespace.Selection, sel: [new(0, 5)]));

    // Monaco: 'ws-sel-multiple'
    [Fact]
    public void Selection_Multiple() =>
        Assert.Equal([0, 13], Run(" Hello world!\t", RenderWhitespace.Selection, sel: [new(0, 5), new(9, 14)]));

    // Monaco: 'ws-sel-unsorted'
    [Fact]
    public void Selection_Unsorted() =>
        Assert.Equal([0, 13], Run(" Hello world!\t", RenderWhitespace.Selection, sel: [new(9, 14), new(0, 5)]));

    // Monaco: 'ws-sel-adjacent'
    [Fact]
    public void Selection_Adjacent() =>
        Assert.Equal([0, 2], Run(" * S", RenderWhitespace.Selection, sel: [new(0, 1), new(1, 2), new(2, 3)]));

    // Monaco: 'ws-trail-no-trail'
    [Fact]
    public void Trailing_NoTrailingWhitespace_ReturnsEmpty() =>
        Assert.Empty(Run(" Hello world!", RenderWhitespace.Trailing));

    // Monaco: 'ws-trail-with-trail'
    [Fact]
    public void Trailing_WithTrailingWhitespace() =>
        Assert.Equal([13, 14], Run(" Hello world! \t", RenderWhitespace.Trailing));

    // Monaco: 'ws-trail-8-8'
    [Fact]
    public void Trailing_EightAndEight() =>
        Assert.Equal(Seq(20, 27), Run("        Hello world!        ", RenderWhitespace.Trailing));

    // Monaco: 'ws-trail-only'
    [Fact]
    public void WhitespaceOnlyLine_Trailing_AllRendered() =>
        Assert.Equal([0, 1, 2], Run(" \t ", RenderWhitespace.Trailing));

    [Fact]
    public void None_ReturnsEmpty() =>
        Assert.Empty(Run("  a  ", RenderWhitespace.None));

    [Fact]
    public void Trailing_ContinuesWithWrappedLine_ReturnsEmpty() =>
        Assert.Empty(Run("a  ", RenderWhitespace.Trailing, wrapped: true));

    [Fact]
    public void Boundary_ContinuesWithWrappedLine_SingleTrailingSpace_NotRendered() =>
        Assert.Empty(Run("ab ", RenderWhitespace.Boundary, wrapped: true));

    [Fact]
    public void Boundary_ContinuesWithWrappedLine_TwoTrailingSpaces_Rendered() =>
        Assert.Equal([2, 3], Run("ab  ", RenderWhitespace.Boundary, wrapped: true));

    [Fact]
    public void Length_CutsTheLine() =>
        Assert.Equal([1], Run("a b  ", RenderWhitespace.All, length: 3));

    [Fact]
    public void EmptyLine_ReturnsEmpty() =>
        Assert.Empty(Run("", RenderWhitespace.All));

    [Fact]
    public void SelectionsOnLine_MultiLineSelection_UsesMinAndMaxColumn()
    {
        var selections = new[] { new Selection(1, 3, 3, 2) };
        var middle = RenderWhitespaceComputer.SelectionsOnLine(selections, 2, 1, 9);
        Assert.Equal([new OffsetRange(0, 8)], middle);
        Assert.Equal([new OffsetRange(2, 8)], RenderWhitespaceComputer.SelectionsOnLine(selections, 1, 1, 9));
        Assert.Equal([new OffsetRange(0, 1)], RenderWhitespaceComputer.SelectionsOnLine(selections, 3, 1, 9));
        Assert.Null(RenderWhitespaceComputer.SelectionsOnLine(selections, 4, 1, 9));
    }

    [Fact]
    public void SelectionsOnLine_EmptySelection_Skipped() =>
        Assert.Null(RenderWhitespaceComputer.SelectionsOnLine([new Selection(1, 2, 1, 2)], 1, 1, 9));
}
