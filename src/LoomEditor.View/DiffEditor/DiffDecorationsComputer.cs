// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// Turns a <see cref="DiffState"/> into the decorations of the original and the modified model (Monaco: the
/// <c>_decorations</c> derived value of <c>DiffEditorDecorations</c>).
/// </summary>
/// <remarks>
/// Behavior differences: no moved-text decorations and no true inline view (<c>before</c> decorations). Ranges beyond the
/// model (a stale diff, ADR 0012 decision 4) are dropped when they start past the last line and clamped to its end otherwise.
/// Threading: pure functions.
/// </remarks>
internal static class DiffDecorationsComputer
{
    /// <summary>Computes the decorations of both models.</summary>
    public static (IReadOnlyList<ModelDeltaDecoration> Original, IReadOnlyList<ModelDeltaDecoration> Modified) Compute(
        DiffState diff, bool renderIndicators, bool showEmptyDecorations, int originalLineCount, int modifiedLineCount)
    {
        List<ModelDeltaDecoration> original = [];
        List<ModelDeltaDecoration> modified = [];

        foreach (var m in diff.Mappings)
        {
            var c = m.LineRangeMapping;
            if (!c.Original.IsEmpty)
            {
                original.Add(new(c.Original.ToInclusiveRange()!.Value, renderIndicators ? DiffDecorationOptions.LineDeleteBackgroundWithIndicator : DiffDecorationOptions.LineDeleteBackground));
            }

            if (!c.Modified.IsEmpty)
            {
                modified.Add(new(c.Modified.ToInclusiveRange()!.Value, renderIndicators ? DiffDecorationOptions.LineAddBackgroundWithIndicator : DiffDecorationOptions.LineAddBackground));
            }

            if (c.Modified.IsEmpty || c.Original.IsEmpty)
            {
                if (!c.Original.IsEmpty)
                {
                    original.Add(new(c.Original.ToInclusiveRange()!.Value, DiffDecorationOptions.WholeLineDeleteDecoration));
                }

                if (!c.Modified.IsEmpty)
                {
                    modified.Add(new(c.Modified.ToInclusiveRange()!.Value, DiffDecorationOptions.WholeLineAddDecoration));
                }
            }
            else
            {
                foreach (var i in c.InnerChanges ?? [])
                {
                    // Don't show empty markers outside the line range
                    if (c.Original.Contains(i.OriginalRange.StartLineNumber))
                    {
                        original.Add(new(i.OriginalRange, i.OriginalRange.IsEmpty && showEmptyDecorations ? DiffDecorationOptions.DeleteDecorationEmpty : DiffDecorationOptions.DeleteDecoration));
                    }

                    if (c.Modified.Contains(i.ModifiedRange.StartLineNumber))
                    {
                        modified.Add(new(i.ModifiedRange, i.ModifiedRange.IsEmpty && showEmptyDecorations ? DiffDecorationOptions.AddDecorationEmpty : DiffDecorationOptions.AddDecoration));
                    }
                }
            }
        }

        return (Clamp(original, originalLineCount), Clamp(modified, modifiedLineCount));
    }

    /// <summary>Drops the decorations that start past <paramref name="lineCount"/> and clamps the end of the others to the last line.</summary>
    public static IReadOnlyList<ModelDeltaDecoration> Clamp(IReadOnlyList<ModelDeltaDecoration> decorations, int lineCount)
    {
        List<ModelDeltaDecoration> result = new(decorations.Count);
        foreach (var d in decorations)
        {
            if (d.Range.StartLineNumber > lineCount)
            {
                continue;
            }

            result.Add(d.Range.EndLineNumber > lineCount
                ? d with { Range = new Range(d.Range.StartLineNumber, d.Range.StartColumn, lineCount, int.MaxValue) }
                : d);
        }

        return result;
    }
}
