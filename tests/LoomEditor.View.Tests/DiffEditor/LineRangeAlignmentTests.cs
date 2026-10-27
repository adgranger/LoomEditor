using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class LineRangeAlignmentTests
{
    private const int Lh = 10;

    private static DiffMapping Diff(int os, int oe, int ms, int me, params (Range Original, Range Modified)[] inner) =>
        new(new DetailedLineRangeMapping(
            new LineRange(os, oe),
            new LineRange(ms, me),
            inner.Length == 0 ? null : [.. inner.Select(i => new RangeMapping(i.Original, i.Modified))]));

    private static IReadOnlyList<LineRangeAlignment> Compute(
        DiffMapping[] diffs,
        AdditionalLineHeight[]? originalExtra = null,
        AdditionalLineHeight[]? modifiedExtra = null,
        bool inner = false) =>
        LineRangeAlignments.Compute(diffs, originalExtra ?? [], modifiedExtra ?? [], Lh, Lh, _ => 10, 100, inner);

    private static void AssertAlignment(LineRangeAlignment a, (int, int) original, (int, int) modified, int originalHeight, int modifiedHeight)
    {
        Assert.Equal(new LineRange(original.Item1, original.Item2), a.OriginalRange);
        Assert.Equal(new LineRange(modified.Item1, modified.Item2), a.ModifiedRange);
        Assert.Equal(originalHeight, a.OriginalHeightInPx);
        Assert.Equal(modifiedHeight, a.ModifiedHeightInPx);
    }

    [Fact]
    public void Compute_NoDiffs_NoAlignments() => Assert.Empty(Compute([]));

    [Fact]
    public void Compute_NoDiffsButExtraHeights_AlignsTheTallerSide()
    {
        var result = Compute([], originalExtra: [new(3, 20)]);

        AssertAlignment(Assert.Single(result), (3, 4), (3, 4), Lh + 20, Lh);
        Assert.Null(result[0].Diff);
    }

    [Fact]
    public void Compute_Insertion_ModifiedLonger() =>
        AssertAlignment(Assert.Single(Compute([Diff(2, 2, 2, 5)])), (2, 2), (2, 5), 0, 3 * Lh);

    [Fact]
    public void Compute_Deletion() =>
        AssertAlignment(Assert.Single(Compute([Diff(2, 5, 2, 2)])), (2, 5), (2, 2), 3 * Lh, 0);

    [Fact]
    public void Compute_Replacement_SameLineCount() =>
        AssertAlignment(Assert.Single(Compute([Diff(2, 4, 2, 4)])), (2, 4), (2, 4), 2 * Lh, 2 * Lh);

    [Fact]
    public void Compute_Replacement_DifferentLineCount_ExtraSpaceOnShorterSide() =>
        AssertAlignment(Assert.Single(Compute([Diff(2, 4, 2, 7)])), (2, 4), (2, 7), 2 * Lh, 5 * Lh);

    [Fact]
    public void Compute_InnerHunkAlignment_SplitsAtUnchangedTextOnTheLine()
    {
        DiffMapping diff = Diff(2, 5, 2, 5, (new Range(3, 4, 3, 6), new Range(3, 4, 3, 6)));

        var result = Compute([diff], inner: true);

        Assert.Equal(2, result.Count);
        AssertAlignment(result[0], (2, 3), (2, 3), Lh, Lh);
        AssertAlignment(result[1], (3, 5), (3, 5), 2 * Lh, 2 * Lh);
    }

    [Fact]
    public void Compute_InnerHunkAlignment_Off_SingleAlignmentPerDiff()
    {
        DiffMapping diff = Diff(2, 5, 2, 5, (new Range(3, 4, 3, 6), new Range(3, 4, 3, 6)));

        AssertAlignment(Assert.Single(Compute([diff])), (2, 5), (2, 5), 3 * Lh, 3 * Lh);
    }

    [Fact]
    public void Compute_ExtraHeightInsideDiff_AddedToTheRightBlock()
    {
        var result = Compute([Diff(2, 4, 2, 4)], originalExtra: [new(2, 15)]);

        AssertAlignment(Assert.Single(result), (2, 4), (2, 4), 2 * Lh + 15, 2 * Lh);
    }

    [Fact]
    public void Compute_ExtraHeightsOnBothSidesAtSameDistance_Paired()
    {
        var result = Compute([], originalExtra: [new(3, 5)], modifiedExtra: [new(3, 7)]);

        AssertAlignment(Assert.Single(result), (3, 4), (3, 4), Lh + 5, Lh + 7);
    }

    [Fact]
    public void Compute_DiffsSortedAndNonOverlapping_RangesAreContiguous()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var rnd = new Random(seed);
            var diffs = new List<DiffMapping>();
            int o = 1;
            int m = 1;
            for (int i = rnd.Next(0, 5); i > 0; i--)
            {
                int gap = rnd.Next(1, 5);
                o += gap;
                m += gap;
                int ol = rnd.Next(0, 4);
                int ml = rnd.Next(ol == 0 ? 1 : 0, 4);
                diffs.Add(Diff(o, o + ol, m, m + ml));
                o += ol;
                m += ml;
            }

            AdditionalLineHeight[] Extra() =>
                [.. Enumerable.Range(0, rnd.Next(0, 4)).Select(_ => rnd.Next(0, 25)).Distinct().Order().Select(l => new AdditionalLineHeight(l, 7))];

            var result = Compute([.. diffs], Extra(), Extra());

            int lastEnd = 0;
            foreach (LineRangeAlignment a in result)
            {
                Assert.True(a.OriginalRange.StartLineNumber >= lastEnd, $"seed {seed}: {a.OriginalRange} overlaps previous end {lastEnd}");
                lastEnd = a.OriginalRange.EndLineNumberExclusive;
            }
        }
    }

    [Fact]
    public void ArrayQueue_TakeWhile_Peek_Dequeue()
    {
        var q = new ArrayQueue<int>([1, 2, 3, 4, 5]);

        Assert.True(q.TryPeek(out int first));
        Assert.Equal(1, first);
        q.Dequeue();
        Assert.Equal([2, 3], q.TakeWhile(v => v < 4));
        Assert.Null(q.TakeWhile(v => v < 4));
        Assert.Equal([5], q.TakeFromEndWhile(v => v > 4));
        Assert.True(q.TryPeek(out int next));
        Assert.Equal(4, next);
        q.Dequeue();
        Assert.False(q.TryPeek(out _));
    }
}
