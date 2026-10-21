// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Indentation;

/// <summary>What the indentation commands do to an editor.</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class IndentationActions
{
    /// <summary>Converts the indentation to spaces (Monaco: <c>IndentationToSpacesAction.run</c>).</summary>
    public static void IndentationToSpaces(ViewModel viewModel)
    {
        Convert(viewModel, IndentationCommands.IndentationToSpaces, new IndentationToSpacesCommand(viewModel.GetSelection(), viewModel.Model.Options.TabSize), true);
    }

    /// <summary>Converts the indentation to tabs (Monaco: <c>IndentationToTabsAction.run</c>).</summary>
    public static void IndentationToTabs(ViewModel viewModel)
    {
        Convert(viewModel, IndentationCommands.IndentationToTabs, new IndentationToTabsCommand(viewModel.GetSelection(), viewModel.Model.Options.TabSize), false);
    }

    /// <summary>
    /// Applies a tab size chosen by the user (Monaco: the callback of the quick pick in <c>ChangeIndentationSizeAction.run</c>).
    /// </summary>
    public static void ChangeIndentationSize(ViewModel viewModel, int tabSize, bool insertSpaces, bool displaySizeOnly)
    {
        viewModel.Model.UpdateOptions(displaySizeOnly
            ? new TextModelUpdateOptions(TabSize: tabSize)
            : new TextModelUpdateOptions(TabSize: tabSize, IndentSize: tabSize, InsertSpaces: insertSpaces));
    }

    /// <summary>
    /// The registered size commands have no quick pick: they apply the item it pre-selects, <c>min(tabSize - 1, 7) + 1</c>
    /// (Monaco: <c>autoFocusIndex</c>), as if the user pressed Enter.
    /// </summary>
    public static void ChangeIndentationSizeToPreselected(ViewModel viewModel, bool insertSpaces, bool displaySizeOnly) =>
        ChangeIndentationSize(viewModel, Math.Min(viewModel.Model.Options.TabSize - 1, 7) + 1, insertSpaces, displaySizeOnly);

    /// <summary>Detects the indentation from the content (Monaco: <c>DetectIndentation.run</c>).</summary>
    public static void DetectIndentation(ViewModel viewModel) =>
        viewModel.Model.DetectIndentation(TextModelOptions.Default.InsertSpaces, TextModelOptions.Default.TabSize);

    /// <summary>Re-indents all the lines (Monaco: <c>ReindentLinesAction.run</c>).</summary>
    public static void ReindentLines(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;
        Execute(viewModel, IndentationCommands.ReindentLines,
            ReindentOperations.GetReindentEditOperations(model, model.LanguageConfigurationRegistry, 1, model.LineCount));
    }

    /// <summary>Re-indents the selected lines (Monaco: <c>ReindentSelectedLinesAction.run</c>).</summary>
    public static void ReindentSelectedLines(ViewModel viewModel)
    {
        TextModel model = viewModel.Model;
        var edits = new List<IdentifiedEditOperation>();

        foreach (Selection selection in viewModel.GetSelections())
        {
            int startLineNumber = selection.Range.StartLineNumber;
            int endLineNumber = selection.Range.EndLineNumber;

            if (startLineNumber != endLineNumber && selection.Range.EndColumn == 1)
            {
                endLineNumber--;
            }

            if (startLineNumber == 1)
            {
                if (startLineNumber == endLineNumber)
                {
                    continue;
                }
            }
            else
            {
                startLineNumber--;
            }

            edits.AddRange(ReindentOperations.GetReindentEditOperations(model, model.LanguageConfigurationRegistry, startLineNumber, endLineNumber));
        }

        Execute(viewModel, IndentationCommands.ReindentSelectedLines, edits);
    }

    private static void Convert(ViewModel viewModel, string source, IEditCommand command, bool insertSpaces)
    {
        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands([command], source);
        viewModel.Model.PushStackElement();

        viewModel.Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: insertSpaces));
    }

    private static void Execute(ViewModel viewModel, string source, List<IdentifiedEditOperation> edits)
    {
        if (edits.Count == 0)
        {
            return;
        }

        viewModel.Model.PushStackElement();
        viewModel.ExecuteEdits(source, edits, _ => null);
        viewModel.Model.PushStackElement();
    }
}
