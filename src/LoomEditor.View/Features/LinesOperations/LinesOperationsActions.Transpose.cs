// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (TransposeAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    // JavaScript's split('').reverse().join('') reverses UTF-16 code units: so does this.
    private static string ReverseChars(string text)
    {
        char[] chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>Transposes the characters around each empty cursor (Monaco: <c>TransposeAction.run</c>).</summary>
    public static void Transpose(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;
        var commands = new List<IEditCommand?>();

        foreach (Selection selection in viewModel.GetSelections())
        {
            if (!selection.IsEmpty)
            {
                continue;
            }

            Position cursor = selection.Range.Start;
            int maxColumn = model.GetLineMaxColumn(cursor.LineNumber);

            if (cursor.Column >= maxColumn)
            {
                if (cursor.LineNumber == model.LineCount)
                {
                    continue;
                }

                // The cursor is at the end of current line and current line is not empty
                // then we transpose the character before the cursor and the line break if there is any following line.
                var deleteSelection = new Range(cursor.LineNumber, Math.Max(1, cursor.Column - 1), cursor.LineNumber + 1, 1);
                commands.Add(new ReplaceCommand(deleteSelection, ReverseChars(model.GetValueInRange(deleteSelection))));
            }
            else
            {
                var deleteSelection = new Range(cursor.LineNumber, Math.Max(1, cursor.Column - 1), cursor.LineNumber, cursor.Column + 1);
                commands.Add(new ReplaceCommandThatPreservesSelection(
                    deleteSelection,
                    ReverseChars(model.GetValueInRange(deleteSelection)),
                    Selection.Collapsed(new Position(cursor.LineNumber, cursor.Column + 1))));
            }
        }

        model.PushStackElement();
        viewModel.ExecuteCommands(commands, LinesOperationsCommands.Transpose);
        model.PushStackElement();
    }
}
