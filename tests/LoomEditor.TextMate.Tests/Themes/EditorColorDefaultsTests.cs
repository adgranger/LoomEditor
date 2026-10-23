using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests: the expected values are the ones of the pinned VS Code files.
public class EditorColorDefaultsTests
{
    private static ThemeColor? NoThemeColors(string id) => null;

    private static string? Default(string id, ThemeType type) => EditorColorDefaults.Resolve(id, type, NoThemeColors) is { } c ? c.ToString() : null;

    [Theory]
    [InlineData("editor.background", "#1E1E1EFF")]
    [InlineData("editor.foreground", "#BBBBBBFF")]
    [InlineData("editor.selectionBackground", "#264F78FF")]
    [InlineData("editor.compositionBorder", "#FFFFFFFF")]
    [InlineData("editor.lineHighlightBackground", null)]
    [InlineData("editor.lineHighlightBorder", "#282828FF")]
    [InlineData("editorCursor.foreground", "#AEAFADFF")]
    [InlineData("editorLineNumber.foreground", "#858585FF")]
    [InlineData("editorActiveLineNumber.foreground", "#C6C6C6FF")]
    [InlineData("editorLineNumber.activeForeground", "#C6C6C6FF")] // a reference to the deprecated id
    [InlineData("foreground", "#CCCCCCFF")]
    [InlineData("focusBorder", "#007FD4FF")]
    [InlineData("contrastBorder", null)]
    [InlineData("contrastActiveBorder", null)]
    public void Defaults_DarkTheme_MatchVsCode(string id, string? expected)
    {
        Assert.Equal(expected, Default(id, ThemeType.Dark));
    }

    [Theory]
    [InlineData(ThemeType.Light, "editor.background", "#FFFFFFFF")]
    [InlineData(ThemeType.Light, "editor.foreground", "#333333FF")]
    [InlineData(ThemeType.Light, "editorLineNumber.activeForeground", "#0B216FFF")]
    [InlineData(ThemeType.HighContrastDark, "editor.background", "#000000FF")]
    [InlineData(ThemeType.HighContrastDark, "editor.lineHighlightBorder", "#F38518FF")]
    [InlineData(ThemeType.HighContrastDark, "editorLineNumber.activeForeground", "#F38518FF")] // contrastActiveBorder -> focusBorder
    [InlineData(ThemeType.HighContrastLight, "editor.foreground", "#292929FF")] // a reference to `foreground`
    [InlineData(ThemeType.HighContrastLight, "editor.lineHighlightBorder", "#0F4A85FF")] // a reference to `contrastBorder`
    public void Defaults_OtherThemeTypes_MatchVsCode(ThemeType type, string id, string expected)
    {
        Assert.Equal(expected, Default(id, type));
    }

    [Theory]
    [InlineData(ThemeType.Dark, "editorWhitespace.foreground", "#E3E4E229")]
    [InlineData(ThemeType.Light, "editorIndentGuide.background1", "#33333333")] // deprecated id -> whitespace color
    [InlineData(ThemeType.HighContrastLight, "editorIndentGuide.activeBackground1", "#CCCCCCFF")]
    [InlineData(ThemeType.Dark, "editorIndentGuide.background2", "#00000000")]
    [InlineData(ThemeType.Dark, "editorBracketPairGuide.activeBackground6", "#00000000")]
    public void Defaults_GuideColors_MatchVsCode(ThemeType type, string id, string expected)
    {
        Assert.Equal(expected, Default(id, type));
    }

    [Fact]
    public void WithAlphaFactor_MultipliesAlpha()
    {
        Assert.Equal(ThemeColor.Parse("#FFD7004D"), ThemeColor.Parse("#FFD700").WithAlphaFactor(0.3));
    }

