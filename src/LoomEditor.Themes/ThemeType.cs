// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/colorThemeData.ts (themeTypeSelector, type)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>The kind of a color theme: it selects the default value of the colors the theme does not set (VS Code: <c>ColorScheme</c>).</summary>
public enum ThemeType
{
    /// <summary>A dark theme (<c>vs-dark</c>, also the fallback).</summary>
    Dark,

    /// <summary>A light theme (<c>vs</c>).</summary>
    Light,

    /// <summary>A high-contrast dark theme (<c>hc-black</c>).</summary>
    HighContrastDark,

    /// <summary>A high-contrast light theme (<c>hc-light</c>).</summary>
    HighContrastLight,
}

/// <summary>Conversions of <see cref="ThemeType"/>.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class ThemeTypes
{
    /// <summary>
    /// The type of a theme from its <c>uiTheme</c> (VS Code: <c>themeTypeSelector</c> then <c>type</c>): <c>vs</c> is
    /// light, <c>hc-black</c> high-contrast dark, <c>hc-light</c> high-contrast light, anything else dark. Only the
    /// first word counts (VS Code ids hold class names separated by spaces).
    /// </summary>
    public static ThemeType FromUiTheme(string? uiTheme)
    {
        string selector = uiTheme?.Split(' ')[0] ?? string.Empty;
        return selector switch
        {
            "vs" => ThemeType.Light,
            "hc-black" => ThemeType.HighContrastDark,
            "hc-light" => ThemeType.HighContrastLight,
            _ => ThemeType.Dark,
        };
    }
}
