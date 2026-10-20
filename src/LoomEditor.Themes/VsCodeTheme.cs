// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/colorThemeData.ts (tokenColors, getColor, getTokenColorIndex, tokenColorMap, defaultThemeColors)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>A token rule as read from a theme file: it has scopes and settings (the others have no effect and are dropped on load).</summary>
/// <param name="Scopes">The scope selectors, never empty.</param>
/// <param name="Foreground">The foreground, as written.</param>
/// <param name="Background">The background, as written.</param>
/// <param name="FontStyle">The font style, as written.</param>
/// <param name="IsInfoToken">Whether the scope is exactly the string <c>token.info-token</c> (it replaces the default log colors).</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record ThemeRule(IReadOnlyList<string> Scopes, string? Foreground, string? Background, string? FontStyle, bool IsInfoToken);

/// <summary>
/// A loaded VS Code color theme (VS Code: <c>ColorThemeData</c>, loaded): its type, the colors the file sets, and the
/// token color rules and color map the tokenizer needs. Not ported: custom colors and token colors, semantic token
/// rules, font sizes and families.
/// </summary>
/// <remarks>Threading: immutable once loaded; the lazy values are computed from immutable data.</remarks>
internal sealed class VsCodeTheme
{
    // defaultThemeColors: the colors of the log tokens (token.info-token, …), when the theme has no token.info-token rule.
    private static readonly Dictionary<ThemeType, (string Scope, string Foreground)[]> DefaultThemeColors = new()
    {
        [ThemeType.Light] = [("token.info-token", "#316bcd"), ("token.warn-token", "#cd9731"), ("token.error-token", "#cd3131"), ("token.debug-token", "#800080")],
        [ThemeType.Dark] = [("token.info-token", "#6796e6"), ("token.warn-token", "#cd9731"), ("token.error-token", "#f44747"), ("token.debug-token", "#b267e6")],
        [ThemeType.HighContrastLight] = [("token.info-token", "#316bcd"), ("token.warn-token", "#cd9731"), ("token.error-token", "#cd3131"), ("token.debug-token", "#800080")],
        [ThemeType.HighContrastDark] = [("token.info-token", "#6796e6"), ("token.warn-token", "#008000"), ("token.error-token", "#FF0000"), ("token.debug-token", "#b267e6")],
    };

    private readonly IReadOnlyDictionary<string, ThemeColor> _colors;
    private readonly IReadOnlyList<ThemeRule> _themeRules;
    private IReadOnlyList<TokenColorRule>? _tokenColors;
    private IReadOnlyList<ThemeColor>? _tokenColorMap;

    /// <summary>Creates a loaded theme.</summary>
    public VsCodeTheme(ThemeType type, IReadOnlyDictionary<string, ThemeColor> colors, IReadOnlyList<ThemeRule> themeRules)
    {
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(themeRules);
        Type = type;
        _colors = colors;
        _themeRules = themeRules;
    }

    /// <summary>The type of the theme (<c>type</c>).</summary>
    public ThemeType Type { get; }

    /// <summary>The colors the theme files set, by id (<c>colorMap</c>).</summary>
    public IReadOnlyDictionary<string, ThemeColor> Colors => _colors;

    /// <summary><c>getColor(id, useDefault: true)</c>: the color the theme sets, else VS Code's default for the theme type.</summary>
    public ThemeColor? GetColor(string id) => EditorColorDefaults.Resolve(id, Type, key => _colors.TryGetValue(key, out ThemeColor c) ? c : null);

    /// <summary>
    /// <c>tokenColors</c>: rule 0 is the default (empty scopes, <c>editor.foreground</c> and <c>editor.background</c>),
    /// then the rules of the theme files (included files first), then the log token colors unless the theme has a
    /// <c>token.info-token</c> rule.
    /// </summary>
    public IReadOnlyList<TokenColorRule> TokenColors => _tokenColors ??= BuildTokenColors();

    /// <summary><c>tokenColorMap</c>: the colors of <see cref="TokenColors"/> by id; index 0 is unused.</summary>
    public IReadOnlyList<ThemeColor> TokenColorMap => _tokenColorMap ??= BuildTokenColorMap();

    private List<TokenColorRule> BuildTokenColors()
    {
        // the default rule (scope empty) is always the first rule. Ignore all other default rules.
        ThemeColor foreground = GetColor("editor.foreground") ?? throw new InvalidOperationException("editor.foreground has no default");
        ThemeColor background = GetColor("editor.background") ?? throw new InvalidOperationException("editor.background has no default");
        var result = new List<TokenColorRule> { new([], foreground, background, null) };

        bool hasDefaultTokens = false;
        foreach (ThemeRule rule in _themeRules)
        {
            hasDefaultTokens |= rule.IsInfoToken;
            result.Add(ToRule(rule.Scopes, rule.Foreground, rule.Background, rule.FontStyle));
        }

        if (!hasDefaultTokens)
        {
            foreach ((string scope, string color) in DefaultThemeColors[Type])
            {
                result.Add(ToRule([scope], color, null, null));
            }
        }

        return result;
    }

    private static TokenColorRule ToRule(IReadOnlyList<string> scopes, string? foreground, string? background, string? fontStyle) =>
        new(scopes, ParseNormalized(foreground), ParseNormalized(background), fontStyle);

    // normalizeColor, then a color: text that is not a color is "no color".
    private static ThemeColor? ParseNormalized(string? color) => ColorNormalization.Normalize(color) is { } normalized ? ThemeColor.Parse(normalized) : null;

    private ThemeColor[] BuildTokenColorMap()
    {
        var index = new TokenColorIndex();
        foreach (TokenColorRule rule in TokenColors)
        {
            index.Add(rule.Foreground);
            index.Add(rule.Background);
        }

        return [.. index.AsArray()];
    }
}