    [Fact]
    public void Defaults_ReferenceToAnotherColor_UsesTheThemeValueFirst()
    {
        // editorLineNumber.activeForeground is `editorActiveLineNumber.foreground` unless the theme sets it itself.
        ThemeColor? Theme(string id) => id == "editorActiveLineNumber.foreground" ? ThemeColor.Parse("#123456") : null;

        Assert.Equal(ThemeColor.Parse("#123456"), EditorColorDefaults.Resolve("editorLineNumber.activeForeground", ThemeType.Dark, Theme));
        Assert.Equal(ThemeColor.Parse("#654321"), EditorColorDefaults.Resolve("editorLineNumber.activeForeground", ThemeType.Dark, id => id == "editorLineNumber.activeForeground" ? ThemeColor.Parse("#654321") : Theme(id)));
    }

    [Fact]
    public void Resolve_Transparent_MultipliesAlpha()
    {
        // editor.foldBackground = transparent(editor.selectionBackground (#264F78 in dark), 0.3): 0.3 * 255 rounds to 0x4D
        Assert.Equal("#264F784D", Default("editor.foldBackground", ThemeType.Dark));
    }

    [Fact]
    public void Resolve_FindColors_DarkAndLightDefaults()
    {
        Assert.Equal("#515C6AFF", Default("editor.findMatchBackground", ThemeType.Dark));
        Assert.Equal("#A8AC94FF", Default("editor.findMatchBackground", ThemeType.Light));
        Assert.Equal("#EA5C0055", Default("editor.findMatchHighlightBackground", ThemeType.Dark));
        Assert.Equal("#EA5C0055", Default("editor.findMatchHighlightBackground", ThemeType.Light));
        Assert.Equal("#3A3D4166", Default("editor.findRangeHighlightBackground", ThemeType.Dark));
        Assert.Equal("#B4B4B44D", Default("editor.findRangeHighlightBackground", ThemeType.Light));
        Assert.Equal("#FFFFFF0B", Default("editor.rangeHighlightBackground", ThemeType.Dark));
        Assert.Equal("#FDFF0033", Default("editor.rangeHighlightBackground", ThemeType.Light));
        Assert.Null(Default("editor.findMatchBorder", ThemeType.Dark));
        Assert.Null(Default("editor.rangeHighlightBorder", ThemeType.Light));
    }

    [Fact]
    public void Resolve_FindWidgetColors_DarkAndLightDefaults()
    {
        Assert.Equal("#252526FF", Default("editorWidget.background", ThemeType.Dark));
        Assert.Equal("#F3F3F3FF", Default("editorWidget.background", ThemeType.Light));
        Assert.Equal("#CCCCCCFF", Default("editorWidget.foreground", ThemeType.Dark));
        Assert.Equal("#CCCCCC33", Default("editorWidget.border", ThemeType.Dark));
        Assert.Equal("#61616133", Default("editorWidget.border", ThemeType.Light));
        Assert.Equal("#3C3C3CFF", Default("input.background", ThemeType.Dark));
        Assert.Equal("#FFFFFFFF", Default("input.background", ThemeType.Light));
        Assert.Null(Default("input.border", ThemeType.Dark));
        Assert.Equal("#6FC3DFFF", Default("input.border", ThemeType.HighContrastDark));
        Assert.Equal("#007ACCFF", Default("inputOption.activeBorder", ThemeType.Light));
        Assert.Equal("#007FD466", Default("inputOption.activeBackground", ThemeType.Dark));
        Assert.Equal("#0090F133", Default("inputOption.activeBackground", ThemeType.Light));
        Assert.Equal("#FFFFFFFF", Default("inputOption.activeForeground", ThemeType.Dark));
        Assert.Equal("#F48771FF", Default("errorForeground", ThemeType.Dark));
        Assert.Equal("#B5200DFF", Default("errorForeground", ThemeType.HighContrastLight));
    }

    [Fact]
    public void Resolve_Transparent_UsesTheThemesValueOfTheOperand()
    {
        ThemeColor? Theme(string id) => id == "editor.selectionBackground" ? ThemeColor.Parse("#FF0000") : null;

        Assert.Equal("#FF00004D", EditorColorDefaults.Resolve("editor.foldBackground", ThemeType.Dark, Theme)?.ToString());
    }

