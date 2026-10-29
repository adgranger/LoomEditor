using LoomEditor.View.Controller;

namespace LoomEditor.View.Tests.Controller;

public class MouseWheelZoomGestureTests
{
    private static double? Physical(double deltaY, bool ctrl = true, bool shift = false, bool alt = false, double level = 0) =>
        new MouseWheelZoomGesture().OnWheel(deltaY, true, ctrl, shift, alt, false, 1000, level);

    private static double? Touchpad(MouseWheelZoomGesture g, double deltaY, bool ctrl, long time, double level = 0) =>
        g.OnWheel(deltaY, false, ctrl, false, false, false, time, level);

    [Fact]
    public void PhysicalWheel_CtrlUp_ZoomsInByOne() => Assert.Equal(1, Physical(1));

    [Fact]
    public void PhysicalWheel_CtrlDown_ZoomsOutByOne() => Assert.Equal(-1, Physical(-1));

    [Fact]
    public void PhysicalWheel_NoCtrl_ReturnsNull() => Assert.Null(Physical(1, ctrl: false));

    [Fact]
    public void PhysicalWheel_CtrlShift_ReturnsNull() => Assert.Null(Physical(1, shift: true));

    [Fact]
    public void PhysicalWheel_CtrlAlt_ReturnsNull() => Assert.Null(Physical(1, alt: true));

    [Fact]
    public void Touchpad_Gesture_AccumulatesDividedByFive()
    {
        var g = new MouseWheelZoomGesture();
        Assert.Equal(0.4, Touchpad(g, 2, true, 1000)!.Value, 10);
        Assert.Equal(0.8, Touchpad(g, 2, true, 1010)!.Value, 10);
        Assert.Equal(1.0, Touchpad(g, 1, true, 1020)!.Value, 10);
    }

    [Fact]
    public void Touchpad_NewGestureAfter50Ms_RestartsFromCurrentLevel()
    {
        var g = new MouseWheelZoomGesture();
        Touchpad(g, 5, true, 1000);

        Assert.Equal(3.5, Touchpad(g, 2.5, true, 1100, level: 3)!.Value, 10);
    }

    [Fact]
    public void Touchpad_InertiaWithoutCtrl_DoesNotZoom()
    {
        var g = new MouseWheelZoomGesture();
        Assert.Null(Touchpad(g, 1, false, 1000));
        Assert.Null(Touchpad(g, 1, true, 1010));
        Assert.NotNull(Touchpad(g, 1, true, 1100));
    }

    [Fact]
    public void Touchpad_CtrlPressedMidGesture_DoesNotZoom()
    {
        var g = new MouseWheelZoomGesture();
        Touchpad(g, 1, true, 1000);
        Assert.Equal(0.4, Touchpad(g, 1, true, 1010)!.Value, 10);
        var h = new MouseWheelZoomGesture();
        Touchpad(h, 1, false, 1000);
        Assert.Null(Touchpad(h, 1, true, 1020));
    }
}
