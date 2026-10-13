// Ported from microsoft/vscode: src/vs/editor/test/common/core/range.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// Each ported test keeps the original Monaco test name in a comment. Tests without such a comment are LoomEditor additions.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class RangeTests
{
    // Monaco: 'empty range'
    [Fact]
    public void Constructor_SamePositions_CreatesEmptyRange()
    {
        var range = new Range(1, 1, 1, 1);

        AssertRange(range, 1, 1, 1, 1);
        Assert.True(range.IsEmpty);
    }

    // Monaco: 'swap start and stop same line'
    [Fact]
    public void Constructor_EndBeforeStartOnSameLine_SwapsPositions()
    {
        var range = new Range(1, 2, 1, 1);

        AssertRange(range, 1, 1, 1, 2);
        Assert.False(range.IsEmpty);
    }

    // Monaco: 'swap start and stop'
    [Fact]
    public void Constructor_EndLineBeforeStartLine_SwapsPositions()
    {
        var range = new Range(2, 1, 1, 2);

        AssertRange(range, 1, 2, 2, 1);
        Assert.False(range.IsEmpty);
    }

    // Monaco: 'no swap same line'
    [Fact]
    public void Constructor_OrderedPositionsOnSameLine_KeepsPositions()
    {
        var range = new Range(1, 1, 1, 2);

        AssertRange(range, 1, 1, 1, 2);
        Assert.False(range.IsEmpty);
    }

    // Monaco: 'no swap'
    [Fact]
    public void Constructor_OrderedPositions_KeepsPositions()
    {
        var range = new Range(1, 1, 2, 1);

        AssertRange(range, 1, 1, 2, 1);
        Assert.False(range.IsEmpty);
    }

    [Theory]
    [InlineData(0, 1, 1, 1)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, 1, 0, 1)]
    [InlineData(1, 1, 1, 0)]
    public void Constructor_ZeroValue_Throws(int startLine, int startColumn, int endLine, int endColumn)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Range(startLine, startColumn, endLine, endColumn));
    }

    // Monaco: 'compareRangesUsingEnds'
    [Theory]
    [InlineData(1, 1, 1, 3, 1, 2, 1, 4, -1)] // a.start < b.start, a.end < b.end
    [InlineData(1, 1, 1, 3, 1, 1, 1, 4, -1)] // a.start = b.start, a.end < b.end
    [InlineData(1, 2, 1, 3, 1, 1, 1, 4, -1)] // a.start > b.start, a.end < b.end
    [InlineData(1, 1, 1, 4, 1, 2, 1, 4, -1)] // a.start < b.start, a.end = b.end
    [InlineData(1, 1, 1, 4, 1, 1, 1, 4, 0)]  // a.start = b.start, a.end = b.end
    [InlineData(1, 2, 1, 4, 1, 1, 1, 4, 1)]  // a.start > b.start, a.end = b.end
    [InlineData(1, 1, 1, 5, 1, 2, 1, 4, 1)]  // a.start < b.start, a.end > b.end
    [InlineData(1, 1, 2, 4, 1, 1, 1, 4, 1)]  // a.start = b.start, a.end > b.end
    [InlineData(1, 2, 5, 1, 1, 1, 1, 4, 1)]  // a.start > b.start, a.end > b.end
    public void CompareUsingEnds_ReturnsExpectedSign(
        int aStartLine, int aStartColumn, int aEndLine, int aEndColumn,
        int bStartLine, int bStartColumn, int bEndLine, int bEndColumn,
        int expectedSign)
    {
        var a = new Range(aStartLine, aStartColumn, aEndLine, aEndColumn);
        var b = new Range(bStartLine, bStartColumn, bEndLine, bEndColumn);

        Assert.Equal(expectedSign, Math.Sign(Range.CompareUsingEnds(a, b)));
    }

    [Fact]
    public void CompareUsingStarts_SortsByStartThenEnd()
    {
        Range[] ranges = [new(2, 1, 2, 5), new(1, 1, 3, 1), new(1, 1, 2, 1)];

        Array.Sort(ranges, Range.CompareUsingStarts);

        Assert.Equal([new(1, 1, 2, 1), new(1, 1, 3, 1), new(2, 1, 2, 5)], ranges);
    }

    // Monaco: 'containsPosition'
    [Theory]
    [InlineData(1, 3, false)]
    [InlineData(2, 1, false)]
    [InlineData(2, 2, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 1, true)]
    [InlineData(5, 9, true)]
    [InlineData(5, 10, true)]
    [InlineData(5, 11, false)]
    [InlineData(6, 1, false)]
    public void Contains_Position(int lineNumber, int column, bool expected)
    {
        Assert.Equal(expected, new Range(2, 2, 5, 10).Contains(new Position(lineNumber, column)));
    }

    [Theory]
    [InlineData(2, 2, false)]
    [InlineData(2, 3, true)]
    [InlineData(5, 9, true)]
    [InlineData(5, 10, false)]
    public void StrictContains_Position_ExcludesEdges(int lineNumber, int column, bool expected)
    {
        Assert.Equal(expected, new Range(2, 2, 5, 10).StrictContains(new Position(lineNumber, column)));
    }

    // Monaco: 'containsRange'
    [Theory]
    [InlineData(1, 3, 2, 2, false)]
    [InlineData(2, 1, 2, 2, false)]
    [InlineData(2, 2, 5, 11, false)]
    [InlineData(2, 2, 6, 1, false)]
    [InlineData(5, 9, 6, 1, false)]
    [InlineData(5, 10, 6, 1, false)]
    [InlineData(2, 2, 5, 10, true)]
    [InlineData(2, 3, 5, 9, true)]
    [InlineData(3, 100, 4, 100, true)]
    public void Contains_Range(int startLine, int startColumn, int endLine, int endColumn, bool expected)
    {
        Assert.Equal(expected, new Range(2, 2, 5, 10).Contains(new Range(startLine, startColumn, endLine, endColumn)));
    }

    [Theory]
    [InlineData(2, 2, 5, 9, false)]
    [InlineData(2, 3, 5, 10, false)]
    [InlineData(2, 3, 5, 9, true)]
    public void StrictContains_Range_ExcludesEdges(int startLine, int startColumn, int endLine, int endColumn, bool expected)
    {
        Assert.Equal(expected, new Range(2, 2, 5, 10).StrictContains(new Range(startLine, startColumn, endLine, endColumn)));
    }

    // Monaco: 'areIntersecting'
    [Theory]
    [InlineData(2, 2, 3, 2, 4, 2, 5, 2, false)]
    [InlineData(4, 2, 5, 2, 2, 2, 3, 2, false)]
    [InlineData(4, 2, 5, 2, 5, 2, 6, 2, false)]
    [InlineData(5, 2, 6, 2, 4, 2, 5, 2, false)]
    [InlineData(2, 2, 2, 7, 2, 4, 2, 6, true)]
    [InlineData(2, 2, 2, 7, 2, 4, 2, 9, true)]
    [InlineData(2, 4, 2, 9, 2, 2, 2, 7, true)]
    public void Intersects(
        int aStartLine, int aStartColumn, int aEndLine, int aEndColumn,
        int bStartLine, int bStartColumn, int bEndLine, int bEndColumn,
        bool expected)
    {
        var a = new Range(aStartLine, aStartColumn, aEndLine, aEndColumn);
        var b = new Range(bStartLine, bStartColumn, bEndLine, bEndColumn);

        Assert.Equal(expected, a.Intersects(b));
    }

    [Fact]
    public void IntersectsOrTouches_TouchingRanges_ReturnsTrue()
    {
        var a = new Range(4, 2, 5, 2);
        var b = new Range(5, 2, 6, 2);

        Assert.False(a.Intersects(b));
        Assert.True(a.IntersectsOrTouches(b));
        Assert.True(b.IntersectsOrTouches(a));
        Assert.False(a.IntersectsOrTouches(new Range(5, 3, 6, 2)));
    }

    [Fact]
    public void Intersect_OverlappingRanges_ReturnsOverlap()
    {
        Assert.Equal(new Range(2, 4, 2, 7), new Range(2, 2, 2, 7).Intersect(new Range(2, 4, 2, 9)));
        Assert.Equal(new Range(3, 1, 4, 5), new Range(1, 1, 4, 5).Intersect(new Range(3, 1, 8, 1)));
    }

    [Fact]
    public void Intersect_TouchingRanges_ReturnsEmptyRange()
    {
        Assert.Equal(new Range(5, 2, 5, 2), new Range(4, 2, 5, 2).Intersect(new Range(5, 2, 6, 2)));
    }

    [Fact]
    public void Intersect_DisjointRanges_ReturnsNull()
    {
        Assert.Null(new Range(2, 2, 3, 2).Intersect(new Range(4, 2, 5, 2)));
    }

    [Fact]
    public void Plus_ReturnsCoveringRange()
    {
        Assert.Equal(new Range(1, 5, 4, 2), new Range(1, 5, 2, 1).Plus(new Range(3, 1, 4, 2)));
        Assert.Equal(new Range(1, 2, 1, 9), new Range(1, 4, 1, 9).Plus(new Range(1, 2, 1, 5)));
    }

    [Fact]
    public void SpansMultipleLines()
    {
        Assert.False(new Range(1, 1, 1, 10).SpansMultipleLines);
        Assert.True(new Range(1, 10, 2, 1).SpansMultipleLines);
    }

    [Fact]
    public void Collapse_ReturnsEmptyRangeAtEdge()
    {
        var range = new Range(1, 2, 3, 4);

        Assert.Equal(new Range(1, 2, 1, 2), range.CollapseToStart());
        Assert.Equal(new Range(3, 4, 3, 4), range.CollapseToEnd());
    }

    [Fact]
    public void WithStartAndWithEnd_NormalizeTheResult()
    {
        var range = new Range(2, 2, 3, 3);

        Assert.Equal(new Range(1, 1, 3, 3), range.WithStart(new Position(1, 1)));
        Assert.Equal(new Range(1, 1, 2, 2), range.WithEnd(new Position(1, 1)));
    }

    [Fact]
    public void Delta_MovesLines()
    {
        Assert.Equal(new Range(4, 2, 5, 3), new Range(2, 2, 3, 3).Delta(2));
        Assert.Equal(new Range(1, 2, 2, 3), new Range(2, 2, 3, 3).Delta(-1));
    }

    [Fact]
    public void StartAndEnd_ReturnPositions()
    {
        var range = new Range(3, 4, 1, 2);

        Assert.Equal(new Position(1, 2), range.Start);
        Assert.Equal(new Position(3, 4), range.End);
    }

    [Fact]
    public void ToString_UsesMonacoFormat()
    {
        Assert.Equal("[1,2 -> 3,4]", new Range(1, 2, 3, 4).ToString());
    }

    private static void AssertRange(Range range, int startLine, int startColumn, int endLine, int endColumn)
    {
        Assert.Equal(startLine, range.StartLineNumber);
        Assert.Equal(startColumn, range.StartColumn);
        Assert.Equal(endLine, range.EndLineNumber);
        Assert.Equal(endColumn, range.EndColumn);
    }
}
