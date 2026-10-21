// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorDeleteOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Backspace and Delete: the commands to run for each selection (Monaco: <c>DeleteOperations</c>).</summary>
/// <remarks>
/// <c>cut</c> is ported with the clipboard commands (P3-36).
/// Threading: stateless; call it from the UI thread, like the model it reads.
/// </remarks>
internal static class DeleteOperations
{
    /// <summary>The commands that delete to the right (Delete key), one per selection; <see langword="null"/> when there is nothing to delete.</summary>
    public static (bool ShouldPushStackElementBefore, IReadOnlyList<IEditCommand?> Commands) DeleteRight(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        ICursorSimpleModel model,
        IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        bool shouldPushStackElementBefore = prevEditOperationType != EditOperationType.DeletingRight;
        foreach (Selection selection in selections)
        {
            Range deleteSelection = GetDeleteRightRange(selection, model, config);

            if (deleteSelection.IsEmpty)
            {
                // Probably at end of file => ignore
                commands.Add(null);
                continue;
            }

            if (deleteSelection.StartLineNumber != deleteSelection.EndLineNumber)
            {
                shouldPushStackElementBefore = true;
            }

            commands.Add(new ReplaceCommand(deleteSelection, string.Empty));
        }

        return (shouldPushStackElementBefore, commands);
    }

