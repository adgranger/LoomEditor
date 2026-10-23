using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco generates CSS rules in a theming participant: all tests are new.
public class GuideColorsTests
{
    private static EditorTheme Theme(string colors) => EditorTheme.Load("{ \"colors\": { " + colors + " } }", "vs-dark", "Test");

    // A theme without any color: every default of the table applies (EditorTheme.Dark is Dark Modern, which sets its own).
    private static readonly EditorTheme Plain = Theme("");

    [Fact]
    public void IndentGuideColor_DarkTheme_UsesTheDefaults()
    {
        // Only level 1 has a visible default (editorWhitespace.foreground #e3e4e229); levels 2–6 are transparent and dropped.
        string expected = ThemeColor.Parse("#e3e4e229").ToString();

        foreach (int level in new[] { 0, 5 })
        {
            Assert.Equal(expected, GuideColors.GetIndentGuideColor(Plain, level, active: false)?.ToString());
            Assert.Equal(expected, GuideColors.GetIndentGuideColor(Plain, level, active: true)?.ToString());
        }
    }

    [Fact]
    public void BracketGuideColor_NoGuideColor_FallsBackToTheBracketColorAt30Percent()
    {
        // Dark defaults: bracket 1 is #FFD700; alpha 255 * 0.3 = 76.5 rounds to 0x4D, like Monaco's transparent.
        Assert.Equal("#FFD7004D", GuideColors.GetBracketGuideColor(Plain, 0, active: false)?.ToString());
        Assert.Equal("#FFD700FF", GuideColors.GetBracketGuideColor(Plain, 0, active: true)?.ToString());
        Assert.Equal("#DA70D64D", GuideColors.GetBracketGuideColor(Plain, 1, active: false)?.ToString());
    }

    [Fact]
    public void BracketGuideColor_TransparentLevelsAreSkipped()
    {
        // Brackets 4–6 are transparent by default, so only three levels remain and level 3 wraps to level 0.
        Assert.Equal(
            GuideColors.GetBracketGuideColor(Plain, 0, active: false),
            GuideColors.GetBracketGuideColor(Plain, 3, active: false));
        Assert.NotEqual(
            GuideColors.GetBracketGuideColor(Plain, 0, active: false),
            GuideColors.GetBracketGuideColor(Plain, 2, active: false));
    }

    [Fact]
    public void BracketGuideColor_ThemeGuideColor_WinsOverTheBracketColor()
    {
        var theme = Theme("\"editorBracketPairGuide.background1\": \"#112233\"");

        Assert.Equal("#112233FF", GuideColors.GetBracketGuideColor(theme, 0, active: false)?.ToString());
    }

    [Fact]
    public void IndentGuideColor_LevelCyclesOverTheEffectiveColors()
    {
        var theme = Theme("\"editorIndentGuide.background2\": \"#220000\", \"editorIndentGuide.activeBackground2\": \"#2200FF\"");

        string?[] colors = [.. Enumerable.Range(0, 4).Select(l => GuideColors.GetIndentGuideColor(theme, l, active: false)?.ToString())];

        Assert.Equal(colors[0], colors[2]);
        Assert.Equal(colors[1], colors[3]);
        Assert.NotEqual(colors[0], colors[1]);
        Assert.Equal("#2200FFFF", GuideColors.GetIndentGuideColor(theme, 3, active: true)?.ToString());
    }

    [Fact]
    public void IndentGuideColor_AllTransparent_NoColor()
    {
        var theme = Theme("\"editorIndentGuide.background1\": \"#00000000\"");

        Assert.Null(GuideColors.GetIndentGuideColor(theme, 0, active: false));
    }
}
