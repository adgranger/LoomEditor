using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests: the expected values follow colorThemeData.ts and themeCompatibility.ts.
public class VsCodeThemeLoaderTests
{
    private static VsCodeTheme Load(string path, string? uiTheme, Dictionary<string, string> files) =>
        VsCodeThemeLoader.Load(path, uiTheme, p => files.GetValueOrDefault(p));

    private static VsCodeTheme Load(string json, string? uiTheme = "vs-dark") => Load("themes/t.json", uiTheme, new() { ["themes/t.json"] = json });

    private static string Color(ThemeColor? color) => color?.ToString() ?? "none";

    [Fact]
    public void Load_Include_ParentRulesComeFirst()
    {
        VsCodeTheme theme = Load(
            "themes/child.json",
            "vs-dark",
            new()
            {
                ["themes/child.json"] = """{ "include": "../base/parent.json", "colors": { "editor.background": "#111111" }, "tokenColors": [ { "scope": "child", "settings": { "foreground": "#222222" } } ] }""",
                ["base/parent.json"] = """{ "colors": { "editor.background": "#999999", "editor.foreground": "#888888" }, "tokenColors": [ { "scope": "parent", "settings": { "fontStyle": "bold" } } ] }""",
            });

        Assert.Equal("#111111FF", Color(theme.GetColor("editor.background"))); // the including file wins
        Assert.Equal("#888888FF", Color(theme.GetColor("editor.foreground"))); // inherited
        Assert.Equal(["parent", "child"], theme.TokenColors.Skip(1).Take(2).Select(r => r.Scopes[0]));
    }

    [Fact]
    public void Load_ColorDefault_RemovesInheritedColor()
    {
        VsCodeTheme theme = Load(
            "child.json",
            "vs-dark",
            new()
            {
                ["child.json"] = """{ "include": "./parent.json", "colors": { "editor.background": "default" } }""",
                ["parent.json"] = """{ "colors": { "editor.background": "#999999" } }""",
            });

        Assert.DoesNotContain("editor.background", theme.Colors.Keys);
        Assert.Equal("#1E1E1EFF", Color(theme.GetColor("editor.background"))); // the dark default
    }

    [Fact]
    public void Load_CommentsAndTrailingCommas_AreAccepted()
    {
        VsCodeTheme theme = Load("""
            {
              // a comment
              "colors": { "editor.background": "#101010", /* block */ },
              "tokenColors": [ { "scope": "a", "settings": { "foreground": "#202020", }, }, ],
            }
            """);

        Assert.Equal("#101010FF", Color(theme.GetColor("editor.background")));
        Assert.Equal("a", theme.TokenColors[1].Scopes[0]);
    }

    [Fact]
    public void TokenColors_FirstRuleIsTheDefaultForegroundAndBackground()
    {
        VsCodeTheme theme = Load("""{ "colors": { "editor.foreground": "#ABCDEF" }, "tokenColors": [ { "scope": "x", "settings": { "foreground": "#123" } } ] }""");

        TokenColorRule first = theme.TokenColors[0];
        Assert.Empty(first.Scopes);
        Assert.Equal("#ABCDEFFF", Color(first.Foreground));
        Assert.Equal("#1E1E1EFF", Color(first.Background)); // not set: the dark default
        Assert.Equal("#112233FF", Color(theme.TokenColors[1].Foreground)); // #RGB is expanded
    }

    [Fact]
    public void TokenColors_NoInfoTokenRule_AppendsDefaultThemeColors()
    {
        VsCodeTheme theme = Load("""{ "tokenColors": [ { "scope": "a, b", "settings": { "foreground": "#222222" } } ] }""");

        Assert.Equal(["a", " b"], theme.TokenColors[1].Scopes); // comma separated; vscode-textmate trims later
        Assert.Equal(["token.info-token", "token.warn-token", "token.error-token", "token.debug-token"], theme.TokenColors.Skip(2).Select(r => r.Scopes[0]));
        Assert.Equal("#6796E6FF", Color(theme.TokenColors[2].Foreground));

        VsCodeTheme withInfo = Load("""{ "tokenColors": [ { "scope": "token.info-token", "settings": { "foreground": "#222222" } } ] }""");
        Assert.Equal(2, withInfo.TokenColors.Count); // default rule + the theme's own, nothing appended

        Assert.Equal("#316BCDFF", Color(Load("{}", "vs").TokenColors[1].Foreground)); // the light defaults
    }

    [Fact]
    public void TokenColorIndex_SameColorTwice_SameId_AndIdsStartAtOne()
    {
        var index = new TokenColorIndex();

        Assert.Equal(0, index.Add(null));
        Assert.Equal(1, index.Add(ThemeColor.Parse("#112233")));
        Assert.Equal(2, index.Add(ThemeColor.Parse("#445566")));
        Assert.Equal(1, index.Add(ThemeColor.Parse("#112233")));
        Assert.Equal(1, index.Add(ThemeColor.Parse("#112233FF"))); // an FF alpha is the same color
        Assert.Equal(3, index.Add(ThemeColor.Parse("#11223380")));
        Assert.Equal([default, ThemeColor.Parse("#112233"), ThemeColor.Parse("#445566"), ThemeColor.Parse("#11223380")], index.AsArray());
    }

