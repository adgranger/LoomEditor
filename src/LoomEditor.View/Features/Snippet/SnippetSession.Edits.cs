// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetSession.ts (adjustWhitespace, adjustSelection, createEditsAndSnippetsFromSelections)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Features.Snippet;

internal sealed partial class SnippetSession
{
    /// <summary>
    /// Normalizes the line breaks and the indentation of the text of <paramref name="snippet"/> for an insertion at
    /// <paramref name="position"/> and returns the leading whitespace of that line.
    /// </summary>
    public static string AdjustWhitespace(TextModel model, Position position, bool adjustIndentation, TextmateSnippet snippet, ISet<SnippetMarker>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(snippet);

        string line = model.GetLineContent(position.LineNumber);
        string lineLeadingWhitespace = Strings.GetLeadingWhitespace(line, 0, position.Column - 1);

        // The snippet as inserted.
        string? snippetTextString = null;

        snippet.Walk(marker =>
        {
            // All text elements that are not inside a choice.
            if (marker is not TextMarker text || text.Parent is Choice)
            {
                return true;
            }

            if (filter is not null && !filter.Contains(marker))
            {
                return true;
            }

            string[] lines = LineBreakRegex().Split(text.Value);

            if (adjustIndentation)
            {
                // The snippet start is only normalized; all the following lines are extra-indented and normalized; the text start
                // is extra-indented and normalized when it follows a line break.
                int offset = snippet.Offset(marker);
                if (offset == 0)
                {
                    lines[0] = model.NormalizeIndentation(lines[0]);
                }
                else
                {
                    snippetTextString ??= snippet.ToString();
                    char prevChar = snippetTextString[offset - 1];
                    if (prevChar is '\n' or '\r')
                    {
                        lines[0] = model.NormalizeIndentation(lineLeadingWhitespace + lines[0]);
                    }
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    lines[i] = model.NormalizeIndentation(lineLeadingWhitespace + lines[i]);
                }
            }

            string newValue = string.Join(model.Eol, lines);
            if (newValue != text.Value)
            {
                SnippetMarker.Replace(text, [new TextMarker(newValue)]);
                snippetTextString = null;
            }

            return true;
        });

        return lineLeadingWhitespace;
    }

    /// <summary>
    /// Grows <paramref name="selection"/> around its caret by <paramref name="overwriteBefore"/> and <paramref name="overwriteAfter"/>
    /// characters, within the line.
    /// </summary>
    public static Selection AdjustSelection(TextModel model, Selection selection, int overwriteBefore, int overwriteAfter)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (overwriteBefore != 0 || overwriteAfter != 0)
        {
            // overwrite[Before|After] is computed using the position, not the whole selection.
            Range range = model.ValidateRange(
                selection.PositionLineNumber,
                selection.PositionColumn - overwriteBefore,
                selection.PositionLineNumber,
                selection.PositionColumn + overwriteAfter);
            selection = Selection.FromRange(range, selection.Direction);
        }

        return selection;
    }

    private (IdentifiedEditOperation[] Edits, OneSnippet[] Snippets) CreateEditsAndSnippetsFromSelections(
        string template,
        SnippetSessionInsertOptions options,
        bool enforceFinalTabstop)
    {
        TextModel model = _editor.Model;
        IReadOnlyList<Selection> selections = _editor.Selections;
        var edits = new IdentifiedEditOperation[selections.Count];
        var snippets = new OneSnippet[selections.Count];
        Selection primary = selections[0];

        // Know what text the overwrite[Before|After] extensions of the primary cursor have selected: only when the secondary
        // selections extend to the same text can we grow them.
        string firstBeforeText = model.GetValueInRange(AdjustSelection(model, primary, options.OverwriteBefore, 0).Range);
        string firstAfterText = model.GetValueInRange(AdjustSelection(model, primary, 0, options.OverwriteAfter).Range);

        // Remember the first non-whitespace column to decide if `adjustWhitespace` should be overruled for secondary selections.
        int firstLineFirstNonWhitespace = model.GetLineFirstNonWhitespaceColumn(primary.PositionLineNumber);

        // Sort the selections by their start position but remember the original index: that keeps the primary cursor primary.
        var indexedSelections = selections
            .Select((selection, idx) => (Selection: selection, Idx: idx))
            .OrderBy(item => item.Selection.Range, Comparer<Range>.Create(Range.CompareUsingStarts))
            .ToList();
        bool spread = _editor.ViewModel.CursorConfig.MultiCursorPaste == MultiCursorPasteMode.Spread;

        foreach ((Selection selection, int idx) in indexedSelections)
        {
            // Extend the selection with overwriteBefore and overwriteAfter, then compare with the extensions of the primary selection.
            Selection extensionBefore = AdjustSelection(model, selection, options.OverwriteBefore, 0);
            Selection extensionAfter = AdjustSelection(model, selection, 0, options.OverwriteAfter);
            if (firstBeforeText != model.GetValueInRange(extensionBefore.Range))
            {
                extensionBefore = selection;
            }

            if (firstAfterText != model.GetValueInRange(extensionAfter.Range))
            {
                extensionAfter = selection;
            }

            // Merge the before and after selection into one.
            Selection snippetSelection = selection
                .WithStart(extensionBefore.Range.Start)
                .WithEnd(extensionAfter.Range.End);

            TextmateSnippet snippet = new SnippetParser().Parse(template, true, enforceFinalTabstop);

            // Adjust the template to the indentation and whitespace rules of this insert location (different for each cursor): when
            // asked for, or when this is a secondary cursor whose leading whitespace is different.
            string snippetLineLeadingWhitespace = AdjustWhitespace(
                model,
                snippetSelection.Range.Start,
                options.AdjustWhitespace || (idx > 0 && firstLineFirstNonWhitespace != model.GetLineFirstNonWhitespaceColumn(selection.PositionLineNumber)),
                snippet);

            snippet.ResolveVariables(new CompositeVariableResolver(
                new ModelBasedVariableResolver(_host),
                new ClipboardBasedVariableResolver(() => options.ClipboardText ?? _host.ReadClipboardText(), idx, indexedSelections.Count, spread),
                new SelectionBasedVariableResolver(model, selection, idx),
                new CommentBasedVariableResolver(model, selection.Position),
                new TimeBasedVariableResolver(_time),
                new WorkspaceBasedVariableResolver(_host),
                new RandomBasedVariableResolver(_random)));

            // Store the snippets with the index of their originating selection; the identifier marks the edit so only our undo edits
            // generate the end cursors.
            edits[idx] = new IdentifiedEditOperation(
                snippetSelection.Range,
                snippet.ToString(),
                new EditOperationIdentifier(idx, 0),
                IsTracked: true);
            snippets[idx] = new OneSnippet(_editor, snippet, snippetLineLeadingWhitespace);
        }

        return (edits, snippets);
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreakRegex();
}
