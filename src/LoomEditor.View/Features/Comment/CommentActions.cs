// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/comment.ts (CommentLineAction.run, BlockCommentAction.run)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Comment;

/// <summary>What the comment commands do to the selections of an editor.</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class CommentActions
{
    private sealed class SelectionInfo(Selection selection, int index)
    {
        public Selection Selection { get; } = selection;

        public int Index { get; } = index;

        public bool IgnoreFirstLine { get; set; }
    }

    /// <summary>Runs a line comment command on every selection (Monaco: <c>CommentLineAction.run</c>).</summary>
    public static void RunLineComment(ViewModel viewModel, LineCommentType type, EditorCommentsOptions options, string source)
    {
        var commands = new List<IEditCommand?>();
        IReadOnlyList<Selection> current = viewModel.GetSelections();
        // OrderBy is stable, like Monaco's Array.sort.
        List<SelectionInfo> selections = [.. current.Select((selection, index) => new SelectionInfo(selection, index))
            .OrderBy(s => s.Selection.Range, Comparer<Range>.Create(Range.CompareUsingStarts))];

        // Remove selections that would result in copying the same line
        SelectionInfo prev = selections[0];
        for (int i = 1; i < selections.Count; i++)
        {
            SelectionInfo curr = selections[i];
            if (prev.Selection.Range.EndLineNumber == curr.Selection.Range.StartLineNumber)
            {
                // these two selections would copy the same line
                if (prev.Index < curr.Index)
                {
                    // prev wins
                    curr.IgnoreFirstLine = true;
                }
                else
                {
                    // curr wins
                    prev.IgnoreFirstLine = true;
                    prev = curr;
                }
            }
        }

        foreach (SelectionInfo selection in selections)
        {
            commands.Add(new LineCommentCommand(
                viewModel.Model.LanguageConfigurationRegistry,
                selection.Selection,
                viewModel.Model.Options.IndentSize,
                type,
                options.InsertSpace,
                options.IgnoreEmptyLines,
                selection.IgnoreFirstLine));
        }

        Execute(viewModel, commands, source);
    }

    /// <summary>Runs a block comment command on every selection (Monaco: <c>BlockCommentAction.run</c>).</summary>
    public static void RunBlockComment(ViewModel viewModel, EditorCommentsOptions options, string source)
    {
        var commands = new List<IEditCommand?>();
        foreach (Selection selection in viewModel.GetSelections())
        {
            commands.Add(new BlockCommentCommand(selection, options.InsertSpace, viewModel.Model.LanguageConfigurationRegistry));
        }

        Execute(viewModel, commands, source);
    }

    private static void Execute(ViewModel viewModel, List<IEditCommand?> commands, string source)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(commands, source);
        viewModel.Model.PushStackElement();
    }
}