    [Fact]
    public void Resolve_LessProminent_DarkTheme_IsTheDarkerColorMadeTransparent()
    {
        // The selection is lighter than the background: Monaco takes the darker color, then makes it transparent.
        Color selection = Color.FromThemeColor(ThemeColor.Parse("#264F78"));
        Color background = Color.FromThemeColor(ThemeColor.Parse("#1E1E1E"));
        string expected = Color.GetDarkerColor(selection, background, 0.3).Transparent(0.6).ToThemeColor().ToString();

        Assert.Equal(expected, Default("editor.selectionHighlightBackground", ThemeType.Dark));
    }

    [Fact]
    public void Resolve_LessProminent_LightTheme_IsTheLighterColorMadeTransparent()
    {
        // The selection is darker than the background: Monaco takes the lighter color, then makes it transparent.
        Color selection = Color.FromThemeColor(ThemeColor.Parse("#ADD6FF"));
        Color background = Color.FromThemeColor(ThemeColor.Parse("#ffffff"));
        string expected = Color.GetLighterColor(selection, background, 0.3).Transparent(0.6).ToThemeColor().ToString();

        Assert.Equal(expected, Default("editor.selectionHighlightBackground", ThemeType.Light));
    }

    [Theory]
    [InlineData("editor.findMatchForeground")]
    [InlineData("editorBracketMatch.foreground")]
    [InlineData("editor.selectionHighlightBackground")] // null for the high contrast themes
    public void Resolve_NullDefault_IsNull(string id)
    {
        Assert.Null(Default(id, id == "editor.selectionHighlightBackground" ? ThemeType.HighContrastDark : ThemeType.Dark));
    }

    [Fact]
    public void GetColor_DarkModern_ReturnsTheThemeValueElseTheDefault()
    {
        // Dark Modern sets editor.findMatchBackground (#9E6A03) but not editor.findMatchHighlightBackground.
        Assert.Equal(ThemeColor.Parse("#9E6A03"), EditorTheme.Dark.GetColor("editor.findMatchBackground"));
        Assert.Equal(ThemeColor.Parse("#EA5C0055"), EditorTheme.Dark.GetColor("editor.findMatchHighlightBackground"));
        Assert.Null(EditorTheme.Dark.GetColor("nope"));
    }

    [Fact]
    public void Resolve_UnknownColor_IsNull()
    {
        Assert.False(EditorColorDefaults.IsKnown("nope"));
        Assert.Null(EditorColorDefaults.Resolve("nope", ThemeType.Dark, NoThemeColors));
    }

    [Theory]
    [InlineData("vs", ThemeType.Light)]
    [InlineData("vs-dark", ThemeType.Dark)]
    [InlineData("hc-black", ThemeType.HighContrastDark)]
    [InlineData("hc-light", ThemeType.HighContrastLight)]
    [InlineData("hc-black extra", ThemeType.HighContrastDark)]
    [InlineData("whatever", ThemeType.Dark)]
    [InlineData(null, ThemeType.Dark)]
    public void ThemeType_FromUiTheme(string? uiTheme, ThemeType expected)
    {
        Assert.Equal(expected, ThemeTypes.FromUiTheme(uiTheme));
    }

    // Monaco: editorColors.ts:125
    [Fact]
    public void SelectionForeground_Dark_HasNoDefault() => Assert.Null(Default("editor.selectionForeground", ThemeType.Dark));

    [Fact]
    public void SelectionForeground_Light_HasNoDefault() => Assert.Null(Default("editor.selectionForeground", ThemeType.Light));

    [Fact]
    public void SelectionForeground_HcDark_IsBlack() => Assert.Equal("#000000FF", Default("editor.selectionForeground", ThemeType.HighContrastDark));

    [Fact]
    public void SelectionForeground_HcLight_IsWhite() => Assert.Equal("#FFFFFFFF", Default("editor.selectionForeground", ThemeType.HighContrastLight));
}
