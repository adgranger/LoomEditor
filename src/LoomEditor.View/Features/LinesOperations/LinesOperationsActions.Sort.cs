// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (sort, duplicates, reverse, trim)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    private static List<Selection> WholeDocumentIfSingleLine(TextModel model, List<Selection> selections) =>
        selections.Count == 1 && selections[0].Range.StartLineNumber == selections[0].Range.EndLineNumber
            ? [new Selection(1, 1, model.LineCount, model.GetLineMaxColumn(model.LineCount))]
            : selections;

    /// <summary>Sorts the selected lines (Monaco: <c>AbstractSortLinesAction.run</c>).</summary>
    public static void SortLines(ViewModel viewModel, bool descending, string id)
    {
        TextModel model = viewModel.Model;
        List<Selection> selections = WholeDocumentIfSingleLine(model, viewModel.GetSelections());

        foreach (Selection selection in selections)
        {
            if (!SortLinesCommand.CanRun(model, selection, descending))
            {
                return;
            }
        }

        Execute(viewModel, id, [.. selections.Select(s => (IEditCommand?)new SortLinesCommand(s, descending))]);
    }

    /// <summary>Removes the duplicate lines of the selections (Monaco: <c>DeleteDuplicateLinesAction.run</c>).</summary>
    public static void RemoveDuplicateLines(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;
        if (model.LineCount == 1 && model.GetLineMaxColumn(1) == 1)
        {
            return;
        }

        var edits = new List<IdentifiedEditOperation>();
        var endCursorState = new List<Selection>();

        int linesDeleted = 0;
        bool updateSelection = true;

        List<Selection> selections = viewModel.GetSelections();
        if (selections.Count == 1 && selections[0].Range.StartLineNumber == selections[0].Range.EndLineNumber)
        {
            // Apply to whole document.
            selections = [new Selection(1, 1, model.LineCount, model.GetLineMaxColumn(model.LineCount))];
            updateSelection = false;
        }

        foreach (Selection selection in selections)
        {
            var uniqueLines = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<string>();

            for (int i = selection.Range.StartLineNumber; i <= selection.Range.EndLineNumber; i++)
            {
                string line = model.GetLineContent(i);

                if (!uniqueLines.Add(line))
                {
                    continue;
                }

                lines.Add(line);
            }

            var selectionToReplace = new Range(
                selection.Range.StartLineNumber,
                1,
                selection.Range.EndLineNumber,
                model.GetLineMaxColumn(selection.Range.EndLineNumber));

            int adjustedSelectionStart = selection.Range.StartLineNumber - linesDeleted;
            var finalSelection = new Selection(
                adjustedSelectionStart,
                1,
                adjustedSelectionStart + lines.Count - 1,
                lines[^1].Length + 1);

            edits.Add(new IdentifiedEditOperation(selectionToReplace, string.Join('\n', lines)));
            endCursorState.Add(finalSelection);

            linesDeleted += selection.Range.EndLineNumber - selection.Range.StartLineNumber + 1 - lines.Count;
        }

        ExecuteEdits(viewModel, LinesOperationsCommands.RemoveDuplicateLines, edits, updateSelection ? endCursorState : null);
    }

    /// <summary>Reverses the selected lines (Monaco: <c>ReverseLinesAction.run</c>).</summary>
    public static void ReverseLines(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;
        List<Selection> originalSelections = viewModel.GetSelections();
        List<Selection> selections = WholeDocumentIfSingleLine(model, originalSelections);

        var edits = new List<IdentifiedEditOperation>();
        var resultingSelections = new List<Selection>();

        for (int i = 0; i < selections.Count; i++)
        {
            Selection selection = selections[i];
            Selection originalSelection = originalSelections[i];
            int endLineNumber = selection.Range.EndLineNumber;
            if (selection.Range.StartLineNumber < selection.Range.EndLineNumber && selection.Range.EndColumn == 1)
            {
                endLineNumber--;
            }

            var range = new Range(selection.Range.StartLineNumber, 1, endLineNumber, model.GetLineMaxColumn(endLineNumber));

            // Exclude last line if empty and we're at the end of the document
            if (endLineNumber == model.LineCount && model.GetLineContent(range.EndLineNumber).Length == 0)
            {
                range = new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber - 1, model.GetLineMaxColumn(range.EndLineNumber - 1));
            }

            var lines = new List<string>();
            for (int lineNumber = range.EndLineNumber; lineNumber >= range.StartLineNumber; lineNumber--)
            {
                lines.Add(model.GetLineContent(lineNumber));
            }

            edits.Add(new IdentifiedEditOperation(range, string.Join('\n', lines)));

            Range reversed = range;
            int UpdateLineNumber(int lineNumber) =>
                lineNumber <= reversed.EndLineNumber ? reversed.EndLineNumber - lineNumber + reversed.StartLineNumber : lineNumber;

            Position selectionStart = originalSelection.SelectionStart;
            Position position = originalSelection.Position;
            resultingSelections.Add(originalSelection.IsEmpty
                // keep just the cursor
                ? Selection.Collapsed(new Position(UpdateLineNumber(position.LineNumber), position.Column))
                // keep selection, maintaining its direction
                : new Selection(
                    new Position(UpdateLineNumber(selectionStart.LineNumber), selectionStart.Column),
                    new Position(UpdateLineNumber(position.LineNumber), position.Column)));
        }

        ExecuteEdits(viewModel, LinesOperationsCommands.ReverseLines, edits, resultingSelections);
    }

    /// <summary>
    /// Trims the trailing whitespace (Monaco: <c>TrimTrailingWhitespaceAction.run</c>). The command has no arguments, so the
    /// <c>auto-save</c> reason never happens and no cursor line is spared.
    /// </summary>
    public static void TrimTrailingWhitespace(ViewModel viewModel)
    {
        // files.trimTrailingWhitespaceInRegexAndStrings: VS Code's default
        var command = new TrimTrailingWhitespaceCommand(viewModel.GetSelection(), [], true);
        Execute(viewModel, LinesOperationsCommands.TrimTrailingWhitespace, [command]);
    }

    private static void ExecuteEdits(ViewModel viewModel, string id, List<IdentifiedEditOperation> edits, List<Selection>? endCursorState)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteEdits(id, edits, _ => endCursorState);
        viewModel.Model.PushStackElement();
    }
}
