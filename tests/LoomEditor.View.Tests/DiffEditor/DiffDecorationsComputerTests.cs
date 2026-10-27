using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffDecorationsComputerTests
{
    private static DiffState State(params DetailedLineRangeMapping[] changes) =>
        new([.. changes.Select(c => new DiffMapping(c))], [], false, false);

    private static (IReadOnlyList<ModelDeltaDecoration> O, IReadOnlyList<ModelDeltaDecoration> M) Run(
        DiffState s, bool indicators = true, bool empty = true, int oc = 100, int mc = 100) =>
        DiffDecorationsComputer.Compute(s, indicators, empty, oc, mc);

    private static DetailedLineRangeMapping Map(LineRange o, LineRange m, IReadOnlyList<RangeMapping>? inner) => new(o, m, inner);

    private static Range Lines(int start, int endInclusive) => new(start, 1, endInclusive, int.MaxValue);

    [Fact]
    public void Compute_NoDiff_Empty()
    {
        var (o, m) = Run(State());
        Assert.Empty(o);
        Assert.Empty(m);
    }

    [Fact]
    public void Compute_Insertion_ModifiedGetsLineAndWholeLineAdd()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 2), new LineRange(2, 4), null)));
        Assert.Empty(o);
        Assert.Equal(
            [new ModelDeltaDecoration(Lines(2, 3), DiffDecorationOptions.LineAddBackgroundWithIndicator),
             new ModelDeltaDecoration(Lines(2, 3), DiffDecorationOptions.WholeLineAddDecoration)], m);
    }

    [Fact]
    public void Compute_Deletion_OriginalGetsLineAndWholeLineDelete()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 4), new LineRange(2, 2), null)));
        Assert.Empty(m);
        Assert.Equal(
            [new ModelDeltaDecoration(Lines(2, 3), DiffDecorationOptions.LineDeleteBackgroundWithIndicator),
             new ModelDeltaDecoration(Lines(2, 3), DiffDecorationOptions.WholeLineDeleteDecoration)], o);
    }

    [Fact]
    public void Compute_Replacement_InnerChangesBecomeCharDecorations()
    {
        var oi = new Range(2, 2, 2, 4);
        var mi = new Range(2, 2, 2, 6);
        var (o, m) = Run(State(Map(new LineRange(2, 3), new LineRange(2, 3), [new RangeMapping(oi, mi)])));
        Assert.Equal(
            [new ModelDeltaDecoration(Lines(2, 2), DiffDecorationOptions.LineDeleteBackgroundWithIndicator),
             new ModelDeltaDecoration(oi, DiffDecorationOptions.DeleteDecoration)], o);
        Assert.Equal(
            [new ModelDeltaDecoration(Lines(2, 2), DiffDecorationOptions.LineAddBackgroundWithIndicator),
             new ModelDeltaDecoration(mi, DiffDecorationOptions.AddDecoration)], m);
    }

    [Fact]
    public void Compute_EmptyInnerRange_UsesEmptyOption_WhenShowEmptyDecorations()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 3), new LineRange(2, 3),
            [new RangeMapping(new Range(2, 2, 2, 2), new Range(2, 2, 2, 2))])), empty: true);
        Assert.Equal(DiffDecorationOptions.DeleteDecorationEmpty, o[1].Options);
        Assert.Equal(DiffDecorationOptions.AddDecorationEmpty, m[1].Options);
    }

    [Fact]
    public void Compute_EmptyInnerRange_UsesNormalOption_WhenNotShowEmptyDecorations()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 3), new LineRange(2, 3),
            [new RangeMapping(new Range(2, 2, 2, 2), new Range(2, 2, 2, 2))])), empty: false);
        Assert.Equal(DiffDecorationOptions.DeleteDecoration, o[1].Options);
        Assert.Equal(DiffDecorationOptions.AddDecoration, m[1].Options);
    }

    // Monaco: "Don't show empty markers outside the line range"
    [Fact]
    public void Compute_InnerChangeOutsideLineRange_Skipped()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 3), new LineRange(2, 3),
            [new RangeMapping(new Range(3, 1, 3, 1), new Range(3, 1, 3, 1))])));
        Assert.Single(o);
        Assert.Single(m);
    }

    [Fact]
    public void Compute_RenderIndicatorsFalse_PlainBackgrounds()
    {
        var (o, m) = Run(State(Map(new LineRange(2, 3), new LineRange(2, 3), null)), indicators: false);
        Assert.Equal(DiffDecorationOptions.LineDeleteBackground, o[0].Options);
        Assert.Equal(DiffDecorationOptions.LineAddBackground, m[0].Options);
    }

    [Fact]
    public void Compute_StaleRangesBeyondModel_ClampedOrDropped()
    {
        var (o, m) = Run(State(
            Map(new LineRange(2, 8), new LineRange(2, 3), null),
            Map(new LineRange(9, 10), new LineRange(9, 10), null)), oc: 5, mc: 5);
        Assert.Equal(Lines(2, 5), o[0].Range);
        Assert.All(o, d => Assert.True(d.Range.EndLineNumber <= 5));
        Assert.DoesNotContain(m, d => d.Range.StartLineNumber > 5);
        Assert.Single(o);
        Assert.Single(m);
    }
}
