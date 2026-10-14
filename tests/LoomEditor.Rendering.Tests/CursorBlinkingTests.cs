namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the blinking logic (it is driven by DOM timers): all tests are new.
public class CursorBlinkingTests
{
    [Fact]
    public void Update_Unfocused_IsHiddenAndNeedsNoTimer()
    {
        var blinking = new CursorBlinking();

        blinking.Update(hasFocus: false, readOnly: false);

        Assert.False(blinking.IsVisible);
        Assert.False(blinking.NeedsTimer);
    }

    [Fact]
    public void Update_Focused_IsVisibleAndNeedsTheTimer()
    {
        var blinking = new CursorBlinking();

        blinking.Update(hasFocus: true, readOnly: false);

        Assert.True(blinking.IsVisible);
        Assert.True(blinking.NeedsTimer);
    }

    [Fact]
    public void Tick_WhenBlinking_TogglesVisibility()
    {
        var blinking = new CursorBlinking();
        blinking.Update(hasFocus: true, readOnly: false);

        blinking.Tick();
        Assert.False(blinking.IsVisible);

        blinking.Tick();
        Assert.True(blinking.IsVisible);
    }

    [Fact]
    public void Reset_AfterATick_ShowsTheCaretAgain()
    {
        var blinking = new CursorBlinking();
        blinking.Update(hasFocus: true, readOnly: false);
        blinking.Tick();

        blinking.Reset();

        Assert.True(blinking.IsVisible);
    }

    [Fact]
    public void Reset_Unfocused_StaysHidden()
    {
        var blinking = new CursorBlinking();

        blinking.Reset();

        Assert.False(blinking.IsVisible);
    }

    [Fact]
    public void Update_FocusedAndReadOnly_IsSolid()
    {
        var blinking = new CursorBlinking();

        blinking.Update(hasFocus: true, readOnly: true);
        blinking.Tick();

        Assert.True(blinking.IsVisible);
        Assert.False(blinking.NeedsTimer);
    }

    [Theory]
    [InlineData((int)TextEditorCursorBlinkingStyle.Smooth)]
    [InlineData((int)TextEditorCursorBlinkingStyle.Phase)]
    [InlineData((int)TextEditorCursorBlinkingStyle.Expand)]
    public void Tick_CssAnimatedStyles_BehaveLikeBlink(int style)
    {
        var blinking = new CursorBlinking { Style = (TextEditorCursorBlinkingStyle)style };
        blinking.Update(hasFocus: true, readOnly: false);

        blinking.Tick();

        Assert.True(blinking.NeedsTimer);
        Assert.False(blinking.IsVisible);
    }

    [Fact]
    public void Update_VisibilityChanges_RaisesChanged()
    {
        var blinking = new CursorBlinking();
        int raised = 0;
        blinking.Changed += (_, _) => raised++;

        blinking.Update(hasFocus: true, readOnly: false);
        blinking.Update(hasFocus: true, readOnly: false);
        blinking.Tick();

        Assert.Equal(2, raised);
    }
}
