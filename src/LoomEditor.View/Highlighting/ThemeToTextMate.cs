// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts (_updateTheme, toColorMap)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Theme;
using LoomEditor.Themes;

namespace LoomEditor.View.Highlighting;

/// <summary>
/// Turns an <see cref="EditorTheme"/> into what the tokenizers need: the TextMate theme (<c>{ name, settings: tokenColors }</c>),
/// its color map as text, and Core's color map as ARGB integers (<c>toColorMap</c>).
/// </summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class ThemeToTextMate
{
    /// <summary>The TextMate theme: one setting per token color rule; rule 0 has no scope (the default colors).</summary>
    public static RawTheme ToRawTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return new RawTheme(
            [.. theme.TokenColors.Select(rule => new RawThemeSetting(
                null,
                null,
                rule.Scopes.Count == 0 ? null : rule.Scopes,
                new RawThemeSettings(rule.FontStyle, ToText(rule.Foreground), ToText(rule.Background))))],
            theme.Name);
    }

    /// <summary>The color map for vscode-textmate: the same text as the rules use; index 0 is unused.</summary>
    public static IReadOnlyList<string> ToTextColorMap(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return [.. theme.TokenColorMap.Select((color, i) => i == 0 ? string.Empty : ColorNormalization.Normalize(color))];
    }

    /// <summary>The color map of the text model: <c>0xAARRGGBB</c>, index 0 unused.</summary>
    public static IReadOnlyList<uint> ToColorMap(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return [.. theme.TokenColorMap.Select((c, i) => i == 0 ? 0u : ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B)];
    }

    private static string? ToText(ThemeColor? color) => color is { } c ? ColorNormalization.Normalize(c) : null;
}
