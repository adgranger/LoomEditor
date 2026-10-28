// Ported from microsoft/vscode: src/vs/editor/contrib/caretOperations/browser/caretOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.CaretOperations;

/// <summary>Adds the "move selected text left / right" commands (no default key, as in Monaco).</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class CaretOperationsFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.caretOperations";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        void Register(string id, bool left) =>
            // Monaco: precondition `writable`.
            _registrations.Add(context.Commands.Register(new EditorCommand(id, () => Move(viewModel, id, left), () => !viewModel.CursorConfig.ReadOnly)));

        Register(CaretOperationsCommands.MoveCaretLeft, true);
        Register(CaretOperationsCommands.MoveCaretRight, false);
    }

    private static void Move(ViewModel viewModel, string id, bool left)
    {
        var commands = new List<IEditCommand?>();
        foreach (Selection selection in viewModel.GetSelections())
        {
            commands.Add(new MoveCaretCommand(selection, left));
        }

        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(commands, id);
        viewModel.Model.PushStackElement();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
    }
}
