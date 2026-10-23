using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorLayoutInfoComputerAccessibilityTests
{
    private static EditorLayoutInfo Compute(
        WordWrap wordWrap,
        int wordWrapColumn = 80,
        AccessibilitySupport setting = AccessibilitySupport.Disabled,
        AccessibilitySupport detected = AccessibilitySupport.Unknown,
        bool dominated = false)
    {
        var options = new EditorLayoutOptions
        {
            GlyphMargin = false,
            LineNumbers = false,
            LineDecorationsWidth = 10,
            Folding = false,
            Minimap = new() { Enabled = false },
            VerticalScrollbarSize = 0,
            HorizontalScrollbarSize = 0,
            Wrapping = new EditorWrappingOptions { WordWrap = wordWrap, WordWrapColumn = wordWrapColumn },
            Accessibility = new EditorAccessibilityOptions { Setting = setting, Detected = detected },
        };
        var env = new EditorLayoutInfoComputerEnv(500, 300, 16, 1, 10, 10, 1, 1, IsDominatedByLongLines: dominated);
        return EditorLayoutInfoComputer.ComputeLayout(options, env);
    }

    private static EditorLayoutInfo On => Compute(WordWrap.On);

    [Fact]
    public void ForcedWrapping_EnabledAndDominated_WrapsAtTheViewportEvenWhenWordWrapIsOff()
    {
        EditorLayoutInfo info = Compute(WordWrap.Off, setting: AccessibilitySupport.Enabled, dominated: true);
        Assert.True(info.IsViewportWrapping);
        Assert.Equal(On.WrappingColumn, info.WrappingColumn);
    }

    [Fact]
    public void ForcedWrapping_EnabledButNotDominated_DoesNotWrap()
    {
        EditorLayoutInfo info = Compute(WordWrap.Off, setting: AccessibilitySupport.Enabled);
        Assert.False(info.IsViewportWrapping);
        Assert.Equal(-1, info.WrappingColumn);
    }

    [Fact]
    public void ForcedWrapping_DisabledAndDominated_DoesNotWrap()
    {
        EditorLayoutInfo info = Compute(WordWrap.Off, setting: AccessibilitySupport.Disabled, dominated: true);
        Assert.False(info.IsViewportWrapping);
        Assert.Equal(-1, info.WrappingColumn);
    }

    [Fact]
    public void ForcedWrapping_DetectedEnabledWithAutoSetting_Wraps()
    {
        EditorLayoutInfo info = Compute(
            WordWrap.Off, setting: AccessibilitySupport.Unknown, detected: AccessibilitySupport.Enabled, dominated: true);
        Assert.True(info.IsViewportWrapping);
    }

    [Fact]
    public void ForcedWrapping_BeatsWordWrapColumn()
    {
        EditorLayoutInfo info = Compute(
            WordWrap.WordWrapColumn, 50, AccessibilitySupport.Enabled, dominated: true);
        Assert.True(info.IsViewportWrapping);
        Assert.Equal(On.WrappingColumn, info.WrappingColumn);
    }

    [Fact]
    public void ForcedWrapping_BoundedStillTakesTheMinimum()
    {
        EditorLayoutInfo info = Compute(WordWrap.Bounded, 5, AccessibilitySupport.Enabled, dominated: true);
        Assert.Equal(5, info.WrappingColumn);
    }
}
