// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts (AutoIndentOnPasteCommand)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Indentation;

/// <summary>Applies the indentation edits computed after a paste and keeps the selection where it was.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutoIndentOnPasteCommand(IReadOnlyList<(Range Range, string Text)> edits, Selection initialSelection) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        foreach ((Range range, string text) in edits)
        {
            builder.AddEditOperation(range, text);
        }

        bool selectionIsSet = false;
        if (edits.Count == 1 && initialSelection.IsEmpty)
        {
            Range editRange = edits[0].Range;
            if (editRange.StartColumn == initialSelection.Range.EndColumn &&
                editRange.StartLineNumber == initialSelection.Range.EndLineNumber)
            {
                selectionIsSet = true;
                _selectionId = builder.TrackSelection(initialSelection, true);
            }
            else if (editRange.EndColumn == initialSelection.Range.StartColumn &&
                editRange.EndLineNumber == initialSelection.Range.StartLineNumber)
            {
                selectionIsSet = true;
                _selectionId = builder.TrackSelection(initialSelection, false);
            }
        }

        if (!selectionIsSet)
        {
            _selectionId = builder.TrackSelection(initialSelection);
        }
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);
}
