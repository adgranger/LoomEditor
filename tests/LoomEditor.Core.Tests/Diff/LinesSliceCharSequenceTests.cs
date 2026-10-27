using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class LinesSliceCharSequenceTests
{
    private static LinesSliceCharSequence Seq(string[] lines, bool ws = true) =>
        new(lines, new Range(1, 1, lines.Length, int.MaxValue), ws);

    [Fact]
    public void Length_TrimsLeadingWhitespace_WhenNotConsideringWhitespace()
    {
        Assert.Equal("ab", Seq(["  ab  "], ws: false).Text);
        Assert.Equal("  ab  ", Seq(["  ab  "], ws: true).Text);
    }

    [Fact]
    public void Length_MultipleLines_JoinsWithLineFeed()
    {
        LinesSliceCharSequence s = Seq(["ab", "cd"]);
        Assert.Equal(5, s.Length);
        Assert.Equal("ab\ncd", s.Text);
    }

    [Fact]
    public void Constructor_RangeInsideLines_SlicesFirstAndLast()
    {
        var s = new LinesSliceCharSequence(["abcd", "efgh"], new Range(1, 3, 2, 3), true);
        Assert.Equal("cd\nef", s.Text);
    }

    [Fact]
    public void TranslateOffset_MultipleLines()
    {
        LinesSliceCharSequence s = Seq(["ab", "cd"]);
        Assert.Equal(new Position(1, 3), s.TranslateOffset(2));
        Assert.Equal(new Position(2, 1), s.TranslateOffset(3));
        Assert.Equal(new Position(2, 2), s.TranslateOffset(4));
    }

    [Fact]
    public void TranslateOffset_TrimmedIndent_ShiftsRightButNotLeft()
    {
        LinesSliceCharSequence s = Seq(["x", "  cd"], ws: false);
        Assert.Equal(new Position(2, 3), s.TranslateOffset(2));
        Assert.Equal(new Position(2, 1), s.TranslateOffset(2, preferLeft: true));
    }

    [Fact]
    public void TranslateRange_Inside_ReturnsOriginalColumns()
    {
        LinesSliceCharSequence s = Seq(["  abc"], ws: false);
        Assert.Equal(new Range(1, 4, 1, 5), s.TranslateRange(new OffsetRange(1, 2)));
    }

    [Fact]
    public void TranslateRange_EmptyAtLineStart_Collapses()
    {
        LinesSliceCharSequence s = Seq(["x", "  cd"], ws: false);
        Assert.True(s.TranslateRange(new OffsetRange(2, 2)).IsEmpty);
    }

    [Fact]
    public void FindWordContaining_InsideAndOutsideWord()
    {
        LinesSliceCharSequence s = Seq(["foo bar"]);
        Assert.Equal(new OffsetRange(0, 3), s.FindWordContaining(1));
        Assert.Null(s.FindWordContaining(3));
        Assert.Null(s.FindWordContaining(-1));
        Assert.Null(s.FindWordContaining(7));
    }

    [Fact]
    public void FindSubWordContaining_CamelCase()
    {
        LinesSliceCharSequence s = Seq(["fooBarBaz"]);
        Assert.Equal(new OffsetRange(3, 6), s.FindSubWordContaining(4));
    }

    [Fact]
    public void FindSubWordContaining_SnakeCase()
    {
        LinesSliceCharSequence s = Seq(["foo_bar"]);
        Assert.Equal(new OffsetRange(0, 3), s.FindSubWordContaining(1));
        Assert.Null(s.FindSubWordContaining(3));
    }

    [Fact]
    public void GetBoundaryScore_PrefersLineBreakOverWordBoundaryOverInsideWord()
    {
        LinesSliceCharSequence s = Seq(["ab cd", "ef"]);
        int lineBreak = s.GetBoundaryScore(6);
        int wordBoundary = s.GetBoundaryScore(2);
        int insideWord = s.GetBoundaryScore(1);
        Assert.True(lineBreak > wordBoundary);
        Assert.True(wordBoundary > insideWord);
    }

    [Fact]
    public void GetBoundaryScore_BetweenCrAndLf_IsZero()
    {
        var s = new LinesSliceCharSequence(["a\r", "b"], new Range(1, 1, 2, 2), true);
        Assert.Equal(0, s.GetBoundaryScore(2));
    }

    [Fact]
    public void ExtendToFullLines_ExtendsToLineStartsAndNextLineStart()
    {
        LinesSliceCharSequence s = Seq(["ab", "cd", "ef"]);
        Assert.Equal(new OffsetRange(3, 6), s.ExtendToFullLines(new OffsetRange(4, 5)));
        Assert.Equal(new OffsetRange(3, 8), s.ExtendToFullLines(new OffsetRange(4, 7)));
    }

    [Fact]
    public void ExtendToFullLines_LastLine_ExtendsToEnd()
    {
        LinesSliceCharSequence s = Seq(["ab", "cd"]);
        Assert.Equal(new OffsetRange(3, 5), s.ExtendToFullLines(new OffsetRange(4, 5)));
    }

    [Fact]
    public void CountLinesIn_CountsLineBreaks()
    {
        LinesSliceCharSequence s = Seq(["ab", "cd", "ef"]);
        Assert.Equal(0, s.CountLinesIn(new OffsetRange(0, 2)));
        Assert.Equal(2, s.CountLinesIn(new OffsetRange(1, 7)));
    }

    [Fact]
    public void IsStronglyEqual_ComparesElements()
    {
        LinesSliceCharSequence s = Seq(["aba"]);
        Assert.True(s.IsStronglyEqual(0, 2));
        Assert.False(s.IsStronglyEqual(0, 1));
    }

    [Fact]
    public void GetText_Range_ReturnsSubstring()
    {
        Assert.Equal("bc", Seq(["abcd"]).GetText(new OffsetRange(1, 3)));
    }
}
