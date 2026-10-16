// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>Monaco: <c>CoreNavigationCommands.CreateCursorCommandOptions</c>.</summary>
internal sealed record CreateCursorCommandArgs(
    Position Position,
    Position? ViewPosition = null,
    bool WholeLine = false,
    string? Source = null) : BaseCommandArgs(Source);

// The mouse commands. They need a position, so they are not registered in the CommandRegistry (no key is bound to them
// in Monaco either): `ViewController` calls them directly.
internal static partial class CoreNavigationCommands
{
    private sealed class CreateCursorImpl() : CoreEditorCommand<CreateCursorCommandArgs>("createCursor")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, CreateCursorCommandArgs args)
        {
            PartialCursorState newState = args.WholeLine
                ? CursorMoveCommands.Line(viewModel, viewModel.GetPrimaryCursorState(), false, args.Position, args.ViewPosition)
                : CursorMoveCommands.MoveTo(viewModel, viewModel.GetPrimaryCursorState(), false, args.Position, args.ViewPosition);

            List<PartialCursorState> states = [.. viewModel.GetCursorStates()];

            // Check if we should remove a cursor (sort of like a toggle).
            if (states.Count > 1)
            {
                Position? newModelPosition = newState.ModelState?.Position;
                Position? newViewPosition = newState.ViewState?.Position;

                for (int i = 0; i < states.Count; i++)
                {
                    PartialCursorState state = states[i];

                    if (newModelPosition is { } modelPosition && !state.ModelState!.Selection.Range.Contains(modelPosition))
                    {
                        continue;
                    }

                    if (newViewPosition is { } viewPosition && !state.ViewState!.Selection.Range.Contains(viewPosition))
                    {
                        continue;
                    }

                    // => Remove the cursor
                    states.RemoveAt(i);

                    viewModel.Model.PushStackElement();
                    viewModel.SetCursorStates(args.Source, CursorChangeReason.Explicit, states);
                    return;
                }
            }

            // => Add the new cursor
            states.Add(newState);

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(args.Source, CursorChangeReason.Explicit, states);
        }
    }

    public static readonly CoreEditorCommand<CreateCursorCommandArgs> CreateCursor = new CreateCursorImpl();

    private sealed class LastCursorMoveToSelectImpl() : CoreEditorCommand<MoveCommandArgs>("_lastCursorMoveToSelect")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            int lastAddedCursorIndex = viewModel.GetLastAddedCursorIndex();

            List<CursorState> states = viewModel.GetCursorStates();
            List<PartialCursorState> newStates = [.. states];
            newStates[lastAddedCursorIndex] = CursorMoveCommands.MoveTo(viewModel, states[lastAddedCursorIndex], true, args.Position, args.ViewPosition);

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(args.Source, CursorChangeReason.Explicit, newStates);
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> LastCursorMoveToSelect = new LastCursorMoveToSelectImpl();

    private sealed class WordCommand(string id, bool inSelectionMode) : CoreEditorCommand<MoveCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [CursorMoveCommands.Word(viewModel, viewModel.GetPrimaryCursorState(), inSelectionMode, args.Position)]);
            if (args.RevealType != NavigationCommandRevealType.None)
            {
                viewModel.RevealAllCursors(args.Source, true, true);
            }
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> WordSelect = new WordCommand("_wordSelect", false);

    public static readonly CoreEditorCommand<MoveCommandArgs> WordSelectDrag = new WordCommand("_wordSelectDrag", true);

    private sealed class LastCursorWordSelectImpl() : CoreEditorCommand<MoveCommandArgs>("lastCursorWordSelect")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            int lastAddedCursorIndex = viewModel.GetLastAddedCursorIndex();

            List<CursorState> states = viewModel.GetCursorStates();
            List<PartialCursorState> newStates = [.. states];
            CursorState lastAddedState = states[lastAddedCursorIndex];
            newStates[lastAddedCursorIndex] = CursorMoveCommands.Word(viewModel, lastAddedState, lastAddedState.ModelState.HasSelection(), args.Position);

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(args.Source, CursorChangeReason.Explicit, newStates);
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> LastCursorWordSelect = new LastCursorWordSelectImpl();

    private sealed class LineCommand(string id, bool inSelectionMode) : CoreEditorCommand<MoveCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [CursorMoveCommands.Line(viewModel, viewModel.GetPrimaryCursorState(), inSelectionMode, args.Position, args.ViewPosition)]);
            if (args.RevealType != NavigationCommandRevealType.None)
            {
                viewModel.RevealAllCursors(args.Source, false, true);
            }
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> LineSelect = new LineCommand("_lineSelect", false);

    public static readonly CoreEditorCommand<MoveCommandArgs> LineSelectDrag = new LineCommand("_lineSelectDrag", true);

    private sealed class LastCursorLineCommand(string id, bool inSelectionMode) : CoreEditorCommand<MoveCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            int lastAddedCursorIndex = viewModel.GetLastAddedCursorIndex();

            List<CursorState> states = viewModel.GetCursorStates();
            List<PartialCursorState> newStates = [.. states];
            newStates[lastAddedCursorIndex] = CursorMoveCommands.Line(viewModel, states[lastAddedCursorIndex], inSelectionMode, args.Position, args.ViewPosition);

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(args.Source, CursorChangeReason.Explicit, newStates);
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> LastCursorLineSelect = new LastCursorLineCommand("lastCursorLineSelect", false);

    public static readonly CoreEditorCommand<MoveCommandArgs> LastCursorLineSelectDrag = new LastCursorLineCommand("lastCursorLineSelectDrag", true);

    // Monaco: `SelectAll` runs `SelectAllCommand` ("editor.action.selectAll", owned by SelectAllFeature here);
    // `runCoreEditorCommand` is what `ViewController._selectAll` calls.
    private sealed class SelectAllImpl() : CoreEditorCommand<BaseCommandArgs>("editor.action.selectAll")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                "keyboard",
                CursorChangeReason.Explicit,
                [CursorMoveCommands.SelectAll(viewModel, viewModel.GetPrimaryCursorState())]);
        }
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> SelectAll = new SelectAllImpl();
}
