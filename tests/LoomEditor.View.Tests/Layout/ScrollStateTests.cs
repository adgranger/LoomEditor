using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for ScrollState: all tests are new.
public class ScrollStateTests
{
    [Fact]
    public void Constructor_ScrollBeyondContent_IsClamped()
    {
        var state = new ScrollState(false, 100, 300, 500, 50, 120, 500);

        Assert.Equal(200, state.ScrollLeft);
        Assert.Equal(70, state.ScrollTop);
        Assert.Equal(500, state.RawScrollLeft);
        Assert.Equal(500, state.RawScrollTop);
    }

    [Fact]
    public void Constructor_NegativeValues_BecomeZero()
    {
        var state = new ScrollState(false, -5, 300, -10, -7, 120, -3);

        Assert.Equal(0, state.Width);
        Assert.Equal(0, state.Height);
        Assert.Equal(0, state.ScrollLeft);
        Assert.Equal(0, state.ScrollTop);
    }

    [Fact]
    public void Constructor_ForceIntegerValues_TruncatesTowardZero()
    {
        var state = new ScrollState(true, 100.9, 300.9, 10.7, 50.9, 300.9, 20.5);

        Assert.Equal(10, state.ScrollLeft);
        Assert.Equal(20, state.ScrollTop);
        Assert.Equal(100, state.Width);

        var negative = new ScrollState(true, 100, 300, -10.7, 50, 300, -10.7);
        Assert.Equal(-10, negative.RawScrollLeft);
        Assert.Equal(0, negative.ScrollLeft);
        Assert.Equal(0, negative.ScrollTop);
    }

    [Fact]
    public void WithScrollDimensions_UseRawScrollPositions_RestoresClampedPosition()
    {
        var state = new ScrollState(false, 100, 100, 0, 100, 100, 500);
        Assert.Equal(0, state.ScrollTop);

        var raw = state.WithScrollDimensions(new NewScrollDimensions(ScrollHeight: 1000), true);
        var clamped = state.WithScrollDimensions(new NewScrollDimensions(ScrollHeight: 1000), false);

        Assert.Equal(500, raw.ScrollTop);
        Assert.Equal(0, clamped.ScrollTop);
    }

    [Fact]
    public void WithScrollPosition_OnlyTop_KeepsRawLeft()
    {
        var state = new ScrollState(false, 100, 100, 700, 100, 1000, 0);

        var next = state.WithScrollPosition(new NewScrollPosition(ScrollTop: 50));

        Assert.Equal(700, next.RawScrollLeft);
        Assert.Equal(50, next.ScrollTop);
    }

    [Fact]
    public void CreateScrollEvent_OnlyChangedValuesAreFlagged()
    {
        var previous = new ScrollState(false, 100, 300, 0, 50, 500, 0);
        var next = previous.WithScrollPosition(new NewScrollPosition(ScrollTop: 20))
            .WithScrollDimensions(new NewScrollDimensions(Height: 60), true);

        var e = next.CreateScrollEvent(previous, true);

        Assert.True(e.InSmoothScrolling);
        Assert.True(e.ScrollTopChanged);
        Assert.True(e.HeightChanged);
        Assert.False(e.WidthChanged);
        Assert.False(e.ScrollWidthChanged);
        Assert.False(e.ScrollLeftChanged);
        Assert.False(e.ScrollHeightChanged);
        Assert.Equal(0, e.OldScrollTop);
        Assert.Equal(20, e.ScrollTop);
        Assert.Equal(50, e.OldHeight);
        Assert.Equal(60, e.Height);
    }

    [Fact]
    public void Equals_OnlyRawPositionDiffers_IsFalse()
    {
        var a = new ScrollState(false, 100, 100, 0, 100, 100, 200);
        var b = new ScrollState(false, 100, 100, 0, 100, 100, 300);

        Assert.Equal(a.ScrollTop, b.ScrollTop);
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(new ScrollState(false, 100, 100, 0, 100, 100, 200)));
    }
}
