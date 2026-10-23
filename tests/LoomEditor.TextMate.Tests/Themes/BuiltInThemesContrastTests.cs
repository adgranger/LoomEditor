using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests.
public class BuiltInThemesContrastTests
{
    private static EditorTheme Theme(string name) => EditorTheme.BuiltIn.Single(t => t.Name == name);

    [Theory]
    [InlineData("Dark High Contrast")]
    [InlineData("Light High Contrast")]
    public void HighContrastTheme_ForegroundOnBackground_MeetsAaa(string name)
    {
        EditorTheme theme = Theme(name);
        Assert.True(ContrastRatio.Between(theme.Foreground, theme.Background) >= ContrastRatio.AaaNormalText);
    }

    [Theory]
    [InlineData("Dark High Contrast")]
    [InlineData("Light High Contrast")]
    public void HighContrastTheme_LineNumbersOnBackground_MeetsAaa(string name)
    {
        EditorTheme theme = Theme(name);
        Assert.True(ContrastRatio.Between(theme.LineNumberForeground, theme.Background) >= ContrastRatio.AaaNormalText);
    }

    [Theory]
    [InlineData("Dark High Contrast")]
    [InlineData("Light High Contrast")]
    public void HighContrastTheme_CursorOnBackground_MeetsNonTextContrast(string name)
    {
        EditorTheme theme = Theme(name);
        Assert.True(ContrastRatio.Between(theme.CursorForeground, theme.Background) >= ContrastRatio.NonText);
    }

    [Theory]
    [InlineData("Dark High Contrast", ThemeType.HighContrastDark)]
    [InlineData("Light High Contrast", ThemeType.HighContrastLight)]
    public void HighContrastTheme_TypeIsHighContrast(string name, ThemeType type) => Assert.Equal(type, Theme(name).Type);

    [Theory]
    [InlineData("Dark Modern")]
    [InlineData("Light Modern")]
    public void ModernTheme_ForegroundOnBackground_MeetsAa(string name)
    {
        EditorTheme theme = Theme(name);
        Assert.True(ContrastRatio.Between(theme.Foreground, theme.Background) >= ContrastRatio.AaNormalText);
    }
}
