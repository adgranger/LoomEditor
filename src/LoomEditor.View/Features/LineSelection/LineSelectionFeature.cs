// Ported from microsoft/vscode: src/vs/editor/contrib/lineSelection/browser/lineSelection.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Features.LineSelection;

/// <summary>Registers <see cref="LineSelectionCommands.ExpandLineSelection"/> (Monaco: <c>ExpandLineSelectionAction</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class LineSelectionFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.lineSelection";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetViewModel() is not { } viewModel)
        {
            return;
        }

        _registration = context.Commands.Register(new EditorCommand(LineSelectionCommands.ExpandLineSelection, () =>
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                LineSelectionCommands.ExpandLineSelection,
                CursorChangeReason.Explicit,
                CursorMoveCommands.ExpandLineSelection(viewModel, viewModel.GetCursorStates()));
            viewModel.RevealAllCursors(LineSelectionCommands.ExpandLineSelection, true);
        }));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
