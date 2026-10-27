// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.View.Layout;

namespace LoomEditor.View.DiffEditor;

/// <summary>A zone to add to the modified editor of an inline diff.</summary>
/// <param name="Kind">What the renderer paints.</param>
/// <param name="AfterLineNumber">The modified line the zone comes after.</param>
/// <param name="HeightInPx">The height of the zone.</param>
/// <param name="Data">The <see cref="ViewZone.Data"/> of the zone.</param>
/// <param name="SuppressMouseDown">Whether a mouse down on the zone must not move the cursor.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record ZoneSpec(ViewZoneKind Kind, int AfterLineNumber, int HeightInPx, object? Data, bool SuppressMouseDown);

/// <summary>Turns aligned blocks into the zones of the inline view (Monaco: the inline branch of <c>DiffEditorViewZones</c>).</summary>
/// <remarks>
/// <para>
/// Behavior differences: one view line per model line (word wrap is off in phase 9, so Monaco's <c>viewLineCounts</c> is not needed);
/// the deleted lines zone suppresses mouse down, where Monaco lets the user select the deleted text. Not ported: the zones of the
/// hidden original side, moved text padding.
/// </para>
/// <para>Threading: pure; stateless.</para>
/// </remarks>
internal static class InlineViewZones
{
    /// <summary>Computes the zones of the modified editor.</summary>
    public static IReadOnlyList<ZoneSpec> Compute(
        IReadOnlyList<LineRangeAlignment> alignments, int lineHeight, bool renderIndicators, Func<int, ViewLineData> getOriginalLineData)
    {
        var result = new List<ZoneSpec>();
        foreach (LineRangeAlignment a in alignments)
        {
            if (a.Diff is not null)
            {
                if (!a.OriginalRange.IsEmpty)
                {
                    var data = new DeletedLinesZoneData(a.OriginalRange, getOriginalLineData, DeletedRanges(a), renderIndicators);
                    result.Add(new ZoneSpec(
                        ViewZoneKind.DeletedLines, a.ModifiedRange.StartLineNumber - 1, a.OriginalRange.Length * lineHeight, data, true));
                }

                continue;
            }

            int delta = a.OriginalHeightInPx - a.ModifiedHeightInPx;
            if (delta < 0)
            {
                result.Add(new ZoneSpec(ViewZoneKind.Empty, a.ModifiedRange.EndLineNumberExclusive - 1, -delta, null, true));
            }
        }

        return result;
    }

    // The inner changes of the block, cut to the lines of the block (an inner range may end at column 1 of the next line).
    private static List<Range> DeletedRanges(LineRangeAlignment a)
    {
        var ranges = new List<Range>();
        if (a.Diff is DetailedLineRangeMapping { InnerChanges: { } innerChanges })
        {
            Range block = a.OriginalRange.ToExclusiveRange();
            foreach (RangeMapping change in innerChanges)
            {
                if (change.OriginalRange.Intersect(block) is { IsEmpty: false } clipped)
                {
                    ranges.Add(clipped);
                }
            }
        }

        return ranges;
    }
}
