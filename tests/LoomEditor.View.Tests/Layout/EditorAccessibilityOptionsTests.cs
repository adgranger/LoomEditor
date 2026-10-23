using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public sealed class EditorAccessibilityOptionsTests
{
    [Theory]
    [InlineData((int)AccessibilitySupport.Unknown)]
    [InlineData((int)AccessibilitySupport.Disabled)]
    [InlineData((int)AccessibilitySupport.Enabled)]
    public void EffectiveSupport_SettingUnknown_UsesDetected(int detected)
    {
        var options = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Unknown, Detected = (AccessibilitySupport)detected };
        Assert.Equal((AccessibilitySupport)detected, options.Effective);
    }

    [Theory]
    [InlineData((int)AccessibilitySupport.Unknown)]
    [InlineData((int)AccessibilitySupport.Disabled)]
    [InlineData((int)AccessibilitySupport.Enabled)]
    public void EffectiveSupport_SettingDisabled_IgnoresDetected(int detected)
    {
        var options = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Disabled, Detected = (AccessibilitySupport)detected };
        Assert.Equal(AccessibilitySupport.Disabled, options.Effective);
    }

    [Theory]
    [InlineData((int)AccessibilitySupport.Unknown)]
    [InlineData((int)AccessibilitySupport.Disabled)]
    [InlineData((int)AccessibilitySupport.Enabled)]
    public void EffectiveSupport_SettingEnabled_IgnoresDetected(int detected)
    {
        var options = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Enabled, Detected = (AccessibilitySupport)detected };
        Assert.Equal(AccessibilitySupport.Enabled, options.Effective);
    }

    [Theory]
    [InlineData((int)AccessibilitySupport.Unknown, AccessibilitySupport.Unknown, false)]
    [InlineData((int)AccessibilitySupport.Unknown, AccessibilitySupport.Disabled, false)]
    [InlineData((int)AccessibilitySupport.Unknown, AccessibilitySupport.Enabled, true)]
    [InlineData((int)AccessibilitySupport.Disabled, AccessibilitySupport.Unknown, false)]
    [InlineData((int)AccessibilitySupport.Disabled, AccessibilitySupport.Disabled, false)]
    [InlineData((int)AccessibilitySupport.Disabled, AccessibilitySupport.Enabled, false)]
    [InlineData((int)AccessibilitySupport.Enabled, AccessibilitySupport.Unknown, true)]
    [InlineData((int)AccessibilitySupport.Enabled, AccessibilitySupport.Disabled, true)]
    [InlineData((int)AccessibilitySupport.Enabled, AccessibilitySupport.Enabled, true)]
    public void IsEnabled_OnlyWhenEffectiveIsEnabled(int setting, int detected, bool expected)
    {
        var options = new EditorAccessibilityOptions { Setting = (AccessibilitySupport)setting, Detected = (AccessibilitySupport)detected };
        Assert.Equal(expected, options.IsEnabled);
    }

    [Fact]
    public void GetEffectiveWrapping_Disabled_ReturnsTheWrappingUnchanged()
    {
        var layout = new EditorLayoutOptions { Wrapping = new EditorWrappingOptions { WrappingIndent = WrappingIndent.Indent } };
        Assert.Same(layout.Wrapping, layout.GetEffectiveWrapping());
    }

    [Fact]
    public void GetEffectiveWrapping_Enabled_ForcesIndentNone()
    {
        var layout = new EditorLayoutOptions
        {
            Wrapping = new EditorWrappingOptions { WrappingIndent = WrappingIndent.Indent },
            Accessibility = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Enabled },
        };
        Assert.Equal(WrappingIndent.None, layout.GetEffectiveWrapping().WrappingIndent);
    }

    [Fact]
    public void GetEffectiveWrapping_Enabled_KeepsEveryOtherSetting()
    {
        var wrapping = new EditorWrappingOptions
        {
            WordWrap = WordWrap.WordWrapColumn,
            WordWrapColumn = 42,
            WordWrapIndicator = true,
            WrappingIndent = WrappingIndent.DeepIndent,
            WordBreak = WordBreak.KeepAll,
            WrapOnEscapedLineFeeds = true,
            UseTwoCellFullwidthCharacters = true,
        };
        var layout = new EditorLayoutOptions
        {
            Wrapping = wrapping,
            Accessibility = new EditorAccessibilityOptions { Setting = AccessibilitySupport.Enabled },
        };
        Assert.Equal(wrapping with { WrappingIndent = WrappingIndent.None }, layout.GetEffectiveWrapping());
    }
}
