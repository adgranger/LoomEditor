using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests.
public class SystemHighContrastThemeFactoryTests
{
    // Samples, not a claim about the exact values of Microsoft's schemes.
    private static SystemHighContrastColors Palette(string window, string windowText, string highlight, string highlightText,
        string grayText, string hotlight, string buttonFace, string buttonText) =>
        new(ThemeColor.Parse(window), ThemeColor.Parse(windowText), ThemeColor.Parse(highlight), ThemeColor.Parse(highlightText),
            ThemeColor.Parse(grayText), ThemeColor.Parse(hotlight), ThemeColor.Parse(buttonFace), ThemeColor.Parse(buttonText));

    private static readonly SystemHighContrastColors Black =
        Palette("#000000", "#FFFFFF", "#1AEBFF", "#000000", "#3FF23F", "#FFFF00", "#000000", "#FFFFFF");

    private static readonly SystemHighContrastColors White =
        Palette("#FFFFFF", "#000000", "#37006E", "#FFFFFF", "#600000", "#00009F", "#FFFFFF", "#000000");

    private static readonly SystemHighContrastColors Custom =
        Palette("#000080", "#FFFF00", "#FFFFFF", "#000080", "#C0C0C0", "#00FFFF", "#000080", "#FFFF00");

    public static TheoryData<string> PaletteNames => ["black", "white", "custom"];

    private static SystemHighContrastColors ByName(string name) => name switch
    {
        "black" => Black,
        "white" => White,
        _ => Custom,
    };

    private static TokenColorRule RuleFor(EditorTheme theme, string scope) =>
        theme.TokenColors.Single(r => r.Scopes.Contains(scope));

    [Theory]
    [InlineData("black")]
    [InlineData("custom")]
    public void Create_DarkPalette_IsHighContrastDark(string name) =>
        Assert.Equal(ThemeType.HighContrastDark, SystemHighContrastThemeFactory.Create(ByName(name)).Type);

    [Fact]
    public void Create_LightPalette_IsHighContrastLight() =>
        Assert.Equal(ThemeType.HighContrastLight, SystemHighContrastThemeFactory.Create(White).Type);

    [Fact]
    public void Create_UsesTheGivenName() =>
        Assert.Equal("My \"scheme\" \\ 1", SystemHighContrastThemeFactory.Create(Black, "My \"scheme\" \\ 1").Name);

    [Fact]
    public void Create_DefaultName_IsWindowsHighContrast() =>
        Assert.Equal("Windows High Contrast", SystemHighContrastThemeFactory.Create(Black).Name);

    [Theory]
    [MemberData(nameof(PaletteNames))]
    public void Create_MapsTheEditorColors(string name)
    {
        SystemHighContrastColors c = ByName(name);
        EditorTheme theme = SystemHighContrastThemeFactory.Create(c);

        Assert.Equal(c.Window, theme.Background);
        Assert.Equal(c.WindowText, theme.Foreground);
        Assert.Equal(c.Highlight, theme.SelectionBackground);
        Assert.Equal(c.HighlightText, theme.SelectionForeground);
        Assert.Equal(c.WindowText, theme.CursorForeground);
        Assert.Equal(c.WindowText, theme.LineNumberForeground);
        Assert.Equal(c.Hotlight, theme.LineHighlightBorder);
    }

    [Theory]
    [MemberData(nameof(PaletteNames))]
    public void Create_Palette_MeetsWcagAaa_ForTextAndSelection(string name)
    {
        EditorTheme theme = SystemHighContrastThemeFactory.Create(ByName(name));

        Assert.True(ContrastRatio.Between(theme.Foreground, theme.Background) >= 7);
        Assert.True(ContrastRatio.Between(theme.SelectionForeground!.Value, theme.SelectionBackground) >= 4.5);
    }

    [Fact]
    public void Create_CommentTokens_UseGrayText_Italic()
    {
        TokenColorRule rule = RuleFor(SystemHighContrastThemeFactory.Create(Black), "comment");

        Assert.Equal(Black.GrayText, rule.Foreground);
        Assert.Equal("italic", rule.FontStyle);
    }

    [Fact]
    public void Create_KeywordTokens_AreBold() =>
        Assert.Equal("bold", RuleFor(SystemHighContrastThemeFactory.Create(Black), "keyword").FontStyle);

    [Fact]
    public void Create_LinkTokens_UseHotlightUnderlined()
    {
        TokenColorRule rule = RuleFor(SystemHighContrastThemeFactory.Create(White), "markup.underline.link");

        Assert.Equal(White.Hotlight, rule.Foreground);
        Assert.Equal("underline", rule.FontStyle);
    }

    [Fact]
    public void Create_DefaultRule_HasWindowTextOnWindow()
    {
        TokenColorRule rule = SystemHighContrastThemeFactory.Create(Custom).TokenColors[0];

        Assert.Equal(Custom.WindowText, rule.Foreground);
        Assert.Equal(Custom.Window, rule.Background);
    }

    [Fact]
    public void Create_IgnoresTheAlphaOfTheInput()
    {
        static ThemeColor Translucent(ThemeColor c) => c with { A = 0x20 };
        SystemHighContrastColors t = new(Translucent(Black.Window), Translucent(Black.WindowText), Translucent(Black.Highlight),
            Translucent(Black.HighlightText), Translucent(Black.GrayText), Translucent(Black.Hotlight),
            Translucent(Black.ButtonFace), Translucent(Black.ButtonText));

        EditorTheme a = SystemHighContrastThemeFactory.Create(Black);
        EditorTheme b = SystemHighContrastThemeFactory.Create(t);

        Assert.Equal(a.Type, b.Type);
        Assert.Equal(a.Background, b.Background);
        Assert.Equal(a.Foreground, b.Foreground);
        Assert.Equal(a.SelectionBackground, b.SelectionBackground);
        Assert.Equal(a.SelectionForeground, b.SelectionForeground);
        Assert.Equal(a.LineHighlightBorder, b.LineHighlightBorder);
    }

    [Fact]
    public void Create_WidgetColors_AreResolved()
    {
        EditorTheme theme = SystemHighContrastThemeFactory.Create(Custom);

        Assert.Equal(Custom.ButtonFace, theme.GetColor("editorWidget.background"));
        Assert.Equal(Custom.Hotlight, theme.GetColor("focusBorder"));
        Assert.Equal(Custom.WindowText, theme.GetColor("input.border"));
    }
}
