// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Theme;

/// <summary><c>Theme</c> of vscode-textmate: resolves the style of a scope path.</summary>
/// <remarks>
/// Threading: single-threaded (the match cache is a <see cref="Dictionary{TKey, TValue}"/>); each thread that tokenizes
/// has its own theme (ADR 0008, decision 5).
/// </remarks>
internal sealed class Theme
{
    private readonly ColorMap _colorMap;
    private readonly StyleAttributes _defaults;
    private readonly ThemeTrieElement _root;
    private readonly CachedFn<string, List<ThemeTrieElementRule>> _cachedMatchRoot;

    /// <summary>Creates a theme.</summary>
    public Theme(ColorMap colorMap, StyleAttributes defaults, ThemeTrieElement root)
    {
        _colorMap = colorMap;
        _defaults = defaults;
        _root = root;
        _cachedMatchRoot = new CachedFn<string, List<ThemeTrieElementRule>>(scopeName => _root.Match(scopeName));
    }

    /// <summary>The trie (for tests).</summary>
    internal ThemeTrieElement Root => _root;

    /// <summary><c>createFromRawTheme</c>.</summary>
    public static Theme CreateFromRawTheme(RawTheme? source, IReadOnlyList<string>? colorMap = null) =>
        CreateFromParsedTheme(ThemeParser.ParseTheme(source), colorMap);

    /// <summary><c>createFromParsedTheme</c>.</summary>
    public static Theme CreateFromParsedTheme(IReadOnlyList<ParsedThemeRule> source, IReadOnlyList<string>? colorMap = null) =>
        ThemeParser.ResolveParsedThemeRules(source, colorMap);

    /// <summary><c>getColorMap</c>.</summary>
    public string?[] GetColorMap() => _colorMap.GetColorMap();

    /// <summary><c>getDefaults</c>.</summary>
    public StyleAttributes GetDefaults() => _defaults;

    /// <summary>
    /// <c>match</c>: the style of the most specific rule whose parent scopes match <paramref name="scopePath"/>; the
    /// defaults for <see langword="null"/>; <see langword="null"/> when no rule matches.
    /// </summary>
    public StyleAttributes? Match(ScopeStack? scopePath)
    {
        if (scopePath is null)
        {
            return _defaults;
        }

        string scopeName = scopePath.ScopeName;
        List<ThemeTrieElementRule> matchingTrieElements = _cachedMatchRoot.Get(scopeName);

        ThemeTrieElementRule? effectiveRule = matchingTrieElements.Find(v =>
            ThemeScopes.ScopePathMatchesParentScopes(scopePath.Parent, v.ParentScopes));
        if (effectiveRule is null)
        {
            return null;
        }

        return new StyleAttributes(effectiveRule.FontStyle, effectiveRule.Foreground, effectiveRule.Background);
    }
}
