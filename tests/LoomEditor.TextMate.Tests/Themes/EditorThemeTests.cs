using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests.
public class EditorThemeTests
{
    [Fact]
    public void Load_CustomTheme_WithInclude()
    {
        const string Base = """{ "colors": { "editor.background": "#112233" }, "tokenColors": [ { "scope": "comment", "settings": { "foreground": "#00FF00" } } ] }""";
        const string Main = """{ "include": "./base.json", "tokenColors": [ { "scope": "string", "settings": { "foreground": "#FF0000", "fontStyle": "italic" } } ] }""";

        EditorTheme theme = EditorTheme.Load(Main, "vs-dark", "Mine", path => path == "base.json" ? Base : null);

        Assert.Equal("Mine", theme.Name);
        Assert.Equal(ThemeColor.Parse("#112233"), theme.Background);
        Assert.Equal(["comment"], theme.TokenColors[1].Scopes);
        Assert.Equal(["string"], theme.TokenColors[2].Scopes);
        Assert.Equal("italic", theme.TokenColors[2].FontStyle);
    }

    [Fact]
    public void Load_LightUiTheme_UsesLightDefaults()
    {
        EditorTheme theme = EditorTheme.Load("{}", "vs", "Empty");

        Assert.Equal(ThemeType.Light, theme.Type);
        Assert.Equal(ThemeColor.Parse("#FFFFFF"), theme.Background);
    }
}
