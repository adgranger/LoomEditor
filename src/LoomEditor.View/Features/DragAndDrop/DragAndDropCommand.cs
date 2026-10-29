// Ported from microsoft/vscode: src/vs/editor/contrib/dnd/browser/dragAndDropCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.DragAndDrop;

/// <summary>Moves (or, with <c>copy</c>, copies) the text of a selection to a position and selects the result.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DragAndDropCommand(Selection selection, Position targetPosition, bool copy) : IEditCommand
{
    private Selection? _targetSelection;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Range range = selection.Range;
        string text = model.GetValueInRange(range);
        if (!copy)
        {
            builder.AddEditOperation(range, null);
        }

        builder.AddEditOperation(new Range(targetPosition.LineNumber, targetPosition.Column, targetPosition.LineNumber, targetPosition.Column), text);

        bool sameLine = range.StartLineNumber == range.EndLineNumber;
        int width = range.EndColumn - range.StartColumn;
        if (range.Contains(targetPosition) && !(copy && (range.End == targetPosition || range.Start == targetPosition)))
        {
            // The selection stays as it is (the exception above: we allow users to paste content beside the selection).
            _targetSelection = selection;
            return;
        }

        if (copy)
        {
            _targetSelection = new Selection(
                targetPosition.LineNumber,
                targetPosition.Column,
                range.EndLineNumber - range.StartLineNumber + targetPosition.LineNumber,
                sameLine ? targetPosition.Column + width : range.EndColumn);
            return;
        }

        if (targetPosition.LineNumber > range.EndLineNumber)
        {
            // Drag the selection downwards.
            _targetSelection = new Selection(
                targetPosition.LineNumber - range.EndLineNumber + range.StartLineNumber,
                targetPosition.Column,
                targetPosition.LineNumber,
                sameLine ? targetPosition.Column + width : range.EndColumn);
            return;
        }

        if (targetPosition.LineNumber < range.EndLineNumber)
        {
            // Drag the selection upwards.
            _targetSelection = new Selection(
                targetPosition.LineNumber,
                targetPosition.Column,
                targetPosition.LineNumber + range.EndLineNumber - range.StartLineNumber,
                sameLine ? targetPosition.Column + width : range.EndColumn);
            return;
        }

        // The target position is at the same line as the selection's end position.
        if (range.EndColumn <= targetPosition.Column)
        {
            // The target position is after the selection's end position.
            _targetSelection = new Selection(
                targetPosition.LineNumber - range.EndLineNumber + range.StartLineNumber,
                targetPosition.Column - range.EndColumn + range.StartColumn,
                targetPosition.LineNumber,
                sameLine ? targetPosition.Column : range.EndColumn);
        }
        else
        {
            // Before the selection's end position, and the selection does not contain the target: it is one line, target before it.
            _targetSelection = new Selection(
                targetPosition.LineNumber - range.EndLineNumber + range.StartLineNumber,
                targetPosition.Column,
                targetPosition.LineNumber,
                targetPosition.Column + width);
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => _targetSelection!.Value;
}
