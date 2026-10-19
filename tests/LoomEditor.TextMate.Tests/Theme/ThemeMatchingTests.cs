// Ported from microsoft/vscode-textmate: src/tests/themes.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Theme;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Tests.Theme;

// Not ported: the data-driven theme tests of themes.test.ts (lines 73–123, 'Tokenize with theme <name>') and the timing
// tests (lines 125 and 129). They need vscode-textmate's 11 MB test-cases/themes folder and older grammars; the golden
// tests (P4-48) cover the same path (grammar + theme → colors) with our own grammars and themes.
public class ThemeMatchingTests
{
    private static readonly TextMateTheme CanMatchTheme = TextMateTheme.CreateFromRawTheme(new RawTheme(
    [
        Setting((string?)null, foreground: "#F8F8F2", background: "#272822"),
        Setting("source, something", background: "#100000"),
        Setting(["bar", "baz"], background: "#200000"),
        Setting("source.css selector bar", fontStyle: "bold"),
        Setting("constant", fontStyle: "italic", foreground: "#300000"),
        Setting("constant.numeric", foreground: "#400000"),
        Setting("constant.numeric.hex", fontStyle: "bold"),
        Setting("constant.numeric.oct", fontStyle: "bold italic underline"),
        Setting("constant.numeric.dec", fontStyle: "", foreground: "#500000"),
        Setting("storage.object.bar", fontStyle: "", foreground: "#600000"),
    ]));

    public static TheoryData<string, string[], string> CanMatchCases => new()
    {
        { "simpleMatch1", ["source"], "background=#100000;fontStyle=not set" },
        { "simpleMatch2", ["source.ts"], "background=#100000;fontStyle=not set" },
        { "simpleMatch3", ["source.tss"], "background=#100000;fontStyle=not set" },
        { "simpleMatch4", ["something"], "background=#100000;fontStyle=not set" },
        { "simpleMatch5", ["something.ts"], "background=#100000;fontStyle=not set" },
        { "simpleMatch6", ["something.tss"], "background=#100000;fontStyle=not set" },
        { "simpleMatch7", ["baz"], "background=#200000;fontStyle=not set" },
        { "simpleMatch8", ["baz.ts"], "background=#200000;fontStyle=not set" },
        { "simpleMatch9", ["baz.tss"], "background=#200000;fontStyle=not set" },
        { "simpleMatch10", ["constant"], "foreground=#300000;fontStyle=italic" },
        { "simpleMatch11", ["constant.string"], "foreground=#300000;fontStyle=italic" },
        { "simpleMatch12", ["constant.hex"], "foreground=#300000;fontStyle=italic" },
        { "simpleMatch13", ["constant.numeric"], "foreground=#400000;fontStyle=italic" },
        { "simpleMatch14", ["constant.numeric.baz"], "foreground=#400000;fontStyle=italic" },
        { "simpleMatch15", ["constant.numeric.hex"], "foreground=#400000;fontStyle=bold" },
        { "simpleMatch16", ["constant.numeric.hex.baz"], "foreground=#400000;fontStyle=bold" },
        { "simpleMatch17", ["constant.numeric.oct"], "foreground=#400000;fontStyle=italic bold underline" },
        { "simpleMatch18", ["constant.numeric.oct.baz"], "foreground=#400000;fontStyle=italic bold underline" },
        { "simpleMatch19", ["constant.numeric.dec"], "foreground=#500000;fontStyle=none" },
        { "simpleMatch20", ["constant.numeric.dec.baz"], "foreground=#500000;fontStyle=none" },
        { "simpleMatch21", ["storage.object.bar"], "foreground=#600000;fontStyle=none" },
        { "simpleMatch22", ["storage.object.bar.baz"], "foreground=#600000;fontStyle=none" },
        { "simpleMatch23", ["storage.object.bart"], "fontStyle=not set" },
        { "simpleMatch24", ["storage.object"], "fontStyle=not set" },
        { "simpleMatch25", ["storage"], "fontStyle=not set" },
        { "defaultMatch1", [""], "fontStyle=not set" },
        { "defaultMatch2", ["bazz"], "fontStyle=not set" },
        { "defaultMatch3", ["asdfg"], "fontStyle=not set" },
        { "multiMatch1", ["bar"], "background=#200000;fontStyle=not set" },
        { "multiMatch2", ["source.css", "selector", "bar"], "background=#200000;fontStyle=bold" },
    };

