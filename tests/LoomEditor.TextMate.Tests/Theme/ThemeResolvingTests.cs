// Ported from microsoft/vscode-textmate: src/tests/themes.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.TextMate.Theme;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Tests.Theme;

public class ThemeResolvingTests
{
    private const int NumberNotSet = 0;

    // vscode-textmate: 'Theme resolving always has defaults'
    [Fact]
    public void ThemeResolvingAlwaysHasDefaults()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme([]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#000000");
        int b = colorMap.GetId("#ffffff");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet)));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving respects incoming defaults 1' … '5'
    [Theory]
    [InlineData(1, (int)FontStyle.NotSet, null, null, (int)FontStyle.None, "#000000", "#ffffff")]
    [InlineData(2, (int)FontStyle.None, null, null, (int)FontStyle.None, "#000000", "#ffffff")]
    [InlineData(3, (int)FontStyle.Bold, null, null, (int)FontStyle.Bold, "#000000", "#ffffff")]
    [InlineData(4, (int)FontStyle.NotSet, "#ff0000", null, (int)FontStyle.None, "#ff0000", "#ffffff")]
    [InlineData(5, (int)FontStyle.NotSet, null, "#ff0000", (int)FontStyle.None, "#000000", "#ff0000")]
    public void ThemeResolvingRespectsIncomingDefaults(int number, int fontStyle, string? foreground, string? background,
        int expectedFontStyle, string expectedForeground, string expectedBackground)
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme([new ParsedThemeRule("", null, -1, (FontStyle)fontStyle, foreground, background)]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId(expectedForeground);
        int b = colorMap.GetId(expectedBackground);
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes((FontStyle)expectedFontStyle, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet)));
        AssertThemeEqual(actual, expected, $"Theme resolving respects incoming defaults {number}");
    }

    // vscode-textmate: 'Theme resolving can merge incoming defaults'
    [Fact]
    public void ThemeResolvingCanMergeIncomingDefaults()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, null, "#ff0000"),
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#00ff00", null),
            new ParsedThemeRule("", null, -1, FontStyle.Bold, null, null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#00ff00");
        int b = colorMap.GetId("#ff0000");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.Bold, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet)));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving defaults are inherited'
    [Fact]
    public void ThemeResolvingDefaultsAreInherited()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new ParsedThemeRule("var", null, -1, FontStyle.NotSet, "#ff0000", null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#F8F8F2");
        int b = colorMap.GetId("#272822");
        int c = colorMap.GetId("#ff0000");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet), [], new()
            {
                ["var"] = new ThemeTrieElement(new ThemeTrieElementRule(1, null, FontStyle.NotSet, c, NumberNotSet)),
            }));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving same rules get merged'
    [Fact]
    public void ThemeResolvingSameRulesGetMerged()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new ParsedThemeRule("var", null, 1, FontStyle.Bold, null, null),
            new ParsedThemeRule("var", null, 0, FontStyle.NotSet, "#ff0000", null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#F8F8F2");
        int b = colorMap.GetId("#272822");
        int c = colorMap.GetId("#ff0000");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet), [], new()
            {
                ["var"] = new ThemeTrieElement(new ThemeTrieElementRule(1, null, FontStyle.Bold, c, NumberNotSet)),
            }));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving rules are inherited 1'
    [Fact]
    public void ThemeResolvingRulesAreInherited1()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new ParsedThemeRule("var", null, -1, FontStyle.Bold, "#ff0000", null),
            new ParsedThemeRule("var.identifier", null, -1, FontStyle.NotSet, "#00ff00", null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#F8F8F2");
        int b = colorMap.GetId("#272822");
        int c = colorMap.GetId("#ff0000");
        int d = colorMap.GetId("#00ff00");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet), [], new()
            {
                ["var"] = new ThemeTrieElement(new ThemeTrieElementRule(1, null, FontStyle.Bold, c, NumberNotSet), [], new()
                {
                    ["identifier"] = new ThemeTrieElement(new ThemeTrieElementRule(2, null, FontStyle.Bold, d, NumberNotSet)),
                }),
            }));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving rules are inherited 2'
    [Fact]
    public void ThemeResolvingRulesAreInherited2()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new ParsedThemeRule("var", null, -1, FontStyle.Bold, "#ff0000", null),
            new ParsedThemeRule("var.identifier", null, -1, FontStyle.NotSet, "#00ff00", null),
            new ParsedThemeRule("constant", null, 4, FontStyle.Italic, "#100000", null),
            new ParsedThemeRule("constant.numeric", null, 5, FontStyle.NotSet, "#200000", null),
            new ParsedThemeRule("constant.numeric.hex", null, 6, FontStyle.Bold, null, null),
            new ParsedThemeRule("constant.numeric.oct", null, 7, FontStyle.Bold | FontStyle.Italic | FontStyle.Underline, null, null),
            new ParsedThemeRule("constant.numeric.dec", null, 8, FontStyle.None, "#300000", null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#F8F8F2");
        int b = colorMap.GetId("#272822");
        int c = colorMap.GetId("#100000");
        int d = colorMap.GetId("#200000");
        int e = colorMap.GetId("#300000");
        int f = colorMap.GetId("#ff0000");
        int g = colorMap.GetId("#00ff00");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet), [], new()
            {
                ["var"] = new ThemeTrieElement(new ThemeTrieElementRule(1, null, FontStyle.Bold, f, NumberNotSet), [], new()
                {
                    ["identifier"] = new ThemeTrieElement(new ThemeTrieElementRule(2, null, FontStyle.Bold, g, NumberNotSet)),
                }),
                ["constant"] = new ThemeTrieElement(new ThemeTrieElementRule(1, null, FontStyle.Italic, c, NumberNotSet), [], new()
                {
                    ["numeric"] = new ThemeTrieElement(new ThemeTrieElementRule(2, null, FontStyle.Italic, d, NumberNotSet), [], new()
                    {
                        ["hex"] = new ThemeTrieElement(new ThemeTrieElementRule(3, null, FontStyle.Bold, d, NumberNotSet)),
                        ["oct"] = new ThemeTrieElement(new ThemeTrieElementRule(3, null, FontStyle.Bold | FontStyle.Italic | FontStyle.Underline, d, NumberNotSet)),
                        ["dec"] = new ThemeTrieElement(new ThemeTrieElementRule(3, null, FontStyle.None, e, NumberNotSet)),
                    }),
                }),
            }));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving rules with parent scopes'
    [Fact]
    public void ThemeResolvingRulesWithParentScopes()
    {
        TextMateTheme actual = TextMateTheme.CreateFromParsedTheme(
        [
            new ParsedThemeRule("", null, -1, FontStyle.NotSet, "#F8F8F2", "#272822"),
            new ParsedThemeRule("var", null, -1, FontStyle.Bold, "#100000", null),
            new ParsedThemeRule("var.identifier", null, -1, FontStyle.NotSet, "#200000", null),
            new ParsedThemeRule("var", ["source.css"], 1, FontStyle.Italic, "#300000", null),
            new ParsedThemeRule("var", ["source.css"], 2, FontStyle.Underline, null, null),
        ]);
        var colorMap = new ColorMap();
        int a = colorMap.GetId("#F8F8F2");
        int b = colorMap.GetId("#272822");
        int c = colorMap.GetId("#100000");
        int d = colorMap.GetId("#300000");
        int e = colorMap.GetId("#200000");
        var expected = new TextMateTheme(
            colorMap,
            new StyleAttributes(FontStyle.None, a, b),
            new ThemeTrieElement(new ThemeTrieElementRule(0, null, FontStyle.NotSet, NumberNotSet, NumberNotSet), [], new()
            {
                ["var"] = new ThemeTrieElement(
                    new ThemeTrieElementRule(1, null, FontStyle.Bold, c, 0),
                    [new ThemeTrieElementRule(1, ["source.css"], FontStyle.Underline, d, NumberNotSet)],
                    new()
                    {
                        ["identifier"] = new ThemeTrieElement(
                            new ThemeTrieElementRule(2, null, FontStyle.Bold, e, NumberNotSet),
                            [new ThemeTrieElementRule(1, ["source.css"], FontStyle.Underline, d, NumberNotSet)]),
                    }),
            }));
        AssertThemeEqual(actual, expected);
    }

    // vscode-textmate: 'Theme resolving a rule with child combinator'
    [Fact]
    public void ThemeResolvingARuleWithChildCombinator()
    {
        TextMateTheme theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting(null, "#100000"),
            Setting("b a", "#200000"),
            Setting("b > a", "#300000"),
            Setting("c > b > a", "#400000"),
            Setting("a", "#500000"),
        ]));

        string?[] colorMap = theme.GetColorMap();
        string? Match(params string[] path)
        {
            StyleAttributes? result = theme.Match(ScopeStack.From(path));
            return result is null ? null : colorMap[result.ForegroundId];
        }

        Assert.True(Match("b", "a") == "#300000", "b a");
        Assert.True(Match("b", "c", "a") == "#200000", "b c a");
        Assert.True(Match("c", "b", "a") == "#400000", "c b a");
        Assert.True(Match("c", "b", "d", "a") == "#200000", "c b d a");
    }

    // vscode-textmate: 'Theme resolving should give deeper scopes higher specificity (#233)'
    [Fact]
    public void ThemeResolvingShouldGiveDeeperScopesHigherSpecificity233()
    {
        TextMateTheme theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting(null, "#100000"),
            Setting("y.z a.b", "#200000"),
            Setting("x y a.b", "#300000"),
        ]));

        string?[] colorMap = theme.GetColorMap();
        string? Match(params string[] path)
        {
            StyleAttributes? result = theme.Match(ScopeStack.From(path));
            return result is null || result.ForegroundId == 0 ? null : colorMap[result.ForegroundId];
        }

        // Sanity check
        Assert.True(Match("x", "a.b") is null, "x a.b");
        Assert.True(Match("y", "a.b") is null, "y a.b");
        Assert.True(Match("y.z", "a") is null, "y.z a");
        Assert.True(Match("x", "y", "a.b") == "#300000", "x y a.b");

        // Even though the "x y a.b" rule has more scopes in its path, the "y.z a.b" rule has
        // a deeper match, so it should take precedence.
        Assert.True(Match("x", "y.z", "a.b") == "#200000", "y.z a.b");
    }

    // vscode-textmate: 'Theme resolving issue #38: ignores rules with invalid colors'
    [Fact]
    public void ThemeResolvingIssue38IgnoresRulesWithInvalidColors()
    {
        List<ParsedThemeRule> actual = ThemeParser.ParseTheme(new RawTheme(
        [
            new RawThemeSetting(null, null, null, new RawThemeSettings(null, "#cccccc", "#222222")),
            new RawThemeSetting("Variable", "variable", null, new RawThemeSettings("", null, null)),
            new RawThemeSetting("Function argument", "variable.parameter", null, new RawThemeSettings("italic", "", null)),
            new RawThemeSetting("Library variable", "support.other.variable", null, new RawThemeSettings("", null, null)),
            new RawThemeSetting("Function argument", "variable.other", null, new RawThemeSettings("normal", "", null)),
            new RawThemeSetting("Coffeescript Function argument", "variable.parameter.function.coffee", null, new RawThemeSettings("italic", "#F9D423", null)),
        ]));

        ParsedThemeRule[] expected =
        [
            new("", null, 0, FontStyle.NotSet, "#cccccc", "#222222"),
            new("variable", null, 1, FontStyle.None, null, null),
            new("variable.parameter", null, 2, FontStyle.Italic, null, null),
            new("support.other.variable", null, 3, FontStyle.None, null, null),
            new("variable.other", null, 4, FontStyle.None, null, null),
            new("variable.parameter.function.coffee", null, 5, FontStyle.Italic, "#F9D423", null),
        ];

        Assert.Equal(expected.Select(Describe), actual.Select(Describe));
    }

    // vscode-textmate: 'Theme resolving issue #35: Trailing comma in a tmTheme scope selector'
    [Fact]
    public void ThemeResolvingIssue35TrailingCommaInATmThemeScopeSelector()
    {
        string scope = string.Join('\n',
            "meta.at-rule.return.scss,",
            "meta.at-rule.return.scss punctuation.definition,",
            "meta.at-rule.else.scss,",
            "meta.at-rule.else.scss punctuation.definition,",
            "meta.at-rule.if.scss,",
            "meta.at-rule.if.scss punctuation.definition,");
        List<ParsedThemeRule> actual = ThemeParser.ParseTheme(new RawTheme(
        [
            new RawThemeSetting(null, null, null, new RawThemeSettings(null, "#EFEFEF", "#25292C")),
            new RawThemeSetting("CSS at-rule keyword control", scope, null, new RawThemeSettings(null, "#CC7832", null)),
        ]));

        ParsedThemeRule[] expected =
        [
            new("", null, 0, FontStyle.NotSet, "#EFEFEF", "#25292C"),
            new("meta.at-rule.return.scss", null, 1, FontStyle.NotSet, "#CC7832", null),
            new("punctuation.definition", ["meta.at-rule.return.scss"], 1, FontStyle.NotSet, "#CC7832", null),
            new("meta.at-rule.else.scss", null, 1, FontStyle.NotSet, "#CC7832", null),
            new("punctuation.definition", ["meta.at-rule.else.scss"], 1, FontStyle.NotSet, "#CC7832", null),
            new("meta.at-rule.if.scss", null, 1, FontStyle.NotSet, "#CC7832", null),
            new("punctuation.definition", ["meta.at-rule.if.scss"], 1, FontStyle.NotSet, "#CC7832", null),
        ];

        Assert.Equal(expected.Select(Describe), actual.Select(Describe));
    }

    // assertThemeEqual. Upstream compares the expected color map only (its expected defaults and trie are taken from
    // `actual` by mistake); this port compares all three.
    private static void AssertThemeEqual(TextMateTheme actual, TextMateTheme expected, string? message = null)
    {
        Assert.Equal(expected.GetColorMap(), actual.GetColorMap());
        Assert.Equal(expected.GetDefaults(), actual.GetDefaults());
        Assert.True(Describe(expected.Root) == Describe(actual.Root), $"{message}\nexpected: {Describe(expected.Root)}\nactual:   {Describe(actual.Root)}");
    }

    private static string Describe(ThemeTrieElement element)
    {
        var text = new StringBuilder();
        text.Append('{').Append(Describe(element.MainRule)).Append(" [");
        text.AppendJoin(", ", element.RulesWithParentScopes.Select(Describe));
        text.Append("] {");
        foreach (KeyValuePair<string, ThemeTrieElement> child in element.Children.OrderBy(c => c.Key, StringComparer.Ordinal))
        {
            text.Append(child.Key).Append(": ").Append(Describe(child.Value)).Append("; ");
        }

        return text.Append("}}").ToString();
    }

    private static string Describe(ThemeTrieElementRule rule) =>
        $"({rule.ScopeDepth}, [{string.Join(' ', rule.ParentScopes)}], {rule.FontStyle}, {rule.Foreground}, {rule.Background})";

    private static string Describe(ParsedThemeRule rule) =>
        $"{rule.Scope}|{(rule.ParentScopes is null ? "null" : string.Join(",", rule.ParentScopes))}|{rule.Index}|{rule.FontStyle}|{rule.Foreground}|{rule.Background}";

    private static RawThemeSetting Setting(string? scope, string foreground) =>
        new(null, scope, null, new RawThemeSettings(null, foreground, null));
}
