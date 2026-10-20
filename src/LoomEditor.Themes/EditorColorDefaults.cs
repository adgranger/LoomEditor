// Ported from microsoft/vscode: src/vs/platform/theme/common/colorUtils.ts (resolveDefaultColor, resolveColorValue)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>
/// The default value of every editor color that <see cref="EditorTheme"/> exposes, and the colors they refer to (VS
/// Code: the <c>registerColor</c> calls of <c>editorColors.ts</c>, <c>editorColorRegistry.ts</c> and
/// <c>baseColors.ts</c>, as a static table instead of a registry). A default is <see langword="null"/> (no color), a
/// <c>#hex</c> color, or the id of another color; other transforms of <c>colorUtils.ts</c> (<c>transparent</c>, …)
/// are not needed by these ids.
/// </summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class EditorColorDefaults
{
    // id -> (dark, light, hcDark, hcLight). One line per registerColor, with the upstream line in a comment.
    // Color.black / Color.white are #000000 / #FFFFFF.
    private static readonly Dictionary<string, (string? Dark, string? Light, string? HcDark, string? HcLight)> Table = new(StringComparer.Ordinal)
    {
        // editorColors.ts:19
        ["editor.background"] = ("#1E1E1E", "#ffffff", "#000000", "#FFFFFF"),

        // editorColors.ts:23
        ["editor.foreground"] = ("#BBBBBB", "#333333", "#FFFFFF", "foreground"),

        // editorColors.ts:121
        ["editor.selectionBackground"] = ("#264F78", "#ADD6FF", "#f3f518", "#0F4A85"),

        // editorColors.ts:141
        ["editor.compositionBorder"] = ("#ffffff", "#000000", "#ffffff", "#000000"),

        // editorColorRegistry.ts:14 (registered with a null default: the same for all theme types)
        ["editor.lineHighlightBackground"] = (null, null, null, null),

        // editorColorRegistry.ts:16
        ["editor.lineHighlightBorder"] = ("#282828", "#eeeeee", "#f38518", "contrastBorder"),

        // editorColorRegistry.ts:22
        ["editorCursor.foreground"] = ("#AEAFAD", "#000000", "#FFFFFF", "#0F4A85"),

        // editorColorRegistry.ts:30
        ["editorLineNumber.foreground"] = ("#858585", "#237893", "#FFFFFF", "#292929"),

        // editorColorRegistry.ts:49 (deprecated id)
        ["editorActiveLineNumber.foreground"] = ("#c6c6c6", "#0B216F", "contrastActiveBorder", "contrastActiveBorder"),

        // editorColorRegistry.ts:50 (the default is the deprecated color, for every theme type)
        ["editorLineNumber.activeForeground"] = ("editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground"),

        // baseColors.ts:13
        ["foreground"] = ("#CCCCCC", "#616161", "#FFFFFF", "#292929"),

        // baseColors.ts:37
        ["focusBorder"] = ("#007FD4", "#0090F1", "#F38518", "#006BBD"),

        // baseColors.ts:41
        ["contrastBorder"] = (null, null, "#6FC3DF", "#0F4A85"),

        // baseColors.ts:45
        ["contrastActiveBorder"] = (null, null, "focusBorder", "focusBorder"),
    };

    /// <summary>Whether <paramref name="id"/> has a default (even a <see langword="null"/> one).</summary>
    public static bool IsKnown(string id) => Table.ContainsKey(id);

    /// <summary>
    /// <c>theme.getColor(id, useDefault: true)</c>: the color the theme sets for <paramref name="id"/>, else its default
    /// for <paramref name="type"/>. A default that names another color is resolved the same way, so a theme value of
    /// the referenced color wins over its default (<c>resolveColorValue</c>).
    /// </summary>
    /// <param name="id">A color id, for example <c>editor.background</c>.</param>
    /// <param name="type">The type of the theme.</param>
    /// <param name="themeColors">The colors the theme sets: <see langword="null"/> when it does not set an id.</param>
    /// <returns>The color, or <see langword="null"/> when there is none (unknown id, <see langword="null"/> default).</returns>
    public static ThemeColor? Resolve(string id, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(themeColors);
        return themeColors(id) ?? ResolveDefault(id, type, themeColors);
    }

    // resolveDefaultColor
    private static ThemeColor? ResolveDefault(string id, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        if (!Table.TryGetValue(id, out (string? Dark, string? Light, string? HcDark, string? HcLight) defaults))
        {
            return null;
        }

        string? colorValue = type switch
        {
            ThemeType.Light => defaults.Light,
            ThemeType.HighContrastDark => defaults.HcDark,
            ThemeType.HighContrastLight => defaults.HcLight,
            _ => defaults.Dark,
        };

        // resolveColorValue
        if (colorValue is null)
        {
            return null;
        }

        return colorValue[0] == '#' ? ThemeColor.Parse(colorValue) : Resolve(colorValue, type, themeColors);
    }
}
