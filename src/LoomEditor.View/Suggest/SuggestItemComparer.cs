// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggest.ts (defaultComparator)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>Orders items by sort text, then label, then kind. Ordinal comparisons, like JavaScript's UTF-16 code unit comparison.</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal sealed class SuggestItemComparer : IComparer<SuggestItem>
{
    /// <summary>The shared instance (Monaco: <c>defaultComparator</c>).</summary>
    public static readonly SuggestItemComparer Default = new();

    /// <inheritdoc />
    public int Compare(SuggestItem? a, SuggestItem? b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.SortTextLow is not null && b.SortTextLow is not null)
        {
            if (a.SortTextLow != b.SortTextLow)
            {
                return string.CompareOrdinal(a.SortTextLow, b.SortTextLow);
            }
        }

        if (a.TextLabel != b.TextLabel)
        {
            return string.CompareOrdinal(a.TextLabel, b.TextLabel);
        }

        return (int)a.Completion.Kind - (int)b.Completion.Kind;
    }
}
