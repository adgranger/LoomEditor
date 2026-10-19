// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary><c>ParsedThemeRule</c>: one scope of one theme setting. Font attributes are not ported.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class ParsedThemeRule
{
    /// <summary>Creates a rule.</summary>
    public ParsedThemeRule(string scope, IReadOnlyList<string>? parentScopes, int index, FontStyle fontStyle, string? foreground, string? background)
    {
        Scope = scope;
        ParentScopes = parentScopes;
        Index = index;
        FontStyle = fontStyle;
        Foreground = foreground;
        Background = background;
    }

    /// <summary><c>scope</c>: the scope the rule styles (the last segment of the scope path).</summary>
    public string Scope { get; }

    /// <summary><c>parentScopes</c>: the other segments, deepest first, or <see langword="null"/>.</summary>
    public IReadOnlyList<string>? ParentScopes { get; }

    /// <summary><c>index</c>: the index of the setting in the theme.</summary>
    public int Index { get; }

    /// <summary><c>fontStyle</c>.</summary>
    public FontStyle FontStyle { get; }

    /// <summary><c>foreground</c>: a valid hex color, or <see langword="null"/>.</summary>
    public string? Foreground { get; }

    /// <summary><c>background</c>: a valid hex color, or <see langword="null"/>.</summary>
    public string? Background { get; }
}
