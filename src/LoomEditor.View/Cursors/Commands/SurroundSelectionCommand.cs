// Ported from microsoft/vscode: src/vs/editor/common/commands/surroundSelectionCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>Surrounds a selection with a character before it and a character after it (Monaco: <c>SurroundSelectionCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SurroundSelectionCommand(Selection range, string charBeforeSelection, string charAfterSelection) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        builder.AddTrackedEditOperation(new Range(range.Range.StartLineNumber, range.Range.StartColumn, range.Range.StartLineNumber, range.Range.StartColumn), charBeforeSelection);

        // An empty text must still make an operation (issue #206774): a null text deletes the empty range, exactly like Monaco's `|| null`.
        builder.AddTrackedEditOperation(
            new Range(range.Range.EndLineNumber, range.Range.EndColumn, range.Range.EndLineNumber, range.Range.EndColumn),
            charAfterSelection.Length == 0 ? null : charAfterSelection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        IReadOnlyList<LoomEditor.Core.Model.Edits.ValidEditOperation> inverseEditOperations = helper.GetInverseEditOperations();
        Range firstOperationRange = inverseEditOperations[0].Range;
        Range secondOperationRange = inverseEditOperations[1].Range;

        return new Selection(
            firstOperationRange.EndLineNumber,
            firstOperationRange.EndColumn,
            secondOperationRange.EndLineNumber,
            secondOperationRange.EndColumn - charAfterSelection.Length);
    }
}

/// <summary>A surround selection command that runs after a composition finished (Monaco: <c>CompositionSurroundSelectionCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CompositionSurroundSelectionCommand(Position position, string text, string charAfter) : IEditCommand
{
    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder) =>
        builder.AddTrackedEditOperation(new Range(position.LineNumber, position.Column, position.LineNumber, position.Column), text + charAfter);

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Range opRange = helper.GetInverseEditOperations()[0].Range;

        return new Selection(opRange.EndLineNumber, opRange.StartColumn, opRange.EndLineNumber, opRange.EndColumn - charAfter.Length);
    }
}
