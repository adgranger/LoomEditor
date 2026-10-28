// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/features/overviewRulerFeature.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.DiffEditor;

/// <summary>The zones and the colors of the two rulers of the diff overview (Monaco: <c>OverviewRulerFeature</c>).</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class DiffOverviewZones
{
    // Monaco: defaultInsertColor and defaultRemoveColor (rgba(155, 185, 85, 0.2) and rgba(255, 0, 0, 0.2)), before transparent(2).
    private static readonly ThemeColor DefaultInsert = new(51, 155, 185, 85);
    private static readonly ThemeColor DefaultRemove = new(51, 255, 0, 0);

    /// <summary>Monaco: <c>createZones</c>; ranges without lines are skipped, positions are view lines of the editor.</summary>
    /// <param name="ranges">The model line ranges of one side.</param>
    /// <param name="modelToViewLine">The view line of the start of a model line.</param>
    /// <param name="color">The color of the zones.</param>
    public static IReadOnlyList<OverviewRulerZone> Create(IReadOnlyList<LineRange> ranges, Func<int, int> modelToViewLine, ThemeColor color)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(modelToViewLine);

        List<OverviewRulerZone> zones = [];
        foreach (LineRange range in ranges)
        {
            if (range.Length <= 0)
            {
                continue;
            }

            int start = modelToViewLine(range.StartLineNumber);
            int end = modelToViewLine(range.EndLineNumberExclusive);

            // Monaco: by computing the line count, the view model is not asked for the bottom vertical position later.
            zones.Add(new OverviewRulerZone(start, end, end - start, color));
        }

        return zones;
    }

    /// <summary>The colors of the inserted and removed zones of a theme.</summary>
    public static (ThemeColor Insert, ThemeColor Remove) GetColors(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return GetColors(theme.GetColor);
    }

    /// <summary>
    /// <c>diffEditorOverview.*Foreground</c>, else <c>diffEditor.*TextBackground</c>, else the default; the last one is made twice as
    /// opaque (Monaco: <c>transparent(2)</c>, the alpha is capped at 1).
    /// </summary>
    internal static (ThemeColor Insert, ThemeColor Remove) GetColors(Func<string, ThemeColor?> getColor) =>
        (
            getColor("diffEditorOverview.insertedForeground") ?? Doubled(getColor("diffEditor.insertedTextBackground") ?? DefaultInsert),
            getColor("diffEditorOverview.removedForeground") ?? Doubled(getColor("diffEditor.removedTextBackground") ?? DefaultRemove));

    private static ThemeColor Doubled(ThemeColor color) => color with { A = (byte)Math.Min(255, color.A * 2) };
}
