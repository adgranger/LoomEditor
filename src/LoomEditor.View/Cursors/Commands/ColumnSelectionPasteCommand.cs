// Ported from microsoft/vscode: src/vs/editor/common/commands/columnSelectionPasteCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>
/// Pastes the lines of a column (block) selection as a rectangle: each line goes to the next model line, at the same
/// visible column, and short lines are padded (Monaco: <c>ColumnSelectionPasteCommand</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ColumnSelectionPasteCommand(Selection selection, IReadOnlyList<string> text, int tabSize, bool overtype) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Range selectionRange = selection.Range;
        int visibleColumn = CursorColumns.VisibleColumnFromColumn(model.GetLineContent(selectionRange.StartLineNumber), selectionRange.StartColumn, tabSize);
        string[] paddedText = PadTextToBlockWidth(visibleColumn);
        int lineCount = model.LineCount;

        for (int i = 0; i < paddedText.Length; i++)
        {
            int lineNumber = selectionRange.EndLineNumber + i;
            if (lineNumber > lineCount)
            {
                int endColumn = model.GetLineMaxColumn(lineCount);
                string padding = new(' ', visibleColumn);
                string appendedText = "\n" + padding + string.Join("\n" + padding, paddedText.Skip(i));
                builder.AddTrackedEditOperation(new Range(lineCount, endColumn, lineCount, endColumn), appendedText);
                break;
            }

            Range range;
            string lineText = paddedText[i];
            if (i == 0)
            {
                range = selectionRange;
            }
            else
            {
                string lineContent = model.GetLineContent(lineNumber);
                int column = CursorColumns.ColumnFromVisibleColumn(lineContent, visibleColumn, tabSize);
                range = new Range(lineNumber, column, lineNumber, column);
                if (column == model.GetLineMaxColumn(lineNumber))
                {
                    int endVisibleColumn = CursorColumns.VisibleColumnFromColumn(lineContent, column, tabSize);
                    lineText = new string(' ', Math.Max(0, visibleColumn - endVisibleColumn)) + lineText;
                }
            }

            IEditCommand chosenReplaceCommand = overtype
                ? new ReplaceOvertypeCommand(range, lineText)
                : new ReplaceCommand(range, lineText);
            chosenReplaceCommand.GetEditOperations(model, builder);
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        IReadOnlyList<LoomEditor.Core.Model.Edits.ValidEditOperation> operations = helper.GetInverseEditOperations();
        return Selection.Collapsed(operations[^1].Range.End);
    }

    private string[] PadTextToBlockWidth(int visibleColumn)
    {
        string prefix = new(' ', visibleColumn);
        var endVisibleColumns = new List<int>(text.Count);
        int blockEndVisibleColumn = visibleColumn;

        foreach (string line in text)
        {
            string textWithPrefix = prefix + line;
            int endVisibleColumn = CursorColumns.VisibleColumnFromColumn(textWithPrefix, textWithPrefix.Length + 1, tabSize);
            endVisibleColumns.Add(endVisibleColumn);
            blockEndVisibleColumn = Math.Max(blockEndVisibleColumn, endVisibleColumn);
        }

        var result = new string[text.Count];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = text[index] + new string(' ', blockEndVisibleColumn - endVisibleColumns[index]);
        }

        return result;
    }
}
