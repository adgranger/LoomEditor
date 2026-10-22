using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

public class MinimapSamplingStateTests
{
    private const int Height = 600;

    // pixelRatio 1, no padding: the minimap shows `Height` lines, so N view lines sample at ratio N / Height.
    private static MinimapOptions CreateOptions(bool isSampling)
    {
        var layout = new EditorLayoutInfo(
            Width: 800, Height: Height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.Text, 686, 100, true, isSampling, 1, 2, 100, Height, 100, Height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, Height, 0, 0));
        var theme = EditorTheme.Dark;
        return new MinimapOptions(
            layout, new EditorMinimapOptions(), FontInfo.Default, false, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    [Fact]
    public void Compute_NotSampling_ReturnsNullState()
    {
        (MinimapSamplingState? state, IReadOnlyList<SamplingStateEvent> events) =
            MinimapSamplingState.Compute(CreateOptions(isSampling: false), 3000, null);

        Assert.Null(state);
        Assert.Empty(events);
    }

    [Fact]
    public void Compute_Sampling_TakesEvenlySpacedLines()
    {
        (MinimapSamplingState? state, IReadOnlyList<SamplingStateEvent> events) =
            MinimapSamplingState.Compute(CreateOptions(isSampling: true), 3000, null);

        Assert.NotNull(state);
        Assert.Empty(events);
        Assert.Equal(5, state.SamplingRatio);
        Assert.Equal(Height, state.MinimapLines.Count);
        Assert.Equal(3, state.MinimapLines[0]); // round(0 * 5 + 2.5)
        Assert.Equal(8, state.MinimapLines[1]);
        Assert.Equal(3000, state.MinimapLines[Height - 1]);
    }

    [Fact]
    public void Compute_FromOldState_KeepsCommonSamplesAndEmitsEvents()
    {
        MinimapOptions options = CreateOptions(isSampling: true);
        MinimapSamplingState old = MinimapSamplingState.Compute(options, 3000, null).State!;

        (MinimapSamplingState? same, IReadOnlyList<SamplingStateEvent> sameEvents) = MinimapSamplingState.Compute(options, 3000, old);
        Assert.Equal(old.MinimapLines, same!.MinimapLines);
        Assert.Empty(sameEvents);

        // the sampling ratio changes by a little: most samples are reused, the difference is reported.
        (MinimapSamplingState? grown, IReadOnlyList<SamplingStateEvent> grownEvents) = MinimapSamplingState.Compute(options, 3004, old);
        Assert.NotNull(grown);
        Assert.Equal(Height, grown.MinimapLines.Count);
        Assert.Equal(3000, grown.MinimapLines[Height - 1]); // the old last sample is still within reach, so it is reused
        Assert.Equal(3, grown.MinimapLines[0]);
        Assert.NotEmpty(grownEvents);

        // a much larger document replaces too many samples: a single flush.
        (_, IReadOnlyList<SamplingStateEvent> flushEvents) = MinimapSamplingState.Compute(options, 30000, old);
        Assert.Equal([new SamplingStateFlush()], flushEvents);
    }

    [Fact]
    public void ModelLineToMinimapLine_FindsTheClosestSample()
    {
        MinimapSamplingState state = MinimapSamplingState.Compute(CreateOptions(isSampling: true), 3000, null).State!;

        Assert.Equal(1, state.ModelLineToMinimapLine(1));
        Assert.Equal(2, state.ModelLineToMinimapLine(8));
        Assert.Equal(Height, state.ModelLineToMinimapLine(3000));
        Assert.Equal(Height, state.ModelLineToMinimapLine(5000)); // clamped
        Assert.Null(state.ModelLineRangeToMinimapLineRange(4, 7));
        Assert.Equal((2, 2), state.ModelLineRangeToMinimapLineRange(8, 8));
        Assert.Equal((2, 3), state.ModelLineRangeToMinimapLineRange(8, 13));
        Assert.Equal((1, 2), state.DecorationLineRangeToMinimapLineRange(1, 2));
    }

    [Fact]
    public void OnLinesInserted_ShiftsTheSamplesAfter()
    {
        MinimapSamplingState state = MinimapSamplingState.Compute(CreateOptions(isSampling: true), 3000, null).State!;

        state.OnLinesInserted(6, 7);

        Assert.Equal(3, state.MinimapLines[0]);
        Assert.Equal(10, state.MinimapLines[1]);
        Assert.Equal(3002, state.MinimapLines[Height - 1]);
    }

    [Fact]
    public void OnLinesDeleted_RemovesAndShifts()
    {
        MinimapSamplingState state = MinimapSamplingState.Compute(CreateOptions(isSampling: true), 3000, null).State!;

        // samples 3, 8, 13, 18: lines 7..13 are deleted, so 8 and 13 move to line 6 and 18 shifts by 7.
        (int start, int end) = state.OnLinesDeleted(7, 13);

        Assert.Equal((1, 2), (start, end));
        Assert.Equal(3, state.MinimapLines[0]);
        Assert.Equal(6, state.MinimapLines[1]);
        Assert.Equal(6, state.MinimapLines[2]);
        Assert.Equal(11, state.MinimapLines[3]);
        Assert.Equal(2993, state.MinimapLines[Height - 1]);
    }
}
