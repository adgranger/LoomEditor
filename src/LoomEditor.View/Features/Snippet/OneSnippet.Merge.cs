// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (OneSnippet.merge, _renormalizePlaceholderIndices)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Snippet;

internal sealed partial class OneSnippet
{
    /// <summary>
    /// Merges nested snippets into the active placeholders: one snippet per active placeholder, in order (Monaco: <c>merge</c>).
    /// </summary>
    /// <remarks>
    /// Behavior difference: Monaco gives the nested placeholders fractional indices (<c>index + nestedIndex / nestingLevel</c>) and
    /// renumbers them at the end. <see cref="Placeholder.Index"/> is an <see cref="int"/> here, so the fractions are kept in a local
    /// table until the renumbering, which gives the same final indices.
    /// </remarks>
    public void Merge(IReadOnlyList<OneSnippet> others)
    {
        ArgumentNullException.ThrowIfNull(others);

        TextModel model = _editor.Model;
        _nestingLevel *= 10;
        var sortKeys = new Dictionary<Placeholder, double>();
        int next = 0;

        _editor.ChangeDecorations(accessor =>
        {
            // For each active placeholder take one snippet and merge it in that placeholder (there can be many for `$1foo$1foo`).
            // Because everything is sorted by editor selection, we can simply take the elements from the beginning.
            foreach (Placeholder placeholder in _placeholderGroups[PlaceholderGroupsIdx])
            {
                OneSnippet nested = others[next++];
                Guard.Assert(nested._offset != -1);
                Guard.Assert(nested._placeholderDecorations is null);

                // Massage the placeholder indices of the nested snippet to be sorted right after the insertion point. This ensures we
                // move through the placeholders in the correct order.
                int indexLastPlaceholder = nested._snippet.PlaceholderInfo.Last?.Index ?? 0;
                foreach (Placeholder nestedPlaceholder in nested._snippet.PlaceholderInfo.All)
                {
                    sortKeys[nestedPlaceholder] = nestedPlaceholder.IsFinalTabstop
                        ? placeholder.Index + ((indexLastPlaceholder + 1d) / _nestingLevel)
                        : placeholder.Index + (nestedPlaceholder.Index / (double)_nestingLevel);
                }

                _snippet.Replace(placeholder, nested._snippet.Children);

                // Remove the placeholder at which position we are inserting the snippet, and also its decoration.
                accessor.RemoveDecoration(_placeholderDecorations![placeholder]);
                _placeholderDecorations.Remove(placeholder);

                // For each *new* placeholder create a decoration to monitor how and if it grows or shrinks.
                foreach (Placeholder nestedPlaceholder in nested._snippet.Placeholders)
                {
                    int placeholderOffset = nested._snippet.Offset(nestedPlaceholder);
                    int placeholderLength = TextmateSnippet.FullLength(nestedPlaceholder);
                    var range = new Range(
                        model.GetPositionAt(nested._offset + placeholderOffset),
                        model.GetPositionAt(nested._offset + placeholderOffset + placeholderLength));
                    _placeholderDecorations[nestedPlaceholder] = accessor.AddDecoration(range, InactiveDecoration);
                }
            }

            // Renormalize the fractional placeholder indices back to small integers. Without this, deeply nested merges lose
            // floating-point precision so distinct placeholders collapse onto the same index and produce phantom cursors (#279349).
            RenormalizePlaceholderIndices(sortKeys);

            // Last, re-create the placeholder groups by sorting the placeholders by their index.
            _placeholderGroups = GroupByIndex(_snippet.Placeholders);
        });
    }

    private void RenormalizePlaceholderIndices(Dictionary<Placeholder, double> sortKeys)
    {
        IReadOnlyList<Placeholder> placeholders = _snippet.Placeholders;
        double? KeyOf(Placeholder placeholder) =>
            sortKeys.TryGetValue(placeholder, out double key) ? key : placeholder.IsFinalTabstop ? null : placeholder.Index;

        var remap = new Dictionary<double, int>();
        int rank = 1;
        foreach (double key in placeholders.Select(KeyOf).OfType<double>().Distinct().Order())
        {
            remap[key] = rank++;
        }

        foreach (Placeholder placeholder in placeholders)
        {
            if (KeyOf(placeholder) is { } key)
            {
                placeholder.Index = remap[key];
            }
        }

        _nestingLevel = 1;
    }
}
