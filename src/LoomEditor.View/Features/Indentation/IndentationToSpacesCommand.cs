// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Indentation;

/// <summary>The edits that convert the indentation of the whole model (Monaco: <c>getIndentationEditOperations</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class IndentationConversion
{
    public static void GetIndentationEditOperations(TextModel model, IEditOperationBuilder builder, int tabSize, bool tabsToSpaces)
    {
        if (model.LineCount == 1 && model.GetLineMaxColumn(1) == 1)
        {
            // Model is empty
            return;
        }

        string spaces = new(' ', tabSize);

        for (int lineNumber = 1, lineCount = model.LineCount; lineNumber <= lineCount; lineNumber++)
        {
            int lastIndentationColumn = model.GetLineFirstNonWhitespaceColumn(lineNumber);
            if (lastIndentationColumn == 0)
            {
                lastIndentationColumn = model.GetLineMaxColumn(lineNumber);
            }

            if (lastIndentationColumn == 1)
            {
                continue;
            }

            var originalIndentationRange = new Range(lineNumber, 1, lineNumber, lastIndentationColumn);
            string originalIndentation = model.GetValueInRange(originalIndentationRange);
            // A run of spaces has no case and no regular expression metacharacter: Replace gives Monaco's result.
            string newIndentation = tabsToSpaces
                ? originalIndentation.Replace("\t", spaces, StringComparison.Ordinal)
                : originalIndentation.Replace(spaces, "\t", StringComparison.Ordinal);

            builder.AddEditOperation(originalIndentationRange, newIndentation);
        }
    }
}

/// <summary>Converts the indentation of the whole model to spaces (Monaco: <c>IndentationToSpacesCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class IndentationToSpacesCommand(Selection selection, int tabSize) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        _selectionId = builder.TrackSelection(selection);
        IndentationConversion.GetIndentationEditOperations(model, builder, tabSize, true);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);
}
