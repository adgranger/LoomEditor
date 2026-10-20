// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/colorizedBracketPairsDecorationProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Runtime.CompilerServices;
using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>
/// The colors of the bracket pair colorization (Monaco: the theming participant, 106–128): the six
/// <c>editorBracketHighlight.foregroundN</c> colors minus the missing and fully transparent ones, cycled by nesting level.
/// </summary>
/// <remarks>
/// Behavior difference: a computed <c>DecorationStyles</c> row instead of generated CSS rules. Threading: safe from any thread.
/// </remarks>
internal static class BracketPairColorizationColors
{
    private static readonly string[] ColorIds =
    [
        "editorBracketHighlight.foreground1",
        "editorBracketHighlight.foreground2",
        "editorBracketHighlight.foreground3",
        "editorBracketHighlight.foreground4",
        "editorBracketHighlight.foreground5",
        "editorBracketHighlight.foreground6",
    ];

    private static readonly ConditionalWeakTable<EditorTheme, ThemeColor[]> Cache = new();

    /// <summary>The color of the brackets of a nesting level, or <see langword="null"/> when the theme has no visible color.</summary>
    public static ThemeColor? GetLevelColor(EditorTheme theme, int level)
    {
        ThemeColor[] colors = Cache.GetValue(theme, static t =>
            [.. ColorIds.Select(id => t.GetColor(id)).OfType<ThemeColor>().Where(c => c.A != 0)]);
        return colors.Length == 0 ? null : colors[level % colors.Length];
    }

    /// <summary>The color of an unexpected closing bracket.</summary>
    public static ThemeColor? GetUnexpectedBracketColor(EditorTheme theme) =>
        theme.GetColor("editorBracketHighlight.unexpectedBracket.foreground");
}