    // vscode-textmate: 'Theme matching gives higher priority to deeper matches'
    [Fact]
    public void ThemeMatchingGivesHigherPriorityToDeeperMatches()
    {
        var theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting((string?)null, foreground: "#100000", background: "#200000"),
            Setting("punctuation.definition.string.begin.html", foreground: "#300000"),
            Setting("meta.tag punctuation.definition.string", foreground: "#400000"),
        ]));
        StyleAttributes? actual = theme.Match(ScopeStack.From("punctuation.definition.string.begin.html"));
        Assert.Equal("#300000", theme.GetColorMap()[actual!.ForegroundId]);
    }

    // vscode-textmate: 'Theme matching gives higher priority to parent matches 1'
    [Fact]
    public void ThemeMatchingGivesHigherPriorityToParentMatches1()
    {
        var theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting((string?)null, foreground: "#100000", background: "#200000"),
            Setting("c a", foreground: "#300000"),
            Setting("d a.b", foreground: "#400000"),
            Setting("a", foreground: "#500000"),
        ]));

        string?[] map = theme.GetColorMap();

        Assert.Equal("#400000", map[theme.Match(ScopeStack.From("d", "a.b"))!.ForegroundId]);
    }

    // vscode-textmate: 'Theme matching gives higher priority to parent matches 2'
    [Fact]
    public void ThemeMatchingGivesHigherPriorityToParentMatches2()
    {
        var theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting((string?)null, foreground: "#100000", background: "#200000"),
            Setting("meta.tag entity", foreground: "#300000"),
            Setting("meta.selector.css entity.name.tag", foreground: "#400000"),
            Setting("entity", foreground: "#500000"),
        ]));

        StyleAttributes? result = theme.Match(ScopeStack.From(
            "text.html.cshtml",
            "meta.tag.structure.any.html",
            "entity.name.tag.structure.any.html"));

        string?[] colorMap = theme.GetColorMap();
        Assert.Equal("#300000", colorMap[result!.ForegroundId]);
    }

    // vscode-textmate: suite 'Theme matching can match' ('simpleMatch1' … 'simpleMatch25', 'defaultMatch1' … '3', 'multiMatch1', 'multiMatch2')
    [Theory]
    [MemberData(nameof(CanMatchCases))]
    public void ThemeMatchingCanMatch(string name, string[] path, string expected)
    {
        Assert.True(expected == Match(path), $"{name}: {Match(path)}");
    }

    // vscode-textmate: 'Theme matching Microsoft/vscode#23460'
    [Fact]
    public void ThemeMatchingMicrosoftVscode23460()
    {
        var theme = TextMateTheme.CreateFromRawTheme(new RawTheme(
        [
            Setting((string?)null, foreground: "#aec2e0", background: "#14191f"),
            new RawThemeSetting("JSON String", "meta.structure.dictionary.json string.quoted.double.json", null, new RawThemeSettings(null, "#FF410D", null)),
            Setting("meta.structure.dictionary.json string.quoted.double.json", foreground: "#ffffff"),
            Setting("meta.structure.dictionary.value.json string.quoted.double.json", foreground: "#FF410D"),
        ]));

        ScopeStack? path = ScopeStack.From(
            "source.json",
            "meta.structure.dictionary.json",
            "meta.structure.dictionary.value.json",
            "string.quoted.double.json");
        StyleAttributes? result = theme.Match(path);
        Assert.Equal("#FF410D", theme.GetColorMap()[result!.ForegroundId]);
    }

    [Fact]
    public void Match_NullPath_ReturnsTheDefaults()
    {
        Assert.Same(CanMatchTheme.GetDefaults(), CanMatchTheme.Match(null));
    }

    private static string Match(params string[] path)
    {
        StyleAttributes? result = CanMatchTheme.Match(ScopeStack.From(path));
        if (result is null)
        {
            return "null";
        }

        string?[] map = CanMatchTheme.GetColorMap();
        var parts = new List<string>();
        if (result.ForegroundId != 0)
        {
            parts.Add("foreground=" + map[result.ForegroundId]);
        }

        if (result.BackgroundId != 0)
        {
            parts.Add("background=" + map[result.BackgroundId]);
        }

        parts.Add("fontStyle=" + result.FontStyle.FontStyleToString());
        return string.Join(';', parts);
    }

    private static RawThemeSetting Setting(string? scope, string? fontStyle = null, string? foreground = null, string? background = null) =>
        new(null, scope, null, new RawThemeSettings(fontStyle, foreground, background));

    private static RawThemeSetting Setting(string[] scopes, string? fontStyle = null, string? foreground = null, string? background = null) =>
        new(null, null, scopes, new RawThemeSettings(fontStyle, foreground, background));
}
