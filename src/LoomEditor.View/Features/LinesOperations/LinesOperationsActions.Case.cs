// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (AbstractCaseAction)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

internal static partial class LinesOperationsActions
{
    /// <summary>
    /// Transforms the selections, or the word at each empty cursor, with <paramref name="modifyText"/>
    /// (Monaco: <c>AbstractCaseAction.run</c>).
    /// </summary>
    public static void TransformCase(ViewModel viewModel, string id, Func<string, string> modifyText)
    {
        var textEdits = new List<IdentifiedEditOperation>();

        foreach (Selection selection in viewModel.GetSelections())
        {
            if (selection.IsEmpty)
            {
                Position cursor = selection.Range.Start;
                WordAtPosition? word = Cursors.WordOperations.GetWordAtPosition(
                    viewModel.Model, viewModel.CursorConfig.WordSeparators, viewModel.CursorConfig.WordSegmenterLocales, cursor);

                if (word is not { } w)
                {
                    continue;
                }

                var wordRange = new Range(cursor.LineNumber, w.StartColumn, cursor.LineNumber, w.EndColumn);
                string text = viewModel.Model.GetValueInRange(wordRange);
                textEdits.Add(new IdentifiedEditOperation(wordRange, modifyText(text)));
            }
            else
            {
                string text = viewModel.Model.GetValueInRange(selection.Range);
                textEdits.Add(new IdentifiedEditOperation(selection.Range, modifyText(text)));
            }
        }

        viewModel.Model.PushStackElement();
        viewModel.ExecuteEdits(id, textEdits, _ => null);
        viewModel.Model.PushStackElement();
    }
}
