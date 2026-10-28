// Ported from microsoft/vscode: src/vs/editor/contrib/caretOperations/browser/moveCaretCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.CaretOperations;

/// <summary>Moves a non-empty single-line selection one character left or right, swapping it with the neighbouring character.</summary>
/// <remarks>
/// Like Monaco, <see cref="ComputeCursorState"/> always shifts the selection, even when the guards made the command a no-op;
/// the cursor clamps the result to the model. Threading: UI thread only.
/// </remarks>
internal sealed class MoveCaretCommand(Selection selection, bool isMovingLeft) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        if (selection.Range.StartLineNumber != selection.Range.EndLineNumber || selection.IsEmpty)
        {
            return;
        }

        int lineNumber = selection.Range.StartLineNumber;
        int startColumn = selection.Range.StartColumn;
        int endColumn = selection.Range.EndColumn;
        if (isMovingLeft && startColumn == 1)
        {
            return;
        }

        if (!isMovingLeft && endColumn == model.GetLineMaxColumn(lineNumber))
        {
            return;
        }

        if (isMovingLeft)
        {
            var rangeBefore = new Range(lineNumber, startColumn - 1, lineNumber, startColumn);
            string charBefore = model.GetValueInRange(rangeBefore);
            builder.AddEditOperation(rangeBefore, null);
            builder.AddEditOperation(new Range(lineNumber, endColumn, lineNumber, endColumn), charBefore);
        }
        else
        {
            var rangeAfter = new Range(lineNumber, endColumn, lineNumber, endColumn + 1);
            string charAfter = model.GetValueInRange(rangeAfter);
            builder.AddEditOperation(rangeAfter, null);
            builder.AddEditOperation(new Range(lineNumber, startColumn, lineNumber, startColumn), charAfter);
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        int delta = isMovingLeft ? -1 : 1;
        return new Selection(selection.Range.StartLineNumber, selection.Range.StartColumn + delta, selection.Range.EndLineNumber, selection.Range.EndColumn + delta);
    }
}
