// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Runtime.CompilerServices;
using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>
/// The effective colors of the indent guides and of the bracket pair guides (Monaco: the theming participant, 254–345).
/// A bracket guide falls back to its bracket color (30 % for an inactive guide, 100 % for the active one); the levels
/// that miss a color are dropped and the remaining ones are cycled by level, like the generated CSS rules.
/// </summary>
/// <remarks>
/// Behavior difference: colors are computed on demand instead of generating CSS rules. Threading: safe from any thread.
/// </remarks>
internal static class GuideColors
{
    private const int LevelCount = 6;

    private static readonly ConditionalWeakTable<EditorTheme, (ThemeColor Color, ThemeColor Active)[]> IndentCache = new();
    private static readonly ConditionalWeakTable<EditorTheme, (ThemeColor Color, ThemeColor Active)[]> BracketCache = new();

    /// <summary>The color of the indent guide of a level, or <see langword="null"/> when the theme has no visible color.</summary>
    public static ThemeColor? GetIndentGuideColor(EditorTheme theme, int level, bool active) =>
        Pick(IndentCache.GetValue(theme, ComputeIndent), level, active);

    /// <summary>The color of the bracket pair guide of a level, or <see langword="null"/> when the theme has no visible color.</summary>
    public static ThemeColor? GetBracketGuideColor(EditorTheme theme, int level, bool active) =>
        Pick(BracketCache.GetValue(theme, ComputeBracket), level, active);

    private static ThemeColor? Pick((ThemeColor Color, ThemeColor Active)[] colors, int level, bool active)
    {
        if (colors.Length == 0)
        {
            return null;
        }

        (ThemeColor color, ThemeColor activeColor) = colors[level % colors.Length];
        return active ? activeColor : color;
    }

    private static (ThemeColor Color, ThemeColor Active)[] ComputeBracket(EditorTheme theme)
    {
        List<(ThemeColor, ThemeColor)> result = [];
        for (int i = 1; i <= LevelCount; i++)
        {
            ThemeColor? bracket = theme.GetColor($"editorBracketHighlight.foreground{i}");
            ThemeColor? guide = TransparentToNull(TransparentToNull(theme.GetColor($"editorBracketPairGuide.background{i}")) ?? bracket?.WithAlphaFactor(0.3));
            ThemeColor? active = TransparentToNull(TransparentToNull(theme.GetColor($"editorBracketPairGuide.activeBackground{i}")) ?? bracket);
            if (guide is { } g && active is { } a)
            {
                result.Add((g, a));
            }
        }

        return [.. result];
    }

    private static (ThemeColor Color, ThemeColor Active)[] ComputeIndent(EditorTheme theme)
    {
        List<(ThemeColor, ThemeColor)> result = [];
        for (int i = 1; i <= LevelCount; i++)
        {
            ThemeColor? color = TransparentToNull(theme.GetColor($"editorIndentGuide.background{i}"));
            ThemeColor? active = TransparentToNull(theme.GetColor($"editorIndentGuide.activeBackground{i}"));
            if (color is { } c && active is { } a)
            {
                result.Add((c, a));
            }
        }

        return [.. result];
    }

    private static ThemeColor? TransparentToNull(ThemeColor? color) => color is { A: 0 } ? null : color;
}
