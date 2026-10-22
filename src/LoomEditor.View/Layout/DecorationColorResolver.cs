// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts (getColor)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Runtime.CompilerServices;
using LoomEditor.Core.Decorations;
using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>
/// Resolves the color of a decoration against a theme (Monaco: <c>ModelDecorationOverviewRulerOptions.getColor</c> and <c>ModelDecorationMinimapOptions.getColor</c>, the same rule).
/// </summary>
/// <remarks>
/// Behavior difference: Monaco caches the resolved color on the options and invalidates it on a theme change; here the
/// cache is per theme, so there is nothing to invalidate. Threading: safe from any thread.
/// </remarks>
internal static class DecorationColorResolver
{
    private static readonly ConditionalWeakTable<EditorTheme, Dictionary<DecorationColor, ThemeColor?>> Cache = new();

    /// <summary>The color to draw, or <see langword="null"/> when there is none (Monaco: <c>''</c>).</summary>
    public static ThemeColor? Resolve(DecorationColor? color, DecorationColor? darkColor, EditorTheme theme)
    {
        bool isDark = theme.Type is ThemeType.Dark or ThemeType.HighContrastDark;
        DecorationColor? chosen = isDark && darkColor is not null ? darkColor : color;
        if (chosen is null)
        {
            return null;
        }

        Dictionary<DecorationColor, ThemeColor?> cache = Cache.GetValue(theme, _ => []);
        lock (cache)
        {
            if (!cache.TryGetValue(chosen, out ThemeColor? resolved))
            {
                resolved = chosen.ThemeColorId is { } id ? theme.GetColor(id) : ThemeColor.Parse(chosen.Value!);
                cache[chosen] = resolved;
            }

            return resolved;
        }
    }
}
