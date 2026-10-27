// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.View.DiffEditor;

/// <summary>An original and a modified line range that must start and end level, with the height each side needs (Monaco: <c>ILineRangeAlignment</c>).</summary>
/// <param name="OriginalRange">The range in the original model.</param>
/// <param name="ModifiedRange">The range in the modified model.</param>
/// <param name="OriginalHeightInPx">The height of the original side, including extra space.</param>
/// <param name="ModifiedHeightInPx">The height of the modified side, including extra space.</param>
/// <param name="Diff">The diff this alignment results from, or <c>null</c> for alignments outside of diffs.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LineRangeAlignment(
    LineRange OriginalRange,
    LineRange ModifiedRange,
    int OriginalHeightInPx,
    int ModifiedHeightInPx,
    LineRangeMapping? Diff);

/// <summary>This model line has <paramref name="HeightInPx"/> extra pixels on top of one line height (Monaco: <c>AdditionalLineHeightInfo</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct AdditionalLineHeight(int LineNumber, int HeightInPx);

/// <summary>Computes the aligned blocks of a diff (Monaco: <c>computeRangeAlignment</c>).</summary>
/// <remarks>
/// <para>Deliberate difference: free of editors and models; the caller passes the extra heights and the original line count / max column.</para>
/// <para>Not ported: <c>getAdditionalLineHeights</c> (built from the editors by the side-by-side view), <c>allowsTrueInlineDiffRendering</c>.</para>
/// <para>Threading: pure; stateless.</para>
/// </remarks>
internal static class LineRangeAlignments
{
    /// <summary>Monaco: <c>rangeIsSingleLine</c>.</summary>
    public static bool RangeIsSingleLine(Range range) => range.StartLineNumber == range.EndLineNumber;

