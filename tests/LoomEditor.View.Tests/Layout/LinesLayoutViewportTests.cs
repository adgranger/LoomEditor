using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Ported from microsoft/vscode: src/vs/editor/test/common/viewLayout/linesLayout.test.ts
// Suite: 'Editor ViewLayout - LinesLayout' (the viewport-related tests)
public class LinesLayoutViewportTests
{
    private static string InsertWhitespace(LinesLayout linesLayout, int afterLineNumber, int ordinal, int heightInPx, int minWidth)
    {
        string id = null!;
        linesLayout.ChangeWhitespace(accessor => id = accessor.InsertWhitespace(afterLineNumber, ordinal, heightInPx, minWidth));
        return id;
    }

    private static void AssertViewport(
        LinesLayout linesLayout,
        int offset1,
        int offset2,
        int start,
        int end,
        int completelyStart,
        int completelyEnd,
        int[] relativeOffsets)
    {
        var data = linesLayout.GetLinesViewportData(offset1, offset2);
        var message = $"viewport {offset1}->{offset2}";
        Assert.True(data.StartLineNumber == start, message + " startLineNumber");
        Assert.True(data.EndLineNumber == end, message + " endLineNumber");
        Assert.True(data.CompletelyVisibleStartLineNumber == completelyStart, message + " completelyVisibleStartLineNumber");
        Assert.True(data.CompletelyVisibleEndLineNumber == completelyEnd, message + " completelyVisibleEndLineNumber");
        Assert.True(relativeOffsets.AsSpan().SequenceEqual(data.RelativeVerticalOffset), message + " relativeVerticalOffset");
    }

    // Monaco: 'LinesLayout getCenteredLineInViewport'
    [Fact]
    public void GetCenteredLineInViewport_WithWhitespace_FindsCenteredLine()
    {
        var linesLayout = new LinesLayout(10, 1, 0, 0, []);
        InsertWhitespace(linesLayout, 6, 0, 10, 0);

        // 10 lines
        // whitespace: - a(6,10)
        Assert.Equal(20, linesLayout.GetLinesTotalHeight());
        int[] expectedOffsets = [0, 1, 2, 3, 4, 5, 16, 17, 18, 19];
        for (var i = 0; i < expectedOffsets.Length; i++)
        {
            Assert.Equal(expectedOffsets[i], linesLayout.GetVerticalOffsetForLineNumber(i + 1));
        }

        // Find centered line in viewport 1
        // line      [1, 2, 3, 4, 5, 6,  7,  8,  9, 10]
        // vertical: [0, 1, 2, 3, 4, 5, 16, 17, 18, 19]
        // Index = viewport end - 1; value = expected centered line number.
        int[] centeredForEnd = [1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7];
        for (var end = 1; end <= 33; end++)
        {
            Assert.True(
                centeredForEnd[end - 1] == linesLayout.GetLinesViewportData(0, end).CenteredLineNumber,
                $"viewport 0->{end}");
        }

        // Find centered line in viewport 2
        // Index = viewport start; value = expected centered line number.
        int[] centeredForStart = [7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 9, 9, 10, 10];
        for (var start = 0; start <= 19; start++)
        {
            Assert.True(
                centeredForStart[start] == linesLayout.GetLinesViewportData(start, 20).CenteredLineNumber,
                $"viewport {start}->20");
        }

        Assert.Equal(10, linesLayout.GetLinesViewportData(20, 23).CenteredLineNumber);
        Assert.Equal(10, linesLayout.GetLinesViewportData(21, 23).CenteredLineNumber);
        Assert.Equal(10, linesLayout.GetLinesViewportData(22, 23).CenteredLineNumber);
    }

