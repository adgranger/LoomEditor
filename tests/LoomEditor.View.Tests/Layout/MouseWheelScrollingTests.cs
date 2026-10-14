using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for the wheel arithmetic of _onMouseWheel: all tests are new.
public class MouseWheelScrollingTests
{
    private static readonly MouseWheelOptions Options = new();

    private static NewScrollPosition Compute(MouseWheelInput input, double scrollLeft = 0, double scrollTop = 0)
    {
        NewScrollPosition? result = MouseWheelScrolling.ComputeScrollPosition(
            input, new ScrollPosition(scrollLeft, scrollTop), Options);
        Assert.NotNull(result);
        return result.Value;
    }

    [Fact]
    public void ComputeScrollPosition_OneNotchDown_ScrollsFiftyPixels()
    {
        NewScrollPosition result = Compute(new MouseWheelInput(0, -1, false, false));

        Assert.Equal(50, result.ScrollTop);
        Assert.Null(result.ScrollLeft);
    }

    [Fact]
    public void ComputeScrollPosition_AltHeld_ScrollsFast()
    {
        NewScrollPosition result = Compute(new MouseWheelInput(0, -1, false, true));

        Assert.Equal(250, result.ScrollTop);
    }

    [Fact]
    public void ComputeScrollPosition_ShiftHeld_ScrollsHorizontally()
    {
        NewScrollPosition result = Compute(new MouseWheelInput(0, -1, true, false));

        Assert.Equal(50, result.ScrollLeft);
        Assert.Null(result.ScrollTop);
    }

    [Fact]
    public void ComputeScrollPosition_TinyDelta_StillMovesOnePixel()
    {
        NewScrollPosition result = Compute(new MouseWheelInput(0, 0.003, false, false), scrollTop: 100);

        Assert.Equal(99, result.ScrollTop);
    }

    [Fact]
    public void ComputeScrollPosition_PredominantAxis_ScrollsVerticallyOnly()
    {
        NewScrollPosition result = Compute(new MouseWheelInput(0.2, -1, false, false));

        Assert.Equal(50, result.ScrollTop);
        Assert.Null(result.ScrollLeft);
    }

    [Fact]
    public void ComputeScrollPosition_PredominantAxisOff_ScrollsBothAxes()
    {
        var options = new MouseWheelOptions(ScrollPredominantAxis: false);

        NewScrollPosition? result = MouseWheelScrolling.ComputeScrollPosition(
            new MouseWheelInput(-0.5, -1, false, false), new ScrollPosition(0, 0), options);

        Assert.Equal(25, result!.Value.ScrollLeft);
        Assert.Equal(50, result.Value.ScrollTop);
    }

    [Fact]
    public void ComputeScrollPosition_Sensitivity_MultipliesDelta()
    {
        var options = new MouseWheelOptions(MouseWheelScrollSensitivity: 2);

        NewScrollPosition? result = MouseWheelScrolling.ComputeScrollPosition(
            new MouseWheelInput(0, -1, false, false), new ScrollPosition(0, 10), options);

        Assert.Equal(110, result!.Value.ScrollTop);
    }

    [Fact]
    public void ComputeScrollPosition_NoDelta_ReturnsNull()
    {
        Assert.Null(MouseWheelScrolling.ComputeScrollPosition(
            new MouseWheelInput(0, 0, false, false), new ScrollPosition(0, 0), Options));
    }

    [Fact]
    public void ShouldScrollSmoothly_PhysicalWheel_IsTrueAndTouchpadIsFalse()
    {
        var wheel = new MouseWheelClassifier();
        wheel.Accept(1000, 0, -2);
        wheel.Accept(1050, 0, -2);
        var touchpad = new MouseWheelClassifier();
        touchpad.Accept(1000, 0.3, -0.7);

        Assert.True(MouseWheelScrolling.ShouldScrollSmoothly(wheel));
        Assert.False(MouseWheelScrolling.ShouldScrollSmoothly(touchpad));
        Assert.False(MouseWheelScrolling.ShouldScrollSmoothly(new MouseWheelClassifier()));
    }
}
