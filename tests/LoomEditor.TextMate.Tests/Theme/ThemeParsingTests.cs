// Ported from microsoft/vscode-textmate: src/tests/themes.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Tests.Theme;

public class ThemeParsingTests
{
    // vscode-textmate: 'Theme parsing can parse'
    [Fact]
    public void ThemeParsingCanParse()
    {
        List<ParsedThemeRule> actual = ThemeParser.ParseTheme(new RawTheme(
        [
            Setting((string?)null, foreground: "#F8F8F2", background: "#272822"),
            Setting("source, something", background: "#100000"),
            Setting(["bar", "baz"], background: "#010000"),
            Setting("source.css selector bar", fontStyle: "bold"),
            Setting("constant", fontStyle: "italic", foreground: "#ff0000"),
            Setting("constant.numeric", foreground: "#00ff00"),
            Setting("constant.numeric.hex", fontStyle: "bold"),
            Setting("constant.numeric.oct", fontStyle: "bold italic underline"),
            Setting("constant.numeric.bin", fontStyle: "bold strikethrough"),
            Setting("constant.numeric.dec", fontStyle: "", foreground: "#0000ff"),
            Setting("foo", fontStyle: "", foreground: "#CFA"),
        ]));

        ParsedThemeRule[] expected =
        [
            new("", null, 0, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new("source", null, 1, FontStyle.NotSet, null, "#100000"),
            new("something", null, 1, FontStyle.NotSet, null, "#100000"),
            new("bar", null, 2, FontStyle.NotSet, null, "#010000"),
            new("baz", null, 2, FontStyle.NotSet, null, "#010000"),
            new("bar", ["selector", "source.css"], 3, FontStyle.Bold, null, null),
            new("constant", null, 4, FontStyle.Italic, "#ff0000", null),
            new("constant.numeric", null, 5, FontStyle.NotSet, "#00ff00", null),
            new("constant.numeric.hex", null, 6, FontStyle.Bold, null, null),
            new("constant.numeric.oct", null, 7, FontStyle.Bold | FontStyle.Italic | FontStyle.Underline, null, null),
            new("constant.numeric.bin", null, 8, FontStyle.Bold | FontStyle.Strikethrough, null, null),
            new("constant.numeric.dec", null, 9, FontStyle.None, "#0000ff", null),
            new("foo", null, 10, FontStyle.None, "#CFA", null),
        ];

        Assert.Equal(expected.Select(Describe), actual.Select(Describe));
    }

    // vscode-textmate: 'Theme resolving strcmp works'
    [Fact]
    public void ThemeResolvingStrcmpWorks()
    {
        List<string> actual = ["bar", "z", "zu", "a", "ab", ""];
        actual.Sort(Strings.Strcmp);

        Assert.Equal(["", "a", "ab", "bar", "z", "zu"], actual);
    }

    // vscode-textmate: 'Theme resolving strArrCmp works'
    [Fact]
    public void ThemeResolvingStrArrCmpWorks()
    {
        AssertStrArrCmp("001", null, null, 0);
        AssertStrArrCmp("002", null, [], -1);
        AssertStrArrCmp("003", null, ["a"], -1);
        AssertStrArrCmp("004", [], null, 1);
        AssertStrArrCmp("005", ["a"], null, 1);
        AssertStrArrCmp("006", [], [], 0);
        AssertStrArrCmp("007", [], ["a"], -1);
        AssertStrArrCmp("008", ["a"], [], 1);
        AssertStrArrCmp("009", ["a"], ["a"], 0);
        AssertStrArrCmp("010", ["a", "b"], ["a"], 1);
        AssertStrArrCmp("011", ["a"], ["a", "b"], -1);
        AssertStrArrCmp("012", ["a", "b"], ["a", "b"], 0);
        AssertStrArrCmp("013", ["a", "b"], ["a", "c"], -1);
        AssertStrArrCmp("014", ["a", "c"], ["a", "b"], 1);

        static void AssertStrArrCmp(string testCase, string[]? a, string[]? b, int expected) =>
            Assert.True(Strings.StrArrCmp(a, b) == expected, testCase);
    }

    [Fact]
    public void ParseTheme_ScopeString_TrimsCommasAndKeepsEmptyParts()
    {
        List<ParsedThemeRule> rules = ThemeParser.ParseTheme(new RawTheme([Setting(",,a,,b,,", foreground: "#fff")]));

        Assert.Equal(["a", "", "b"], rules.Select(r => r.Scope));
    }

    [Fact]
    public void ParseTheme_InvalidColorsAndMissingSettings_AreIgnored()
    {
        List<ParsedThemeRule> rules = ThemeParser.ParseTheme(new RawTheme(
        [
            new RawThemeSetting(null, "a", null, null),
            Setting("b", foreground: "red", background: "#12345"),
        ]));

        ParsedThemeRule rule = Assert.Single(rules);
        Assert.Equal(("b", null, null), (rule.Scope, rule.Foreground, rule.Background));
        Assert.Empty(ThemeParser.ParseTheme(null));
        Assert.Empty(ThemeParser.ParseTheme(new RawTheme(null)));
    }

    [Fact]
    public void FontStyleToString_ListsTheFlags()
    {
        Assert.Equal("not set", FontStyle.NotSet.FontStyleToString());
        Assert.Equal("none", FontStyle.None.FontStyleToString());
        Assert.Equal("italic bold underline strikethrough",
            (FontStyle.Italic | FontStyle.Bold | FontStyle.Underline | FontStyle.Strikethrough).FontStyleToString());
    }

    [Fact]
    public void ScopeStack_Extends_And_GetSegments()
    {
        ScopeStack root = ScopeStack.From("source.js")!;
        ScopeStack child = root.Push("meta.block").Push("string");
        ScopeStack? pushed = ScopeStack.Push(root, ["a", "b"]);

        Assert.Equal(["source.js", "meta.block", "string"], child.GetSegments());
        Assert.Equal("source.js meta.block string", child.ToString());
        Assert.True(child.Extends(root));
        Assert.True(child.Extends(child));
        Assert.False(root.Extends(child));
        Assert.False(child.Extends(ScopeStack.From("source.js")!));
        Assert.Equal(["meta.block", "string"], child.GetExtensionIfDefined(root));
        Assert.Null(pushed!.GetExtensionIfDefined(child));
        Assert.Equal(["source.js", "a", "b"], pushed.GetSegments());
        Assert.Null(ScopeStack.From());
    }

    [Theory]
    [InlineData("a b c", "c", true)]
    [InlineData("a b c", "b c", true)]
    [InlineData("a b c", "a c", true)]
    [InlineData("a b c", "b a c", false)]
    [InlineData("a b.x c", "b c", true)]
    [InlineData("a bb c", "b c", false)]
    [InlineData("a b c", "b > c", true)]
    [InlineData("a b c", "a > c", false)]
    [InlineData("a b c", "> c", false)]
    public void ScopePathMatchesParentScopes_FollowsUpstream(string path, string parents, bool expected)
    {
        // The last scope of `path` is the scope being styled; the parent scopes are given deepest first.
        string[] segments = path.Split(' ');
        ScopeStack? parentPath = ScopeStack.From(segments.AsSpan(0, segments.Length - 1));
        string[] parentScopes = parents.Split(' ')[..^1];
        Array.Reverse(parentScopes);

        Assert.Equal(expected, ThemeScopes.ScopePathMatchesParentScopes(parentPath, parentScopes));
    }

    [Fact]
    public void ColorMap_GivesIdsFromOneAndUpperCases()
    {
        var colorMap = new ColorMap();

        Assert.Equal(0, colorMap.GetId(null));
        Assert.Equal(1, colorMap.GetId("#fff"));
        Assert.Equal(2, colorMap.GetId("#000000"));
        Assert.Equal(1, colorMap.GetId("#FFF"));
        Assert.Equal([null, "#FFF", "#000000"], colorMap.GetColorMap());
    }

    [Fact]
    public void ColorMap_Frozen_ThrowsOnUnknownColor()
    {
        var colorMap = new ColorMap(["#000000", "#FFFFFF", "#ff0000"]);

        Assert.Equal(1, colorMap.GetId("#ffffff"));
        // Upstream stores the given colors as they are and looks up the upper-cased color.
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => colorMap.GetId("#ff0000"));
        Assert.Equal("Missing color in color map - #FF0000", exception.Message);
    }

    private static RawThemeSetting Setting(string? scope, string? fontStyle = null, string? foreground = null, string? background = null) =>
        new(null, scope, null, new RawThemeSettings(fontStyle, foreground, background));

    private static RawThemeSetting Setting(string[] scopes, string? fontStyle = null, string? foreground = null, string? background = null) =>
        new(null, null, scopes, new RawThemeSettings(fontStyle, foreground, background));

    private static string Describe(ParsedThemeRule rule) =>
        $"{rule.Scope}|{(rule.ParentScopes is null ? "null" : string.Join(",", rule.ParentScopes))}|{rule.Index}|{rule.FontStyle}|{rule.Foreground}|{rule.Background}";
}
