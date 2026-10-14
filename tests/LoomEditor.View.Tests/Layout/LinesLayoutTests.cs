using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Ported from microsoft/vscode: src/vs/editor/test/common/viewLayout/linesLayout.test.ts
// Suite: 'Editor ViewLayout - LinesLayout'
// The 4 tests that use getLinesViewportData / getWhitespaceAtVerticalOffset / getWhitespaceViewportData belong to P2-05.
public class LinesLayoutTests
{
    private static string InsertWhitespace(LinesLayout linesLayout, int afterLineNumber, int ordinal, int heightInPx, int minWidth)
    {
        string id = null!;
        linesLayout.ChangeWhitespace(accessor => id = accessor.InsertWhitespace(afterLineNumber, ordinal, heightInPx, minWidth));
        return id;
    }

    private static void ChangeOneWhitespace(LinesLayout linesLayout, string id, int newAfterLineNumber, int newHeight)
    {
        linesLayout.ChangeWhitespace(accessor => accessor.ChangeOneWhitespace(id, newAfterLineNumber, newHeight));
    }

    private static void RemoveWhitespace(LinesLayout linesLayout, string id)
    {
        linesLayout.ChangeWhitespace(accessor => accessor.RemoveWhitespace(id));
    }

    // Asserts GetVerticalOffsetForLineNumber(1..n) == expected[0..n-1].
    private static void AssertOffsets(LinesLayout linesLayout, params int[] expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], linesLayout.GetVerticalOffsetForLineNumber(i + 1));
        }
    }

    // Each pair is (verticalOffset, expectedLineNumber).
    private static void AssertLineAtOffset(LinesLayout linesLayout, params (int Offset, int Line)[] expected)
    {
        foreach (var (offset, line) in expected)
        {
            Assert.Equal(line, linesLayout.GetLineNumberAtOrAfterVerticalOffset(offset));
        }
    }

    // Each pair is (verticalOffset, expectedWhitespaceIndex).
    private static void AssertWhitespaceAtOffset(LinesLayout linesLayout, params (int Offset, int Index)[] expected)
    {
        foreach (var (offset, index) in expected)
        {
            Assert.Equal(index, linesLayout.GetWhitespaceIndexAtOrAfterVerticallOffset(offset));
        }
    }

    // Asserts (afterLineNumber, height) of every whitespace, the accumulated heights and the total.
    private static void AssertWhitespaces(LinesLayout linesLayout, (int After, int Height)[] whitespaces, int[] accumulated, int[] beforeLineNumber)
    {
        Assert.Equal(whitespaces.Length, linesLayout.GetWhitespacesCount());
        for (var i = 0; i < whitespaces.Length; i++)
        {
            Assert.Equal(whitespaces[i].After, linesLayout.GetAfterLineNumberForWhitespaceIndex(i));
            Assert.Equal(whitespaces[i].Height, linesLayout.GetHeightForWhitespaceIndex(i));
        }

        for (var i = 0; i < accumulated.Length; i++)
        {
            Assert.Equal(accumulated[i], linesLayout.GetWhitespacesAccumulatedHeight(i));
        }

        Assert.Equal(accumulated[^1], linesLayout.GetWhitespacesTotalHeight());
        for (var i = 0; i < beforeLineNumber.Length; i++)
        {
            Assert.Equal(beforeLineNumber[i], linesLayout.GetWhitespaceAccumulatedHeightBeforeLineNumber(i + 1));
        }
    }

    private static void AssertIds(LinesLayout linesLayout, params string[] expectedIds)
    {
        for (var i = 0; i < expectedIds.Length; i++)
        {
            Assert.Equal(expectedIds[i], linesLayout.GetIdForWhitespaceIndex(i));
        }
    }

    // Each pair is (afterLineNumber, expectedIndex) for GetFirstWhitespaceIndexAfterLineNumber.
    private static void AssertFirstAfter(LinesLayout linesLayout, params int[] expectedForLines1To8)
    {
        for (var i = 0; i < expectedForLines1To8.Length; i++)
        {
            Assert.Equal(expectedForLines1To8[i], linesLayout.GetFirstWhitespaceIndexAfterLineNumber(i + 1));
        }
    }

    // Monaco: 'LinesLayout 1'
    [Fact]
    public void LinesLayout1()
    {
        // Start off with 10 lines
        var linesLayout = new LinesLayout(10, 10, 0, 0, []);

        // lines: [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
        // whitespace: -
        Assert.Equal(100, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90);
        AssertLineAtOffset(linesLayout, (0, 1), (1, 1), (5, 1), (9, 1), (10, 2), (11, 2), (15, 2), (19, 2), (20, 3), (21, 3), (29, 3));

        // Add whitespace of height 5px after 2nd line
        InsertWhitespace(linesLayout, 2, 0, 5, 0);
        // whitespace: a(2,5)
        Assert.Equal(105, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 10, 25, 35, 45);
        AssertLineAtOffset(
            linesLayout, (0, 1), (1, 1), (9, 1), (10, 2), (20, 3), (21, 3), (24, 3), (25, 3), (35, 4), (45, 5), (104, 10), (105, 10));

        // Add two more whitespaces of height 5px
        InsertWhitespace(linesLayout, 3, 0, 5, 0);
        InsertWhitespace(linesLayout, 4, 0, 5, 0);
        // whitespace: a(2,5), b(3, 5), c(4, 5)
        Assert.Equal(115, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 10, 25, 40, 55, 65);
        AssertLineAtOffset(
            linesLayout, (0, 1), (1, 1), (9, 1), (10, 2), (19, 2), (20, 3), (34, 3), (35, 4), (49, 4), (50, 5), (64, 5), (65, 6));

        Assert.Equal(20, linesLayout.GetVerticalOffsetForWhitespaceIndex(0)); // 20 -> 25
        Assert.Equal(35, linesLayout.GetVerticalOffsetForWhitespaceIndex(1)); // 35 -> 40
        Assert.Equal(50, linesLayout.GetVerticalOffsetForWhitespaceIndex(2));

        AssertWhitespaceAtOffset(
            linesLayout,
            (0, 0), (19, 0), (20, 0), (21, 0), (22, 0), (23, 0), (24, 0), (25, 1), (26, 1), (34, 1), (35, 1), (36, 1), (39, 1),
            (40, 2), (41, 2), (49, 2), (50, 2), (51, 2), (54, 2), (55, -1), (1000, -1));
    }

    // Monaco: 'LinesLayout 2'
    [Fact]
    public void LinesLayout2()
    {
        // Start off with 10 lines and one whitespace after line 2, of height 5
        var linesLayout = new LinesLayout(10, 1, 0, 0, []);
        var a = InsertWhitespace(linesLayout, 2, 0, 5, 0);

        // 10 lines
        // whitespace: - a(2,5)
        Assert.Equal(15, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 7, 8, 9, 10, 11, 12, 13, 14);

        // Change whitespace height
        // whitespace: - a(2,10)
        ChangeOneWhitespace(linesLayout, a, 2, 10);
        Assert.Equal(20, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 12, 13, 14, 15, 16, 17, 18, 19);

        // Change whitespace position
        // whitespace: - a(5,10)
        ChangeOneWhitespace(linesLayout, a, 5, 10);
        Assert.Equal(20, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 2, 3, 4, 15, 16, 17, 18, 19);

        // Pretend that lines 5 and 6 were deleted
        // 8 lines
        // whitespace: - a(4,10)
        linesLayout.OnLinesDeleted(5, 6);
        Assert.Equal(18, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 2, 3, 14, 15, 16, 17);

        // Insert two lines at the beginning
        // 10 lines
        // whitespace: - a(6,10)
        linesLayout.OnLinesInserted(1, 2);
        Assert.Equal(20, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 2, 3, 4, 5, 16, 17, 18, 19);

        // Remove whitespace
        // 10 lines
        RemoveWhitespace(linesLayout, a);
        Assert.Equal(10, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
    }

    // Monaco: 'LinesLayout Padding'
    [Fact]
    public void LinesLayoutPadding()
    {
        // Start off with 10 lines
        var linesLayout = new LinesLayout(10, 10, 15, 20, []);

        // whitespace: -
        Assert.Equal(135, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 15, 25, 35, 45, 55, 65, 75, 85, 95, 105);
        AssertLineAtOffset(linesLayout, (0, 1), (10, 1), (15, 1), (24, 1), (25, 2), (34, 2), (35, 3));

        // Add whitespace of height 5px after 2nd line
        InsertWhitespace(linesLayout, 2, 0, 5, 0);
        // whitespace: a(2,5)
        Assert.Equal(140, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 15, 25, 40, 50);
        AssertLineAtOffset(
            linesLayout, (0, 1), (10, 1), (25, 2), (34, 2), (35, 3), (39, 3), (40, 3), (41, 3), (49, 3), (50, 4));

        // Add two more whitespaces of height 5px
        InsertWhitespace(linesLayout, 3, 0, 5, 0);
        InsertWhitespace(linesLayout, 4, 0, 5, 0);
        // whitespace: a(2,5), b(3, 5), c(4, 5)
        Assert.Equal(150, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 15, 25, 40, 55, 70, 80);
        AssertLineAtOffset(
            linesLayout,
            (0, 1), (15, 1), (24, 1), (30, 2), (35, 3), (39, 3), (40, 3), (49, 3), (50, 4), (54, 4), (55, 4), (64, 4), (65, 5),
            (69, 5), (70, 5), (80, 6));

        Assert.Equal(35, linesLayout.GetVerticalOffsetForWhitespaceIndex(0)); // 35 -> 40
        Assert.Equal(50, linesLayout.GetVerticalOffsetForWhitespaceIndex(1)); // 50 -> 55
        Assert.Equal(65, linesLayout.GetVerticalOffsetForWhitespaceIndex(2));

        AssertWhitespaceAtOffset(
            linesLayout,
            (0, 0), (34, 0), (35, 0), (39, 0), (40, 1), (49, 1), (50, 1), (54, 1), (55, 2), (64, 2), (65, 2), (70, -1));
    }

    // Monaco: 'LinesLayout getLineNumberAtOrAfterVerticalOffset'
    [Fact]
    public void GetLineNumberAtOrAfterVerticalOffset_WithWhitespace_FindsLine()
    {
        var linesLayout = new LinesLayout(10, 1, 0, 0, []);
        InsertWhitespace(linesLayout, 6, 0, 10, 0);

        // 10 lines
        // whitespace: - a(6,10)
        Assert.Equal(20, linesLayout.GetLinesTotalHeight());
        AssertOffsets(linesLayout, 0, 1, 2, 3, 4, 5, 16, 17, 18, 19);

        // Do some hit testing
        // line      [1, 2, 3, 4, 5, 6,  7,  8,  9, 10]
        // vertical: [0, 1, 2, 3, 4, 5, 16, 17, 18, 19]
        AssertLineAtOffset(
            linesLayout,
            (-100, 1), (-1, 1), (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 7), (8, 7), (9, 7), (10, 7), (11, 7),
            (12, 7), (13, 7), (14, 7), (15, 7), (16, 7), (17, 8), (18, 9), (19, 10), (20, 10), (21, 10), (22, 10), (23, 10));
    }

    // Monaco: 'LinesLayout'
    [Fact]
    public void WhitespaceLifecycle_InsertChangeRemoveAndLineChanges_KeepsAccumulatedHeights()
    {
        var linesLayout = new LinesLayout(100, 20, 0, 0, []);

        // Insert a whitespace after line number 2, of height 10
        var a = InsertWhitespace(linesLayout, 2, 0, 10, 0);
        // whitespaces: a(2, 10)
        AssertWhitespaces(linesLayout, [(2, 10)], [10], [0, 0, 10, 10]);

        // Insert a whitespace again after line number 2, of height 20
        var b = InsertWhitespace(linesLayout, 2, 0, 20, 0);
        // whitespaces: a(2, 10), b(2, 20)
        AssertWhitespaces(linesLayout, [(2, 10), (2, 20)], [10, 30], [0, 0, 30, 30]);

        // Change last inserted whitespace height to 30
        ChangeOneWhitespace(linesLayout, b, 2, 30);
        // whitespaces: a(2, 10), b(2, 30)
        AssertWhitespaces(linesLayout, [(2, 10), (2, 30)], [10, 40], [0, 0, 40, 40]);

        // Remove last inserted whitespace
        RemoveWhitespace(linesLayout, b);
        // whitespaces: a(2, 10)
        AssertWhitespaces(linesLayout, [(2, 10)], [10], [0, 0, 10, 10]);

        // Add a whitespace before the first line of height 50
        b = InsertWhitespace(linesLayout, 0, 0, 50, 0);
        // whitespaces: b(0, 50), a(2, 10)
        AssertWhitespaces(linesLayout, [(0, 50), (2, 10)], [50, 60], [50, 50, 60, 60]);

        // Add a whitespace after line 4 of height 20
        InsertWhitespace(linesLayout, 4, 0, 20, 0);
        // whitespaces: b(0, 50), a(2, 10), c(4, 20)
        AssertWhitespaces(linesLayout, [(0, 50), (2, 10), (4, 20)], [50, 60, 80], [50, 50, 60, 60, 80]);

        // Add a whitespace after line 3 of height 30
        InsertWhitespace(linesLayout, 3, 0, 30, 0);
        // whitespaces: b(0, 50), a(2, 10), d(3, 30), c(4, 20)
        AssertWhitespaces(linesLayout, [(0, 50), (2, 10), (3, 30), (4, 20)], [50, 60, 90, 110], [50, 50, 60, 90, 110]);

        // Change whitespace after line 2 to height of 100
        ChangeOneWhitespace(linesLayout, a, 2, 100);
        // whitespaces: b(0, 50), a(2, 100), d(3, 30), c(4, 20)
        AssertWhitespaces(linesLayout, [(0, 50), (2, 100), (3, 30), (4, 20)], [50, 150, 180, 200], [50, 50, 150, 180, 200]);

        // Remove whitespace after line 2
        RemoveWhitespace(linesLayout, a);
        // whitespaces: b(0, 50), d(3, 30), c(4, 20)
        AssertWhitespaces(linesLayout, [(0, 50), (3, 30), (4, 20)], [50, 80, 100], [50, 50, 50, 80, 100]);

        // Remove whitespace before line 1
        RemoveWhitespace(linesLayout, b);
        // whitespaces: d(3, 30), c(4, 20)
        AssertWhitespaces(linesLayout, [(3, 30), (4, 20)], [30, 50], [0, 0, 0, 30, 50]);

        // Delete line 1
        linesLayout.OnLinesDeleted(1, 1);
        // whitespaces: d(2, 30), c(3, 20)
        AssertWhitespaces(linesLayout, [(2, 30), (3, 20)], [30, 50], [0, 0, 30, 50, 50]);

        // Insert a line before line 1
        linesLayout.OnLinesInserted(1, 1);
        // whitespaces: d(3, 30), c(4, 20)
        AssertWhitespaces(linesLayout, [(3, 30), (4, 20)], [30, 50], [0, 0, 0, 30, 50]);

        // Delete line 4
        linesLayout.OnLinesDeleted(4, 4);
        // whitespaces: d(3, 30), c(3, 20)
        AssertWhitespaces(linesLayout, [(3, 30), (3, 20)], [30, 50], [0, 0, 0, 50, 50]);
    }

    // Monaco: 'LinesLayout findInsertionIndex'
    [Fact]
    public void FindInsertionIndex_SortedArrays_ReturnsIndexAfterExistingValues()
    {
        static List<EditorWhitespace> Make(int[] afterLineNumbers, int ordinal = 0)
        {
            return [.. afterLineNumbers.Select(afterLineNumber => new EditorWhitespace(string.Empty, afterLineNumber, ordinal, 0, 0))];
        }

        // Every odd number 1, 3, 5, ... with k elements: the insertion index of n is the count of elements <= n.
        Assert.Equal(0, LinesLayout.FindInsertionIndex(Make([]), 0, 0));
        Assert.Equal(0, LinesLayout.FindInsertionIndex(Make([]), 1, 0));
        Assert.Equal(0, LinesLayout.FindInsertionIndex(Make([]), 2, 0));

        for (var count = 1; count <= 8; count++)
        {
            var arr = Make([.. Enumerable.Range(0, count).Select(i => (2 * i) + 1)]);
            for (var n = 0; n <= (2 * count) + 1; n++)
            {
                var expected = Math.Min(count, (n + 1) / 2);
                Assert.Equal(expected, LinesLayout.FindInsertionIndex(arr, n, 0));
            }
        }

        var withOrdinal = Make([1, 3, 5], 3);
        Assert.Equal(0, LinesLayout.FindInsertionIndex(withOrdinal, 0, 0));
        Assert.Equal(0, LinesLayout.FindInsertionIndex(withOrdinal, 1, 0));
        Assert.Equal(1, LinesLayout.FindInsertionIndex(withOrdinal, 2, 0));
        Assert.Equal(1, LinesLayout.FindInsertionIndex(withOrdinal, 3, 0));
        Assert.Equal(2, LinesLayout.FindInsertionIndex(withOrdinal, 4, 0));
        Assert.Equal(2, LinesLayout.FindInsertionIndex(withOrdinal, 5, 0));
        Assert.Equal(3, LinesLayout.FindInsertionIndex(withOrdinal, 6, 0));
    }

    // Monaco: 'LinesLayout changeWhitespaceAfterLineNumber & getFirstWhitespaceIndexAfterLineNumber'
    [Fact]
    public void ChangeWhitespace_AfterLineNumber_ReordersAndFindsFirstWhitespaceAfter()
    {
        var linesLayout = new LinesLayout(100, 20, 0, 0, []);

        var a = InsertWhitespace(linesLayout, 0, 0, 1, 0);
        var b = InsertWhitespace(linesLayout, 7, 0, 1, 0);
        var c = InsertWhitespace(linesLayout, 3, 0, 1, 0);

        AssertIds(linesLayout, a, c, b); // 0, 3, 7
        Assert.Equal(0, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 1, 1, 1, 2, 2, 2, 2, -1); // c c c b b b b --

        // Do not really move a
        ChangeOneWhitespace(linesLayout, a, 1, 1);

        AssertIds(linesLayout, a, c, b); // 1, 3, 7
        Assert.Equal(1, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 0, 1, 1, 2, 2, 2, 2, -1); // a c c b b b b --

        // Do not really move a
        ChangeOneWhitespace(linesLayout, a, 2, 1);

        AssertIds(linesLayout, a, c, b); // 2, 3, 7
        Assert.Equal(2, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 0, 0, 1, 2, 2, 2, 2, -1); // a a c b b b b --

        // Change a to conflict with c => a gets placed after c
        ChangeOneWhitespace(linesLayout, a, 3, 1);

        AssertIds(linesLayout, c, a, b); // 3, 3, 7
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 0, 0, 0, 2, 2, 2, 2, -1); // c c c b b b b --

        // Make a no-op
        ChangeOneWhitespace(linesLayout, c, 3, 1);

        AssertIds(linesLayout, c, a, b); // 3, 3, 7
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 0, 0, 0, 2, 2, 2, 2, -1); // c c c b b b b --

        // Conflict c with b => c gets placed after b
        ChangeOneWhitespace(linesLayout, c, 7, 1);

        AssertIds(linesLayout, a, b, c); // 3, 7, 7
        Assert.Equal(3, linesLayout.GetAfterLineNumberForWhitespaceIndex(0));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(1));
        Assert.Equal(7, linesLayout.GetAfterLineNumberForWhitespaceIndex(2));
        AssertFirstAfter(linesLayout, 0, 0, 0, 1, 1, 1, 1, -1); // a a a b b b b --
    }

    // Monaco: 'LinesLayout Bug'
    [Fact]
    public void InsertWhitespace_MixedPositions_KeepsWhitespacesSorted()
    {
        var linesLayout = new LinesLayout(100, 20, 0, 0, []);

        var a = InsertWhitespace(linesLayout, 0, 0, 1, 0);
        var b = InsertWhitespace(linesLayout, 7, 0, 1, 0);
        AssertIds(linesLayout, a, b); // 0, 7

        var c = InsertWhitespace(linesLayout, 3, 0, 1, 0);
        AssertIds(linesLayout, a, c, b); // 0, 3, 7

        var d = InsertWhitespace(linesLayout, 2, 0, 1, 0);
        AssertIds(linesLayout, a, d, c, b); // 0, 2, 3, 7

        var e = InsertWhitespace(linesLayout, 8, 0, 1, 0);
        AssertIds(linesLayout, a, d, c, b, e); // 0, 2, 3, 7, 8

        var f = InsertWhitespace(linesLayout, 11, 0, 1, 0);
        AssertIds(linesLayout, a, d, c, b, e, f); // 0, 2, 3, 7, 8, 11

        var g = InsertWhitespace(linesLayout, 10, 0, 1, 0);
        AssertIds(linesLayout, a, d, c, b, e, g, f); // 0, 2, 3, 7, 8, 10, 11

        var h = InsertWhitespace(linesLayout, 0, 0, 1, 0);
        AssertIds(linesLayout, a, h, d, c, b, e, g, f); // 0, 0, 2, 3, 7, 8, 10, 11
    }

    [Fact]
    public void GetLinesTotalHeight_TwoAndAHalfMillionLines_DoesNotOverflow()
    {
        var linesLayout = new LinesLayout(2_500_000, 19, 0, 0, []);

        Assert.Equal(47_500_000, linesLayout.GetLinesTotalHeight());
        Assert.Equal(2_500_000, linesLayout.GetLineNumberAtOrAfterVerticalOffset(47_499_999));
        Assert.Equal(1, linesLayout.GetLineNumberAtOrAfterVerticalOffset(0));
        Assert.Equal(47_499_981, linesLayout.GetVerticalOffsetForLineNumber(2_500_000));
    }

    [Fact]
    public void ChangeWhitespace_SeveralChangesAtOnce_RebuildsSortedAndStable()
    {
        var linesLayout = new LinesLayout(100, 10, 0, 0, []);
        string a = null!, b = null!, c = null!;

        var hadAChange = linesLayout.ChangeWhitespace(accessor =>
        {
            a = accessor.InsertWhitespace(5, 1, 10, 3);
            b = accessor.InsertWhitespace(5, 0, 20, 7);
            c = accessor.InsertWhitespace(2, 0, 30, 0);
        });

        Assert.True(hadAChange);
        AssertIds(linesLayout, c, b, a);
        Assert.Equal(60, linesLayout.GetWhitespacesTotalHeight());
        Assert.Equal(7, linesLayout.GetWhitespaceMinWidth());
        Assert.True(linesLayout.HasWhitespace());

        linesLayout.ChangeWhitespace(accessor =>
        {
            accessor.RemoveWhitespace(b);
            accessor.ChangeOneWhitespace(a, 1, 5);
        });

        AssertIds(linesLayout, a, c);
        Assert.Equal(35, linesLayout.GetWhitespacesTotalHeight());
        Assert.Equal(3, linesLayout.GetWhitespaceMinWidth());
        Assert.Equal(2, linesLayout.GetWhitespaces().Count);
    }

    [Fact]
    public void Padding_TopAndBottom_AreDetected()
    {
        var linesLayout = new LinesLayout(10, 10, 15, 20, []);

        Assert.True(linesLayout.IsInTopPadding(14));
        Assert.False(linesLayout.IsInTopPadding(15));
        Assert.True(linesLayout.IsInBottomPadding(115));
        Assert.False(linesLayout.IsInBottomPadding(114));
        Assert.True(linesLayout.IsAfterLines(136));
        Assert.False(linesLayout.IsAfterLines(135));
        Assert.Equal(115, linesLayout.GetVerticalOffsetAfterLineNumber(10));
    }
}
