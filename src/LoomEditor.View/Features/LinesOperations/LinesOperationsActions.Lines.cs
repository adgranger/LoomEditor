// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (delete, indent, outdent, insert line)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    // Monaco: IDeleteLinesOperation. Mutable: the merge of adjacent operations changes the previous one.
    private sealed class DeleteLinesOperation(int startLineNumber, int endLineNumber, int positionColumn)
    {
        public int StartLineNumber { get; } = startLineNumber;

        public int EndLineNumber { get; set; } = endLineNumber;

        public int PositionColumn { get; } = positionColumn;
    }

    /// <summary>Deletes the lines of the selections (Monaco: <c>DeleteLinesAction.run</c>).</summary>
    public static void DeleteLines(ViewModel viewModel)
    {
        List<DeleteLinesOperation> ops = GetLinesToRemove(viewModel);

        TextModel model = viewModel.Model;
        if (model.LineCount == 1 && model.GetLineMaxColumn(1) == 1)
        {
            // Model is empty
            return;
        }

        int linesDeleted = 0;
        var edits = new List<IdentifiedEditOperation>();
        var cursorState = new List<Selection>();
        foreach (DeleteLinesOperation op in ops)
        {
            int startLineNumber = op.StartLineNumber;
            int endLineNumber = op.EndLineNumber;

            int startColumn = 1;
            int endColumn = model.GetLineMaxColumn(endLineNumber);
            if (endLineNumber < model.LineCount)
            {
                endLineNumber += 1;
                endColumn = 1;
            }
            else if (startLineNumber > 1)
            {
                startLineNumber -= 1;
                startColumn = model.GetLineMaxColumn(startLineNumber);
            }

            edits.Add(new IdentifiedEditOperation(new Range(startLineNumber, startColumn, endLineNumber, endColumn), string.Empty));
            cursorState.Add(Selection.Collapsed(new Position(startLineNumber - linesDeleted, op.PositionColumn)));
            linesDeleted += op.EndLineNumber - op.StartLineNumber + 1;
        }

        model.PushStackElement();
        viewModel.ExecuteEdits(LinesOperationsCommands.DeleteLines, edits, _ => cursorState);
        viewModel.RevealAllCursors(LinesOperationsCommands.DeleteLines, true);
        model.PushStackElement();
    }

    private static List<DeleteLinesOperation> GetLinesToRemove(ViewModel viewModel)
    {
        // Construct delete operations
        List<DeleteLinesOperation> operations = [.. viewModel.GetSelections().Select(s =>
        {
            int endLineNumber = s.Range.EndLineNumber;
            if (s.Range.StartLineNumber < s.Range.EndLineNumber && s.Range.EndColumn == 1)
            {
                endLineNumber -= 1;
            }

            return new DeleteLinesOperation(s.Range.StartLineNumber, endLineNumber, s.Position.Column);
        })
            // Sort delete operations (stable)
            .OrderBy(o => o.StartLineNumber)
            .ThenBy(o => o.EndLineNumber)];

        // Merge delete operations which are adjacent or overlapping
        var mergedOperations = new List<DeleteLinesOperation>();
        DeleteLinesOperation previousOperation = operations[0];
        for (int i = 1; i < operations.Count; i++)
        {
            if (previousOperation.EndLineNumber + 1 >= operations[i].StartLineNumber)
            {
                // Merge current operations into the previous one
                previousOperation.EndLineNumber = operations[i].EndLineNumber;
            }
            else
            {
                // Push previous operation
                mergedOperations.Add(previousOperation);
                previousOperation = operations[i];
            }
        }

        // Push the last operation
        mergedOperations.Add(previousOperation);

        return mergedOperations;
    }

    /// <summary>Indents the lines of the selections (Monaco: <c>IndentLinesAction.run</c>).</summary>
    public static void IndentLines(ViewModel viewModel) =>
        // Monaco passes the text model, not the view model.
        Execute(viewModel, LinesOperationsCommands.IndentLines, TypeOperations.Indent(viewModel.CursorConfig, new TextModelCursorSimpleModel(viewModel.Model), viewModel.GetSelections()));

    /// <summary>Outdents the lines of the selections (Monaco: <c>OutdentLinesAction.run</c>, the core <c>outdent</c> command).</summary>
    public static void OutdentLines(ViewModel viewModel) => CoreEditingCommands.Outdent.RunCoreEditingCommand(viewModel);

    /// <summary>Inserts a line above each cursor (Monaco: <c>InsertLineBeforeAction.run</c>; one undo stop, before only).</summary>
    public static void InsertLineBefore(ViewModel viewModel)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(EnterOperation.LineInsertBefore(viewModel.CursorConfig, viewModel.Model, viewModel.GetSelections()), LinesOperationsCommands.InsertLineBefore);
    }

    /// <summary>Inserts a line below each cursor (Monaco: <c>InsertLineAfterAction.run</c>; one undo stop, before only).</summary>
    public static void InsertLineAfter(ViewModel viewModel)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(EnterOperation.LineInsertAfter(viewModel.CursorConfig, viewModel.Model, viewModel.GetSelections()), LinesOperationsCommands.InsertLineAfter);
    }
}