    /// <summary>Computes the alignments; <paramref name="originalExtra"/> and <paramref name="modifiedExtra"/> are sorted by line number.</summary>
    public static IReadOnlyList<LineRangeAlignment> Compute(
        IReadOnlyList<DiffMapping> diffs,
        IReadOnlyList<AdditionalLineHeight> originalExtra,
        IReadOnlyList<AdditionalLineHeight> modifiedExtra,
        int originalLineHeight,
        int modifiedLineHeight,
        Func<int, int> originalLineMaxColumn,
        int originalLineCount,
        bool innerHunkAlignment)
    {
        var originalOverrides = new ArrayQueue<AdditionalLineHeight>(originalExtra);
        var modifiedOverrides = new ArrayQueue<AdditionalLineHeight>(modifiedExtra);
        var result = new List<LineRangeAlignment>();

        int lastOriginalLineNumber = 0;
        int lastModifiedLineNumber = 0;

        void HandleAlignmentsOutsideOfDiffs(int untilOriginalLineNumberExclusive, int untilModifiedLineNumberExclusive)
        {
            while (true)
            {
                AdditionalLineHeight? origNext = originalOverrides.TryPeek(out AdditionalLineHeight o) ? o : null;
                AdditionalLineHeight? modNext = modifiedOverrides.TryPeek(out AdditionalLineHeight m) ? m : null;
                if (origNext is { } oa && oa.LineNumber >= untilOriginalLineNumberExclusive)
                {
                    origNext = null;
                }

                if (modNext is { } ma && ma.LineNumber >= untilModifiedLineNumberExclusive)
                {
                    modNext = null;
                }

                if (origNext is null && modNext is null)
                {
                    break;
                }

                long distOrig = origNext is { } on ? (long)on.LineNumber - lastOriginalLineNumber : int.MaxValue;
                long distNext = modNext is { } mn ? (long)mn.LineNumber - lastModifiedLineNumber : int.MaxValue;

                if (distOrig < distNext)
                {
                    originalOverrides.Dequeue();
                    modNext = new AdditionalLineHeight(origNext!.Value.LineNumber - lastOriginalLineNumber + lastModifiedLineNumber, 0);
                }
                else if (distOrig > distNext)
                {
                    modifiedOverrides.Dequeue();
                    origNext = new AdditionalLineHeight(modNext!.Value.LineNumber - lastModifiedLineNumber + lastOriginalLineNumber, 0);
                }
                else
                {
                    originalOverrides.Dequeue();
                    modifiedOverrides.Dequeue();
                }

                result.Add(new LineRangeAlignment(
                    LineRange.OfLength(origNext!.Value.LineNumber, 1),
                    LineRange.OfLength(modNext!.Value.LineNumber, 1),
                    originalLineHeight + origNext.Value.HeightInPx,
                    modifiedLineHeight + modNext.Value.HeightInPx,
                    null));
            }
        }

        foreach (DiffMapping m in diffs)
        {
            DetailedLineRangeMapping c = m.LineRangeMapping;
            HandleAlignmentsOutsideOfDiffs(c.Original.StartLineNumber, c.Modified.StartLineNumber);

            bool first = true;
            int lastModLineNumber = c.Modified.StartLineNumber;
            int lastOrigLineNumber = c.Original.StartLineNumber;

            void EmitAlignment(int origLineNumberExclusive, int modLineNumberExclusive, bool forceAlignment = false)
            {
                if (origLineNumberExclusive < lastOrigLineNumber || modLineNumberExclusive < lastModLineNumber)
                {
                    return;
                }

                if (first)
                {
                    first = false;
                }
                else if (!forceAlignment && (origLineNumberExclusive == lastOrigLineNumber || modLineNumberExclusive == lastModLineNumber))
                {
                    // This causes a re-alignment of an already aligned line. However, we don't care for the final alignment.
                    return;
                }

                var originalRange = new LineRange(lastOrigLineNumber, origLineNumberExclusive);
                var modifiedRange = new LineRange(lastModLineNumber, modLineNumberExclusive);
                if (originalRange.IsEmpty && modifiedRange.IsEmpty)
                {
                    return;
                }

                int originalAdditionalHeight = originalOverrides.TakeWhile(v => v.LineNumber < origLineNumberExclusive)?.Sum(v => v.HeightInPx) ?? 0;
                int modifiedAdditionalHeight = modifiedOverrides.TakeWhile(v => v.LineNumber < modLineNumberExclusive)?.Sum(v => v.HeightInPx) ?? 0;

                result.Add(new LineRangeAlignment(
                    originalRange,
                    modifiedRange,
                    originalRange.Length * originalLineHeight + originalAdditionalHeight,
                    modifiedRange.Length * modifiedLineHeight + modifiedAdditionalHeight,
                    c));

                lastOrigLineNumber = origLineNumberExclusive;
                lastModLineNumber = modLineNumberExclusive;
            }

            if (innerHunkAlignment)
            {
                foreach (RangeMapping i in c.InnerChanges ?? [])
                {
                    if (i.OriginalRange.StartColumn > 1 && i.ModifiedRange.StartColumn > 1)
                    {
                        // There is some unmodified text on this line before the diff.
                        EmitAlignment(i.OriginalRange.StartLineNumber, i.ModifiedRange.StartLineNumber);
                    }

                    // When the diff is invalid, the ranges might be out of bounds.
                    int maxColumn = i.OriginalRange.EndLineNumber <= originalLineCount
                        ? originalLineMaxColumn(i.OriginalRange.EndLineNumber)
                        : int.MaxValue;
                    if (i.OriginalRange.EndColumn < maxColumn)
                    {
                        // There is some unmodified text on this line after the diff.
                        EmitAlignment(i.OriginalRange.EndLineNumber, i.ModifiedRange.EndLineNumber);
                    }
                }
            }

            EmitAlignment(c.Original.EndLineNumberExclusive, c.Modified.EndLineNumberExclusive, true);

            lastOriginalLineNumber = c.Original.EndLineNumberExclusive;
            lastModifiedLineNumber = c.Modified.EndLineNumberExclusive;
        }

        HandleAlignmentsOutsideOfDiffs(int.MaxValue, int.MaxValue);
        return result;
    }
}
