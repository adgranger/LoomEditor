// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorDeleteOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Backspace and Delete: the commands to run for each selection (Monaco: <c>DeleteOperations</c>).</summary>
/// <remarks>
/// Phase 4–5 cut points: <c>isAutoClosingPairDelete</c> and <c>_runAutoClosingPairDelete</c> (auto-closing pairs need
/// a language configuration). <c>cut</c> is ported with the clipboard commands (P3-36).
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

    /// <summary>The commands that delete to the left (Backspace), one per selection; <see langword="null"/> when there is nothing to delete.</summary>
    public static (bool ShouldPushStackElementBefore, IReadOnlyList<IEditCommand?> Commands) DeleteLeft(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        ICursorSimpleModel model,
        IReadOnlyList<Selection> selections)
    {
        // Phase 4–5 cut point: cursorDeleteOperations.ts 167–169 (isAutoClosingPairDelete): without a language,
        // autoClosingPairsOpenByEnd is empty and Monaco never takes this branch.
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
