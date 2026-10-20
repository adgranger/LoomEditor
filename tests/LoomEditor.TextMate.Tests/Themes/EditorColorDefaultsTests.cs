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

    [Fact]
    public void Defaults_ReferenceToAnotherColor_UsesTheThemeValueFirst()
    {
        // editorLineNumber.activeForeground is `editorActiveLineNumber.foreground` unless the theme sets it itself.
        ThemeColor? Theme(string id) => id == "editorActiveLineNumber.foreground" ? ThemeColor.Parse("#123456") : null;

        Assert.Equal(ThemeColor.Parse("#123456"), EditorColorDefaults.Resolve("editorLineNumber.activeForeground", ThemeType.Dark, Theme));
        Assert.Equal(ThemeColor.Parse("#654321"), EditorColorDefaults.Resolve("editorLineNumber.activeForeground", ThemeType.Dark, id => id == "editorLineNumber.activeForeground" ? ThemeColor.Parse("#654321") : Theme(id)));
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
}
