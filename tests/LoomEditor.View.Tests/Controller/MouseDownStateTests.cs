using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Controller;

// Monaco has no test for MouseDownState: all tests are new.
public class MouseDownStateTests
{
    [Fact]
    public void TrySetCount_TwoClicksOnTheSamePositionWithin400Ms_CountsTwo()
    {
        var clock = new FakeTimeProvider();
        var state = new MouseDownState(clock);

        state.TrySetCount(1, new Position(1, 3));
        clock.Advance(300);
        state.TrySetCount(2, new Position(1, 3));

        Assert.Equal(2, state.Count);
    }

    [Fact]
    public void TrySetCount_SecondClickAfter500Ms_CountsOne()
    {
        var clock = new FakeTimeProvider();
        var state = new MouseDownState(clock);

        state.TrySetCount(1, new Position(1, 3));
        clock.Advance(500);
        state.TrySetCount(2, new Position(1, 3));

        Assert.Equal(1, state.Count);
    }

    [Fact]
    public void TrySetCount_SecondClickAtAnotherPosition_CountsOne()
    {
        var clock = new FakeTimeProvider();
        var state = new MouseDownState(clock);

        state.TrySetCount(1, new Position(1, 3));
        clock.Advance(100);
        state.TrySetCount(2, new Position(1, 4));

        Assert.Equal(1, state.Count);
    }

    [Fact]
    public void TrySetCount_ReportedCountJumps_NeverJumpsByMoreThanOne()
    {
        var clock = new FakeTimeProvider();
        var state = new MouseDownState(clock);

        state.TrySetCount(1, new Position(1, 3));
        clock.Advance(100);
        state.TrySetCount(3, new Position(1, 3));

        Assert.Equal(2, state.Count);
    }

    [Fact]
    public void TrySetCount_ManyClicksOnTheSamePosition_KeepsCounting()
    {
        var clock = new FakeTimeProvider();
        var state = new MouseDownState(clock);

        for (int click = 1; click <= 4; click++)
        {
            state.TrySetCount(click, new Position(1, 3));
            clock.Advance(100);
        }

        Assert.Equal(4, state.Count);
    }

    [Fact]
    public void SetModifiersAndButtons_AreRemembered()
    {
        var state = new MouseDownState(new FakeTimeProvider());
        var e = new EditorMouseEvent(0, 0, true, false, true, false, true, false, 1, 0);

        state.SetModifiers(e);
        state.SetStartButtons(e);
        state.SetStartedOnLineNumbers(true);

        Assert.True(state.AltKey);
        Assert.False(state.CtrlKey);
        Assert.True(state.ShiftKey);
        Assert.False(state.MetaKey);
        Assert.True(state.LeftButton);
        Assert.False(state.MiddleButton);
        Assert.True(state.StartedOnLineNumbers);
    }
}