    [Fact]
    public void TokenColorMap_ListsTheColorsOfTheRulesInOrder()
    {
        VsCodeTheme theme = Load("""{ "colors": { "editor.foreground": "#111111", "editor.background": "#222222" }, "tokenColors": [ { "scope": "x", "settings": { "foreground": "#333333" } } ] }""");

        Assert.Equal(default, theme.TokenColorMap[0]);
        Assert.Equal(["#111111FF", "#222222FF", "#333333FF"], theme.TokenColorMap.Skip(1).Take(3).Select(c => c.ToString()));
    }

    [Fact]
    public void Load_SettingsForm_ConvertsScopelessSettingsToColors()
    {
        VsCodeTheme theme = Load("""
            { "settings": [
                { "settings": { "background": "#101010", "foreground": "#202020", "caret": "#303030", "lineHighlight": "#404040", "findMatchHighlight": "#505050" } },
                { "scope": "comment", "settings": { "foreground": "#606060", "fontStyle": "italic" } }
            ] }
            """);

        Assert.Equal("#101010FF", Color(theme.GetColor("editor.background")));
        Assert.Equal("#202020FF", Color(theme.GetColor("editor.foreground")));
        Assert.Equal("#303030FF", Color(theme.GetColor("editorCursor.foreground")));
        Assert.Equal("#404040FF", Color(theme.GetColor("editor.lineHighlightBackground")));
        Assert.Equal("#505050FF", Color(theme.GetColor("editor.findMatchHighlightBackground")));
        Assert.Equal("#505050FF", Color(theme.GetColor("peekViewResult.matchHighlightBackground")));
        TokenColorRule rule = theme.TokenColors[1];
        Assert.Equal(("comment", "italic", "#606060FF"), (rule.Scopes[0], rule.FontStyle, Color(rule.Foreground)));
    }

    [Fact]
    public void Load_TokenColorsAsPath_IsRejected()
    {
        FormatException e = Assert.Throws<FormatException>(() => Load("""{ "tokenColors": "./tokens.tmTheme" }"""));

        Assert.Contains("themes/t.json", e.Message);
    }

    [Fact]
    public void Load_BrokenFiles_ThrowFormatExceptionWithThePath()
    {
        Assert.Contains("themes/t.json", Assert.Throws<FormatException>(() => Load("{ not json")).Message);
        Assert.Contains("themes/t.json", Assert.Throws<FormatException>(() => Load("[]")).Message);
        Assert.Contains("themes/t.json", Assert.Throws<FormatException>(() => Load("""{ "colors": [] }""")).Message);
        Assert.Contains("gone.json", Assert.Throws<FormatException>(() => Load("""{ "include": "gone.json" }""")).Message);
        Assert.Contains("a.tmTheme", Assert.Throws<FormatException>(() => Load("a.tmTheme", null, [])).Message);
    }

    [Fact]
    public void Load_IncludeCycle_Throws()
    {
        Assert.Throws<FormatException>(() => Load("a.json", null, new() { ["a.json"] = """{ "include": "a.json" }""" }));
    }

    [Fact]
    public void Load_InvalidColor_IsRed_LikeVsCode()
    {
        VsCodeTheme theme = Load("""{ "colors": { "editor.background": "red", "editor.foreground": "#12" } }""");

        Assert.Equal("#FF0000FF", Color(theme.GetColor("editor.background")));
        Assert.Equal("#FF0000FF", Color(theme.GetColor("editor.foreground")));
    }

    [Theory]
    [InlineData("vs", ThemeType.Light)]
    [InlineData(null, ThemeType.Dark)]
    [InlineData("hc-black", ThemeType.HighContrastDark)]
    public void Load_UiTheme_GivesTheType(string? uiTheme, ThemeType expected)
    {
        Assert.Equal(expected, Load("{}", uiTheme).Type);
    }

    [Theory]
    [InlineData("#abc", "#AABBCC")]
    [InlineData("#abcd", "#AABBCCDD")]
    [InlineData("#abcf", "#AABBCC")]
    [InlineData("#a1b2c3", "#A1B2C3")]
    [InlineData("#a1b2c3ff", "#A1B2C3")]
    [InlineData("#a1b2c380", "#A1B2C380")]
    [InlineData("#12345", null)]
    [InlineData("abc", null)]
    [InlineData("#ggg", null)]
    [InlineData("", null)]
    public void NormalizeColor_FollowsVsCode(string text, string? expected)
    {
        Assert.Equal(expected, ColorNormalization.Normalize(text));
    }

    [Fact]
    public void ThemeColor_Parse_FourDigits()
    {
        Assert.Equal(new ThemeColor(0x88, 0xAA, 0xBB, 0xCC), ThemeColor.Parse("#abc8"));
    }
}
