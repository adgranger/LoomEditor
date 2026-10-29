// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (OneSnippet.move, _hasPlaceholderBeenCollapsed)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Snippet;

internal sealed partial class OneSnippet
{
    /// <summary>
    /// Moves to the next (<paramref name="forward"/> <see langword="true"/>) or previous (<see langword="false"/>) tab stop, or restores
    /// the active one (<see langword="null"/>), and returns the selections of the placeholders that became active.
    /// </summary>
    public List<Selection> Move(bool? forward)
    {
        InitDecorations();

        TextModel model = _editor.Model;

        // Transform the placeholder text if necessary.
        if (PlaceholderGroupsIdx >= 0)
        {
            var operations = new List<IdentifiedEditOperation>();
            foreach (Placeholder placeholder in _placeholderGroups[PlaceholderGroupsIdx])
            {
                // Check if the placeholder has a transformation.
                if (placeholder.Transform is { } transform
                    && _placeholderDecorations!.TryGetValue(placeholder, out string? id)
                    && model.GetDecorationRange(id) is { } range)
                {
                    string currentValue = model.GetValueInRange(range);
                    string[] transformedValueLines = LineBreakRegex().Split(transform.Resolve(currentValue));
                    // Fix the indentation of the transformed lines.
                    for (int i = 1; i < transformedValueLines.Length; i++)
                    {
                        transformedValueLines[i] = model.NormalizeIndentation(_snippetLineLeadingWhitespace + transformedValueLines[i]);
                    }

                    operations.Add(new IdentifiedEditOperation(range, string.Join(model.Eol, transformedValueLines)));
                }
            }

            if (operations.Count > 0)
            {
                _editor.ViewModel.ExecuteEdits("snippet.placeholderTransform", operations, _ => null);
            }
        }

        bool couldSkipThisPlaceholder = false;
        if (forward == true && PlaceholderGroupsIdx < _placeholderGroups.Count - 1)
        {
            PlaceholderGroupsIdx += 1;
            couldSkipThisPlaceholder = true;
        }
        else if (forward == false && PlaceholderGroupsIdx > 0)
        {
            PlaceholderGroupsIdx -= 1;
            couldSkipThisPlaceholder = true;
        }

        // Otherwise the selection of the current placeholder might not be accurate any more: simply restore it.
        List<Selection> newSelections = _editor.ChangeDecorations(accessor =>
        {
            var activePlaceholders = new HashSet<Placeholder>();

            // Change the stickiness to always grow when typing at the edges, because these decorations represent the currently
            // active tabstop. Special case #1: reaching the final tabstop. Special case #2: placeholders enclosing active placeholders.
            var selections = new List<Selection>();
            foreach (Placeholder placeholder in _placeholderGroups[PlaceholderGroupsIdx])
            {
                bool hasDecoration = _placeholderDecorations!.TryGetValue(placeholder, out string? id);
                Range? range = hasDecoration ? model.GetDecorationRange(id!) : null;

                // Consider skipping this placeholder index when the decoration range is empty but the placeholder was not: that is a
                // strong hint that the placeholder has been deleted (all the placeholders must match this).
                couldSkipThisPlaceholder = couldSkipThisPlaceholder && HasPlaceholderBeenCollapsed(placeholder);

                if (!hasDecoration || range is not { } placeholderRange)
                {
                    continue;
                }

                selections.Add(new Selection(placeholderRange.StartLineNumber, placeholderRange.StartColumn, placeholderRange.EndLineNumber, placeholderRange.EndColumn));

                accessor.ChangeDecorationOptions(id!, placeholder.IsFinalTabstop ? ActiveFinalDecoration : ActiveDecoration);
                activePlaceholders.Add(placeholder);

                foreach (Placeholder enclosingPlaceholder in TextmateSnippet.EnclosingPlaceholders(placeholder))
                {
                    if (_placeholderDecorations.TryGetValue(enclosingPlaceholder, out string? enclosingId))
                    {
                        accessor.ChangeDecorationOptions(enclosingId, enclosingPlaceholder.IsFinalTabstop ? ActiveFinalDecoration : ActiveDecoration);
                        activePlaceholders.Add(enclosingPlaceholder);
                    }
                }
            }

            // Change the stickiness to never grow when typing at the edges, so that inactive tabstops never grow.
            foreach ((Placeholder placeholder, string id) in _placeholderDecorations!)
            {
                if (!activePlaceholders.Contains(placeholder))
                {
                    accessor.ChangeDecorationOptions(id, placeholder.IsFinalTabstop ? InactiveFinalDecoration : InactiveDecoration);
                }
            }

            return selections;
        });

        return couldSkipThisPlaceholder ? Move(forward) : newSelections;
    }

    // A placeholder is empty when it was not empty when authored but its tracking decoration is empty. This also applies to all the
    // potential parent placeholders.
    private bool HasPlaceholderBeenCollapsed(Placeholder placeholder)
    {
        SnippetMarker? marker = placeholder;
        while (marker is not null)
        {
            if (marker is Placeholder current)
            {
                Range? range = _placeholderDecorations!.TryGetValue(current, out string? id) ? _editor.Model.GetDecorationRange(id) : null;
                if ((range is null || range.Value.IsEmpty) && current.ToString().Length > 0)
                {
                    return true;
                }
            }

            marker = marker.Parent;
        }

        return false;
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreakRegex();
}
