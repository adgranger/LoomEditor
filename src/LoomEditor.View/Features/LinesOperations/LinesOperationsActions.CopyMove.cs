// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (copy, duplicate and move actions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>What the line commands do to the selections of an editor.</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static partial class LinesOperationsActions
{
    private sealed class CopySelection(Selection selection, int index)
    {
        public Selection Selection { get; } = selection;

        public int Index { get; } = index;

        public bool Ignore { get; set; }
    }

    /// <summary>Copies the selected lines up or down (Monaco: <c>AbstractCopyLinesAction.run</c>).</summary>
    public static void CopyLines(ViewModel viewModel, bool down, string id)
    {
        // OrderBy is stable, like Monaco's Array.sort.
        List<CopySelection> selections = [.. viewModel.GetSelections()
            .Select((selection, index) => new CopySelection(selection, index))
            .OrderBy(s => s.Selection.Range, Comparer<Range>.Create(Range.CompareUsingStarts))];

        // Remove selections that would result in copying the same line
        CopySelection prev = selections[0];
        for (int i = 1; i < selections.Count; i++)
        {
            CopySelection curr = selections[i];
            if (prev.Selection.Range.EndLineNumber == curr.Selection.Range.StartLineNumber)
            {
                // these two selections would copy the same line
                if (prev.Index < curr.Index)
                {
                    // prev wins
                    curr.Ignore = true;
                }
                else
                {
                    // curr wins
                    prev.Ignore = true;
                    prev = curr;
                }
            }
        }

        Execute(viewModel, id, [.. selections.Select(s => (IEditCommand?)new CopyLinesCommand(s.Selection, down, s.Ignore))]);
    }

    /// <summary>Duplicates the selections (Monaco: <c>DuplicateSelectionAction.run</c>).</summary>
    public static void DuplicateSelection(ViewModel viewModel)
    {
        var commands = new List<IEditCommand?>();

        foreach (Selection selection in viewModel.GetSelections())
        {
            if (selection.IsEmpty)
            {
                commands.Add(new CopyLinesCommand(selection, true));
            }
            else
            {
                var insertSelection = new Selection(selection.Range.EndLineNumber, selection.Range.EndColumn, selection.Range.EndLineNumber, selection.Range.EndColumn);
                commands.Add(new ReplaceCommandThatSelectsText(insertSelection.Range, viewModel.Model.GetValueInRange(selection.Range)));
            }
        }

        Execute(viewModel, LinesOperationsCommands.DuplicateSelection, commands);
    }

    /// <summary>Moves the selected lines up or down (Monaco: <c>AbstractMoveLinesAction.run</c>).</summary>
    public static void MoveLines(ViewModel viewModel, bool down, string id)
    {
        var commands = new List<IEditCommand?>();

        foreach (Selection selection in viewModel.GetSelections())
        {
            commands.Add(new MoveLinesCommand(selection, down, viewModel.CursorConfig.AutoIndent, viewModel.Model.LanguageConfigurationRegistry));
        }

        Execute(viewModel, id, commands);
    }

    private static void Execute(ViewModel viewModel, string id, IReadOnlyList<IEditCommand?> commands)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(commands, id);
        viewModel.Model.PushStackElement();
    }
}