    /// <summary>Whether Backspace deletes the auto-closing pair around every cursor (Monaco: <c>isAutoClosingPairDelete</c>).</summary>
    public static bool IsAutoClosingPairDelete(
        EditorAutoClosingEditStrategy autoClosingDelete,
        EditorAutoClosingStrategy autoClosingBrackets,
        EditorAutoClosingStrategy autoClosingQuotes,
        IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> autoClosingPairsOpen,
        ICursorSimpleModel model,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters)
    {
        if (autoClosingBrackets == EditorAutoClosingStrategy.Never && autoClosingQuotes == EditorAutoClosingStrategy.Never)
        {
            return false;
        }

        if (autoClosingDelete == EditorAutoClosingEditStrategy.Never)
        {
            return false;
        }

        foreach (Selection selection in selections)
        {
            Position position = selection.Position;

            if (!selection.IsEmpty)
            {
                return false;
            }

            string lineText = model.GetLineContent(position.LineNumber);
            if (position.Column < 2 || position.Column >= lineText.Length + 1)
            {
                return false;
            }

            string character = lineText[position.Column - 2].ToString();

            if (!autoClosingPairsOpen.TryGetValue(character, out List<StandardAutoClosingPairConditional>? autoClosingPairCandidates))
            {
                return false;
            }

            if (CursorCommon.IsQuote(character))
            {
                if (autoClosingQuotes == EditorAutoClosingStrategy.Never)
                {
                    return false;
                }
            }
            else if (autoClosingBrackets == EditorAutoClosingStrategy.Never)
            {
                return false;
            }

            string afterCharacter = lineText[position.Column - 1].ToString();

            bool foundAutoClosingPair = false;
            foreach (StandardAutoClosingPairConditional candidate in autoClosingPairCandidates)
            {
                if (candidate.Open == character && candidate.Close == afterCharacter)
                {
                    foundAutoClosingPair = true;
                }
            }

            if (!foundAutoClosingPair)
            {
                return false;
            }

            // Must delete the pair only if it was automatically inserted by the editor
            if (autoClosingDelete == EditorAutoClosingEditStrategy.Auto)
            {
                bool found = false;
                foreach (Range autoClosedCharacter in autoClosedCharacters)
                {
                    if (position.LineNumber == autoClosedCharacter.StartLineNumber && position.Column == autoClosedCharacter.StartColumn)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Monaco: _runAutoClosingPairDelete
    private static (bool ShouldPushStackElementBefore, IReadOnlyList<IEditCommand?> Commands) RunAutoClosingPairDelete(IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            Position position = selection.Position;
            var deleteSelection = new Range(position.LineNumber, position.Column - 1, position.LineNumber, position.Column + 1);
            commands.Add(new ReplaceCommand(deleteSelection, string.Empty));
        }

        return (true, commands);
    }

    /// <summary>The commands that delete to the left (Backspace), one per selection; <see langword="null"/> when there is nothing to delete.</summary>
    public static (bool ShouldPushStackElementBefore, IReadOnlyList<IEditCommand?> Commands) DeleteLeft(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        ICursorSimpleModel model,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters)
    {
        if (IsAutoClosingPairDelete(
                config.AutoClosingDelete,
                config.AutoClosingBrackets,
                config.AutoClosingQuotes,
                config.AutoClosingPairs.AutoClosingPairsOpenByEnd,
                model,
                selections,
                autoClosedCharacters))
        {
            return RunAutoClosingPairDelete(selections);
        }

        var commands = new List<IEditCommand?>(selections.Count);
        bool shouldPushStackElementBefore = prevEditOperationType != EditOperationType.DeletingLeft;
        foreach (Selection selection in selections)
        {
            Range deleteRange = GetDeleteLeftRange(selection, model, config);

            // Ignore empty delete ranges, as they have no effect
            // They happen if the cursor is at the beginning of the file.
            if (deleteRange.IsEmpty)
            {
                commands.Add(null);
                continue;
            }

            if (deleteRange.StartLineNumber != deleteRange.EndLineNumber)
            {
                shouldPushStackElementBefore = true;
            }

            commands.Add(new ReplaceCommand(deleteRange, string.Empty));
        }

        return (shouldPushStackElementBefore, commands);
    }

    /// <summary>The commands that cut every selection; an empty selection cuts its whole line when <c>EmptySelectionClipboard</c> is on (Monaco: <c>cut</c>).</summary>
    public static EditOperationResult Cut(CursorConfiguration config, ICursorSimpleModel model, IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        Range? lastCutRange = null;

        // Monaco sorts the array in place with a comparison that is not consistent (the start of one selection against the
        // end of the other). List.Sort may throw with such a comparer, so this sorts a copy with a stable insertion sort.
        var sorted = new List<Selection>(selections.Count);
        foreach (Selection item in selections)
        {
            int index = sorted.Count;
            while (index > 0 && sorted[index - 1].Range.Start.CompareTo(item.Range.End) > 0)
            {
                index--;
            }

            sorted.Insert(index, item);
        }

        foreach (Selection selection in sorted)
        {
            if (selection.IsEmpty)
            {
                if (config.EmptySelectionClipboard)
                {
                    // This is a full line cut
                    Position position = selection.Position;

                    int startLineNumber;
                    int startColumn;
                    int endLineNumber;
                    int endColumn;

                    if (position.LineNumber < model.GetLineCount())
                    {
                        // Cutting a line in the middle of the model
                        startLineNumber = position.LineNumber;
                        startColumn = 1;
                        endLineNumber = position.LineNumber + 1;
                        endColumn = 1;
                    }
                    else if (position.LineNumber > 1 && lastCutRange?.EndLineNumber != position.LineNumber)
                    {
                        // Cutting the last line & there are more than 1 lines in the model & a previous cut operation does not touch the current cut operation
                        startLineNumber = position.LineNumber - 1;
                        startColumn = model.GetLineMaxColumn(position.LineNumber - 1);
                        endLineNumber = position.LineNumber;
                        endColumn = model.GetLineMaxColumn(position.LineNumber);
                    }
                    else
                    {
                        // Cutting the single line that the model contains
                        startLineNumber = position.LineNumber;
                        startColumn = 1;
                        endLineNumber = position.LineNumber;
                        endColumn = model.GetLineMaxColumn(position.LineNumber);
                    }

                    var deleteSelection = new Range(startLineNumber, startColumn, endLineNumber, endColumn);
                    lastCutRange = deleteSelection;

                    commands.Add(deleteSelection.IsEmpty ? null : new ReplaceCommand(deleteSelection, string.Empty));
                }
                else
                {
                    // Cannot cut empty selection
                    commands.Add(null);
                }
            }
            else
            {
                commands.Add(new ReplaceCommand(selection.Range, string.Empty));
            }
        }

        return new EditOperationResult(EditOperationType.Other, commands, true, true);
    }

    private static Range GetDeleteRightRange(Selection selection, ICursorSimpleModel model, CursorConfiguration config)
    {
        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        Position position = selection.Position;
        CursorPosition rightOfPosition = MoveOperations.Right(config, model, position);

        if (config.TrimWhitespaceOnDelete && rightOfPosition.LineNumber != position.LineNumber)
        {
            // Smart line join (deleting leading whitespace) is on
            // (and) Delete is happening at the end of a line
            bool currentLineHasContent = model.GetLineFirstNonWhitespaceColumn(position.LineNumber) > 0;
            int firstNonWhitespaceColumn = model.GetLineFirstNonWhitespaceColumn(rightOfPosition.LineNumber);
            if (currentLineHasContent && firstNonWhitespaceColumn > 0)
            {
                // The next line has content
                return new Range(rightOfPosition.LineNumber, firstNonWhitespaceColumn, position.LineNumber, position.Column);
            }
        }

        return new Range(rightOfPosition.LineNumber, rightOfPosition.Column, position.LineNumber, position.Column);
    }

    private static Range GetDeleteLeftRange(Selection selection, ICursorSimpleModel model, CursorConfiguration config)
    {
        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        Position position = selection.Position;

        // Unintend when using tab stops and cursor is within indentation
        if (config.UseTabStops && position.Column > 1)
        {
            string lineContent = model.GetLineContent(position.LineNumber);

            int firstNonWhitespaceIndex = StringClassification.FirstNonWhitespaceIndex(lineContent);
            int lastIndentationColumn = firstNonWhitespaceIndex == -1
                ? lineContent.Length + 1 // entire string is whitespace
                : firstNonWhitespaceIndex + 1;

            if (position.Column <= lastIndentationColumn)
            {
                int fromVisibleColumn = config.VisibleColumnFromColumn(model, position);
                int toVisibleColumn = CursorColumns.PrevIndentTabStop(fromVisibleColumn, config.IndentSize);
                int toColumn = config.ColumnFromVisibleColumn(model, position.LineNumber, toVisibleColumn);
                return new Range(position.LineNumber, toColumn, position.LineNumber, position.Column);
            }
        }

        return new Range(GetPositionAfterDeleteLeft(position, model), position);
    }

    private static Position GetPositionAfterDeleteLeft(Position position, ICursorSimpleModel model)
    {
        if (position.Column > 1)
        {
            // Convert 1-based columns to 0-based offsets and back.
            int idx = Strings.GetLeftDeleteOffset(position.Column - 1, model.GetLineContent(position.LineNumber));
            return new Position(position.LineNumber, idx + 1);
        }

        if (position.LineNumber > 1)
        {
            int newLine = position.LineNumber - 1;
            return new Position(newLine, model.GetLineMaxColumn(newLine));
        }

        return position;
    }
}
