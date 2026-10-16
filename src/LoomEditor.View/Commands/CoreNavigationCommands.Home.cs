// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

internal static partial class CoreNavigationCommands
{
    // Runs a move of every cursor: Monaco repeats this body in each command class.
    private static void RunCursorStatesMove(ViewModel viewModel, string? source, Func<List<CursorState>, List<PartialCursorState>> compute)
    {
        viewModel.Model.PushStackElement();
        viewModel.SetCursorStates(source, CursorChangeReason.Explicit, compute(viewModel.GetCursorStates()));
        viewModel.RevealAllCursors(source, true);
    }

    private sealed class HomeCommand(string id, bool inSelectionMode) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, cursors => CursorMoveCommands.MoveToBeginningOfLine(viewModel, cursors, inSelectionMode));
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorHome = new HomeCommand("cursorHome", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorHomeSelect = new HomeCommand("cursorHomeSelect", true);

    private sealed class LineStartCommand(string id, bool inSelectionMode) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, Exec);

        private List<PartialCursorState> Exec(List<CursorState> cursors)
        {
            var result = new List<PartialCursorState>(cursors.Count);
            foreach (CursorState cursor in cursors)
            {
                int lineNumber = cursor.ModelState.Position.LineNumber;
                result.Add(CursorState.FromModelState(cursor.ModelState.Move(inSelectionMode, lineNumber, 1, 0)));
            }

            return result;
        }
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorLineStart = new LineStartCommand("cursorLineStart", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorLineStartSelect = new LineStartCommand("cursorLineStartSelect", true);

    private sealed class EndCommand(string id, bool inSelectionMode) : CoreEditorCommand<EndCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, EndCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, cursors => CursorMoveCommands.MoveToEndOfLine(viewModel, cursors, inSelectionMode, args.Sticky));
    }

    public static readonly CoreEditorCommand<EndCommandArgs> CursorEnd = new EndCommand("cursorEnd", false);

    public static readonly CoreEditorCommand<EndCommandArgs> CursorEndSelect = new EndCommand("cursorEndSelect", true);

    private sealed class LineEndCommand(string id, bool inSelectionMode) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, cursors => Exec(viewModel, cursors));

        private List<PartialCursorState> Exec(ViewModel viewModel, List<CursorState> cursors)
        {
            var result = new List<PartialCursorState>(cursors.Count);
            foreach (CursorState cursor in cursors)
            {
                int lineNumber = cursor.ModelState.Position.LineNumber;
                int maxColumn = viewModel.Model.GetLineMaxColumn(lineNumber);
                result.Add(CursorState.FromModelState(cursor.ModelState.Move(inSelectionMode, lineNumber, maxColumn, 0)));
            }

            return result;
        }
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorLineEnd = new LineEndCommand("cursorLineEnd", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorLineEndSelect = new LineEndCommand("cursorLineEndSelect", true);

    private sealed class TopCommand(string id, bool inSelectionMode) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, cursors => CursorMoveCommands.MoveToBeginningOfBuffer(viewModel, cursors, inSelectionMode));
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorTop = new TopCommand("cursorTop", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorTopSelect = new TopCommand("cursorTopSelect", true);

    private sealed class BottomCommand(string id, bool inSelectionMode) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            RunCursorStatesMove(viewModel, args.Source, cursors => CursorMoveCommands.MoveToEndOfBuffer(viewModel, cursors, inSelectionMode));
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorBottom = new BottomCommand("cursorBottom", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorBottomSelect = new BottomCommand("cursorBottomSelect", true);
}
