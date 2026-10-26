// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestMemory.ts (Memory.select)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>
/// The item selected when the list opens: the first preselected item among the best matches, else the first item (Monaco: <c>Memory.select</c>).
/// Not ported: the recency and prefix memories (<c>editor.suggestSelection</c> is always <c>first</c>).
/// </summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class SuggestSelector
{
    /// <summary>The index of the initially selected item; 0 when <paramref name="items"/> is empty.</summary>
    public static int SelectInitialIndex(IReadOnlyList<SuggestItem> items)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        int topScore = items[0].Score[0];
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Score[0] != topScore)
            {
                // we only take items that have the same (top) score
                break;
            }

            if (items[i].Completion.Preselect)
            {
                return i;
            }
        }

        return 0;
    }
}
