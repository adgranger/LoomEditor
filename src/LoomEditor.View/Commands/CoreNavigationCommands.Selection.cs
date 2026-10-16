// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

internal static partial class CoreNavigationCommands
{
    private sealed class CancelSelectionImpl() : CoreEditorCommand<BaseCommandArgs>("cancelSelection")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [CursorMoveCommands.CancelSelection(viewModel, viewModel.GetPrimaryCursorState())]);
            viewModel.RevealAllCursors(args.Source, true);
        }
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CancelSelection = new CancelSelectionImpl();

    private sealed class RemoveSecondaryCursorsImpl() : CoreEditorCommand<BaseCommandArgs>("removeSecondaryCursors")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [viewModel.GetPrimaryCursorState()]);
            viewModel.RevealAllCursors(args.Source, true);

            // Phase 3 cut point: Monaco announces "Removed secondary cursors" to screen readers (status()).
        }
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> RemoveSecondaryCursors = new RemoveSecondaryCursorsImpl();

    private sealed class SetSelectionImpl() : CoreEditorCommand<SetSelectionCommandArgs>("setSelection")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, SetSelectionCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [CursorState.FromModelSelection(args.Selection)]);
        }
    }

    public static readonly CoreEditorCommand<SetSelectionCommandArgs> SetSelection = new SetSelectionImpl();
}