    // Monaco: 'LinesLayout getLinesViewportData 1'
    [Fact]
    public void GetLinesViewportData_OneWhitespace_ReturnsVisibleLines()
    {
        var linesLayout = new LinesLayout(10, 10, 0, 0, []);
        InsertWhitespace(linesLayout, 6, 0, 100, 0);

        // 10 lines
        // whitespace: - a(6,100)
        Assert.Equal(200, linesLayout.GetLinesTotalHeight());
        int[] expectedOffsets = [0, 10, 20, 30, 40, 50, 160, 170, 180, 190];
        for (var i = 0; i < expectedOffsets.Length; i++)
        {
            Assert.Equal(expectedOffsets[i], linesLayout.GetVerticalOffsetForLineNumber(i + 1));
        }

        AssertViewport(linesLayout, 0, 50, 1, 5, 1, 5, [0, 10, 20, 30, 40]);
        AssertViewport(linesLayout, 1, 51, 1, 6, 2, 5, [0, 10, 20, 30, 40, 50]);
        AssertViewport(linesLayout, 5, 55, 1, 6, 2, 5, [0, 10, 20, 30, 40, 50]);
        AssertViewport(linesLayout, 10, 60, 2, 6, 2, 6, [10, 20, 30, 40, 50]);
        AssertViewport(linesLayout, 50, 100, 6, 6, 6, 6, [50]);
        AssertViewport(linesLayout, 60, 110, 7, 7, 7, 7, [160]);
        AssertViewport(linesLayout, 65, 115, 7, 7, 7, 7, [160]);
        AssertViewport(linesLayout, 50, 159, 6, 6, 6, 6, [50]);
        AssertViewport(linesLayout, 50, 160, 6, 6, 6, 6, [50]);
        AssertViewport(linesLayout, 51, 161, 6, 7, 7, 7, [50, 160]);
        AssertViewport(linesLayout, 150, 169, 7, 7, 7, 7, [160]);
        AssertViewport(linesLayout, 159, 169, 7, 7, 7, 7, [160]);
        AssertViewport(linesLayout, 160, 169, 7, 7, 7, 7, [160]);
        AssertViewport(linesLayout, 160, 1000, 7, 10, 7, 10, [160, 170, 180, 190]);
    }

    // Monaco: 'LinesLayout getLinesViewportData 2 & getWhitespaceViewportData'
    [Fact]
    public void GetLinesViewportData_TwoWhitespaces_ReturnsLinesAndWhitespaces()
    {
        var linesLayout = new LinesLayout(10, 10, 0, 0, []);
        var a = InsertWhitespace(linesLayout, 6, 0, 100, 0);
        var b = InsertWhitespace(linesLayout, 7, 0, 50, 0);

        // 10 lines
        // whitespace: - a(6,100), b(7, 50)
        Assert.Equal(250, linesLayout.GetLinesTotalHeight());
        int[] expectedOffsets = [0, 10, 20, 30, 40, 50, 160, 220, 230, 240];
        for (var i = 0; i < expectedOffsets.Length; i++)
        {
            Assert.Equal(expectedOffsets[i], linesLayout.GetVerticalOffsetForLineNumber(i + 1));
        }

        AssertViewport(linesLayout, 50, 160, 6, 6, 6, 6, [50]);
        Assert.Equal(
            [new ViewWhitespaceViewportData(a, 6, 60, 100)],
            linesLayout.GetWhitespaceViewportData(50, 160));

        AssertViewport(linesLayout, 50, 219, 6, 7, 6, 7, [50, 160]);
        Assert.Equal(
            [new ViewWhitespaceViewportData(a, 6, 60, 100), new ViewWhitespaceViewportData(b, 7, 170, 50)],
            linesLayout.GetWhitespaceViewportData(50, 219));

        AssertViewport(linesLayout, 50, 220, 6, 7, 6, 7, [50, 160]);
        AssertViewport(linesLayout, 50, 250, 6, 10, 6, 10, [50, 160, 220, 230, 240]);
    }

    // Monaco: 'LinesLayout getWhitespaceAtVerticalOffset'
    [Fact]
    public void GetWhitespaceAtVerticalOffset_TwoWhitespaces_FindsExactWhitespace()
    {
        var linesLayout = new LinesLayout(10, 10, 0, 0, []);
        var a = InsertWhitespace(linesLayout, 6, 0, 100, 0);
        var b = InsertWhitespace(linesLayout, 7, 0, 50, 0);

        (int Offset, string? Id)[] expected =
        [
            (0, null), (59, null), (60, a), (61, a), (159, a), (160, null), (161, null),
            (169, null), (170, b), (171, b), (219, b), (220, null),
        ];

        foreach (var (offset, id) in expected)
        {
            Assert.True(id == linesLayout.GetWhitespaceAtVerticalOffset(offset)?.Id, $"offset {offset}");
        }
    }

    [Fact]
    public void GetLinesViewportData_HugeLayout_ReturnsBigNumbersDeltaAndSmallRelativeOffsets()
    {
        // 2,500,000 lines of 20 DIPs: 50,000,000 total.
        var linesLayout = new LinesLayout(2_500_000, 20, 0, 0, []);

        var data = linesLayout.GetLinesViewportData(47_000_000, 47_000_800);

        Assert.NotEqual(0, data.BigNumbersDelta);
        Assert.Equal(0, data.BigNumbersDelta % 20);
        Assert.All(data.RelativeVerticalOffset, offset => Assert.InRange(offset, 0, 999_999));
        Assert.Equal(
            linesLayout.GetVerticalOffsetForLineNumber(data.StartLineNumber) - data.BigNumbersDelta,
            data.RelativeVerticalOffset[0]);
    }
}
