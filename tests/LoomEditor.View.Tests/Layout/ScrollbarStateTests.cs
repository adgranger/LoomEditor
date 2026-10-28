using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// New tests: Monaco has no test of ScrollbarState. The expected values come from the formulas, by hand.
public class ScrollbarStateTests
{
    private static ScrollbarState State(double visible, double scroll, double position) => new(0, 14, 0, visible, scroll, position);

    [Fact]
    public void SliderSize_ProportionalToVisibleOverScroll() => Assert.Equal(200, State(400, 800, 0).GetSliderSize());

    [Fact]
    public void SliderSize_MinimumTwenty() => Assert.Equal(20, State(100, 1000, 0).GetSliderSize());

    [Fact]
    public void SliderSize_NotNeeded_FullTrack()
    {
        ScrollbarState state = State(100, 80, 0);

        Assert.Equal(100, state.GetSliderSize());
        Assert.Equal(0, state.GetSliderPosition());
        Assert.Equal(0, state.GetDesiredScrollPositionFromOffset(50));
    }

    [Fact]
    public void SliderPosition_AtTop_Zero() => Assert.Equal(0, State(400, 800, 0).GetSliderPosition());

    [Fact]
    public void SliderPosition_AtBottom_TrackMinusSlider()
    {
        ScrollbarState state = State(400, 800, 400);

        Assert.Equal(200, state.GetSliderPosition());
        Assert.Equal(400 - state.GetSliderSize(), state.GetSliderPosition());
    }

    [Fact]
    public void GetDesiredScrollPositionFromOffset_CentersSlider() =>
        Assert.Equal(400, State(400, 800, 0).GetDesiredScrollPositionFromOffset(300));

    [Fact]
    public void GetDesiredScrollPositionFromDelta_Proportional() =>
        Assert.Equal(200, State(400, 800, 100).GetDesiredScrollPositionFromDelta(50));

    [Fact]
    public void SetScrollSize_Change_Recomputes()
    {
        ScrollbarState state = State(400, 800, 0);

        Assert.True(state.SetScrollSize(1600));
        Assert.Equal(100, state.GetSliderSize());
        Assert.False(state.SetScrollSize(1600));
    }
}
