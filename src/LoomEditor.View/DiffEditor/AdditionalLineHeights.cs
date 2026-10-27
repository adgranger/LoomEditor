// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorViewZones/diffEditorViewZones.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// Extra heights of the lines of one editor: the whitespaces of other owners and the extra rows of wrapped lines (Monaco:
/// <c>getAdditionalLineHeights</c> and <c>joinCombine</c> of <c>utils.ts</c>).
/// </summary>
/// <remarks>
/// <para>Deliberate difference: free of editors; the caller passes the whitespaces, the view to model line mapping and the wrapped line counts.</para>
/// <para>Threading: pure; stateless.</para>
/// </remarks>
internal static class AdditionalLineHeights
{
    /// <summary>Computes the extra heights, sorted by model line number.</summary>
    /// <param name="whitespaces">The whitespaces of the editor, sorted by line, as (id, view line they come after, height).</param>
    /// <param name="ignoredIds">The ids of the whitespaces to skip (the diff editor's own zones).</param>
    /// <param name="viewToModelLine">Maps a view line number (at least 1) to its model line number.</param>
    /// <param name="modelLineCount">The number of model lines.</param>
    /// <param name="viewLineCount">The number of view lines of a model line, or <c>null</c> when the editor does not wrap.</param>
    /// <param name="lineHeight">The height of a line.</param>
    public static IReadOnlyList<AdditionalLineHeight> Compute(
        IEnumerable<(string Id, int AfterLineNumber, int Height)> whitespaces,
        IReadOnlySet<string> ignoredIds,
        Func<int, int> viewToModelLine,
        int modelLineCount,
        Func<int, int>? viewLineCount,
        int lineHeight)
    {
        var zoneHeights = new List<AdditionalLineHeight>();
        var wrappingHeights = new List<AdditionalLineHeight>();

        if (viewLineCount is not null)
        {
            for (int i = 1; i <= modelLineCount; i++)
            {
                int count = viewLineCount(i);
                if (count > 1)
                {
                    wrappingHeights.Add(new AdditionalLineHeight(i, lineHeight * (count - 1)));
                }
            }
        }

        foreach ((string id, int afterLineNumber, int height) in whitespaces)
        {
            if (ignoredIds.Contains(id))
            {
                continue;
            }

            int modelLine = afterLineNumber == 0 ? 0 : viewToModelLine(afterLineNumber);
            zoneHeights.Add(new AdditionalLineHeight(modelLine, height));
        }

        return JoinCombine(
            zoneHeights, wrappingHeights, v => v.LineNumber, (a, b) => new AdditionalLineHeight(a.LineNumber, a.HeightInPx + b.HeightInPx));
    }

    /// <summary>Merges two lists sorted by key; items with equal keys (one of each list) are combined (Monaco: <c>joinCombine</c>).</summary>
    public static IReadOnlyList<T> JoinCombine<T>(IReadOnlyList<T> first, IReadOnlyList<T> second, Func<T, int> key, Func<T, T, T> combine)
    {
        if (first.Count == 0)
        {
            return second;
        }

        if (second.Count == 0)
        {
            return first;
        }

        var result = new List<T>(first.Count + second.Count);
        int i = 0;
        int j = 0;
        while (i < first.Count && j < second.Count)
        {
            int key1 = key(first[i]);
            int key2 = key(second[j]);
            if (key1 < key2)
            {
                result.Add(first[i++]);
            }
            else if (key1 > key2)
            {
                result.Add(second[j++]);
            }
            else
            {
                result.Add(combine(first[i++], second[j++]));
            }
        }

        while (i < first.Count)
        {
            result.Add(first[i++]);
        }

        while (j < second.Count)
        {
            result.Add(second[j++]);
        }

        return result;
    }
}
