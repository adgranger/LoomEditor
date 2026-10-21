// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/replaceAllCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Find;

/// <summary>Replaces many ranges in one command, merging the ranges that touch each other (Monaco: <c>ReplaceAllCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ReplaceAllCommand(Selection editorSelection, IReadOnlyList<Range> ranges, IReadOnlyList<string> replaceStrings) : IEditCommand
{
    private string? _trackedEditorSelectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        if (ranges.Count > 0)
        {
            // Collect all edit operations, sorted in ascending order by range starts (OrderBy is stable, like Monaco's sort)
            List<(Range Range, string Text)> ops =
            [
                .. Enumerable.Range(0, ranges.Count)
                    .Select(i => (Range: ranges[i], Text: replaceStrings[i]))
                    .OrderBy(op => op.Range, Comparer<Range>.Create(Range.CompareUsingStarts)),
            ];

            // Merge operations that touch each other
            List<(Range Range, string Text)> resultOps = [];
            var previousOp = ops[0];
            for (var i = 1; i < ops.Count; i++)
            {
                if (previousOp.Range.EndLineNumber == ops[i].Range.StartLineNumber && previousOp.Range.EndColumn == ops[i].Range.StartColumn)
                {
                    // These operations are one after another and can be merged
                    previousOp = (previousOp.Range.Plus(ops[i].Range), previousOp.Text + ops[i].Text);
                }
                else
                {
                    resultOps.Add(previousOp);
                    previousOp = ops[i];
                }
            }

            resultOps.Add(previousOp);

            foreach (var (range, text) in resultOps)
            {
                builder.AddEditOperation(range, text);
            }
        }

        _trackedEditorSelectionId = builder.TrackSelection(editorSelection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) =>
        helper.GetTrackedSelection(_trackedEditorSelectionId!);
}
