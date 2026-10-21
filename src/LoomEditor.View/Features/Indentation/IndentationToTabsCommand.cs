// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Indentation;

/// <summary>Converts the indentation of the whole model to tabs (Monaco: <c>IndentationToTabsCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class IndentationToTabsCommand(Selection selection, int tabSize) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        _selectionId = builder.TrackSelection(selection);
        IndentationConversion.GetIndentationEditOperations(model, builder, tabSize, false);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);
}
