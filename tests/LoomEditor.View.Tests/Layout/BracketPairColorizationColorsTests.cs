using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco generates CSS rules in a theming participant: all tests are new.
public class BracketPairColorizationColorsTests
{
    private static EditorTheme Theme(string colors) => EditorTheme.Load("{ \"colors\": { " + colors + " } }", "vs-dark", "Test");

    private static string?[] Levels(EditorTheme theme, int count) =>
        [.. Enumerable.Range(0, count).Select(level => BracketPairColorizationColors.GetLevelColor(theme, level)?.ToString())];

    [Fact]
    public void GetLevelColor_DefaultDarkColors_CycleOverThreeColors()
    {
        string?[] levels = Levels(EditorTheme.Dark, 6);

        Assert.Equal(ThemeColor.Parse("#FFD700").ToString(), levels[0]);
        Assert.Equal(ThemeColor.Parse("#DA70D6").ToString(), levels[1]);
        Assert.Equal(ThemeColor.Parse("#179FFF").ToString(), levels[2]);
        Assert.Equal(levels[..3], levels[3..]);
    }

    [Fact]
    public void GetLevelColor_ThemeSetsSixColors_CycleOverSix()
    {
        var theme = Theme(
            "\"editorBracketHighlight.foreground1\": \"#110000\", \"editorBracketHighlight.foreground2\": \"#220000\", "
            + "\"editorBracketHighlight.foreground3\": \"#330000\", \"editorBracketHighlight.foreground4\": \"#440000\", "
            + "\"editorBracketHighlight.foreground5\": \"#550000\", \"editorBracketHighlight.foreground6\": \"#660000\"");

        string?[] levels = Levels(theme, 12);

        Assert.Equal(6, levels.Distinct().Count());
        Assert.Equal(levels[..6], levels[6..]);
        Assert.Equal(ThemeColor.Parse("#440000").ToString(), levels[3]);
    }

    [Fact]
    public void GetLevelColor_AllTransparent_NoColor()
    {
        var theme = Theme(
            "\"editorBracketHighlight.foreground1\": \"#00000000\", \"editorBracketHighlight.foreground2\": \"#00000000\", "
            + "\"editorBracketHighlight.foreground3\": \"#00000000\", \"editorBracketHighlight.foreground4\": \"#00000000\", "
            + "\"editorBracketHighlight.foreground5\": \"#00000000\", \"editorBracketHighlight.foreground6\": \"#00000000\"");

        Assert.Null(BracketPairColorizationColors.GetLevelColor(theme, 0));
    }

    [Fact]
    public void GetUnexpectedBracketColor_ReturnsTheThemeColor()
    {
        var theme = Theme("\"editorBracketHighlight.unexpectedBracket.foreground\": \"#123456\"");

        Assert.Equal(ThemeColor.Parse("#123456"), BracketPairColorizationColors.GetUnexpectedBracketColor(theme));
    }

    [Fact]
    public void DecorationStyles_BracketClasses_HaveAComputedForeground()
    {
        Assert.NotNull(DecorationStyles.Get("bracket-highlighting-0")?.ForegroundResolver);
        Assert.NotNull(DecorationStyles.Get("bracket-highlighting-29")?.ForegroundResolver);
        Assert.Null(DecorationStyles.Get("bracket-highlighting-30"));
        Assert.NotNull(DecorationStyles.Get("unexpected-closing-bracket")?.ForegroundResolver);
    }
}
