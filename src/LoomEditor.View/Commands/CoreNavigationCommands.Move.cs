// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>The keyboard navigation commands of Monaco's <c>CoreNavigationCommands</c>.</summary>
/// <remarks>
/// Phase 3 cut points: <c>precondition</c>, <c>kbOpts</c> and <c>metadata</c> of the registrations are not ported
/// (keybindings are P3-39). The commands hold no state. Threading: UI thread only.
/// </remarks>
internal static partial class CoreNavigationCommands
{
    // Monaco: TextEditorSelectionSource.PROGRAMMATIC
    private const string ProgrammaticSource = "api";

    // Monaco: Constants.PAGE_SIZE_MARKER
    private const int PageSizeMarker = -1;

    private sealed class BaseMoveToCommand(string id, bool inSelectionMode) : CoreEditorCommand<MoveCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, MoveCommandArgs args)
        {
            viewModel.Model.PushStackElement();
            bool cursorStateChanged = viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                [CursorMoveCommands.MoveTo(viewModel, viewModel.GetPrimaryCursorState(), inSelectionMode, args.Position, args.ViewPosition)]);
            if (cursorStateChanged && args.RevealType != NavigationCommandRevealType.None)
            {
                viewModel.RevealAllCursors(args.Source, true, true);
            }
        }
    }

    public static readonly CoreEditorCommand<MoveCommandArgs> MoveTo = new BaseMoveToCommand("_moveTo", false);

    public static readonly CoreEditorCommand<MoveCommandArgs> MoveToSelect = new BaseMoveToCommand("_moveToSelect", true);

    private sealed class CursorMoveImpl() : CoreEditorCommand<CursorMoveRunArgs>("cursorMove")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, CursorMoveRunArgs args)
        {
            CursorMoveParsedArguments? parsed = Cursors.CursorMove.Parse(args.Arguments);
            if (parsed is null)
            {
                // illegal arguments
                return;
            }

            RunCursorMove(viewModel, args.Source, parsed);
        }

        private static void RunCursorMove(ViewModel viewModel, string? source, CursorMoveParsedArguments args)
        {
            // If noHistory is true, use PROGRAMMATIC source to prevent adding to navigation history
            string? effectiveSource = args.NoHistory ? ProgrammaticSource : source;

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                effectiveSource,
                CursorChangeReason.Explicit,
                Move(viewModel, viewModel.GetCursorStates(), args));
            viewModel.RevealAllCursors(effectiveSource, true);
        }

        private static List<PartialCursorState>? Move(ViewModel viewModel, List<CursorState> cursors, CursorMoveParsedArguments args)
        {
            bool inSelectionMode = args.Select;
            int value = args.Value;

            switch (args.Direction)
            {
                case CursorMoveDirection.Left:
                case CursorMoveDirection.Right:
                case CursorMoveDirection.Up:
                case CursorMoveDirection.Down:
                case CursorMoveDirection.PrevBlankLine:
                case CursorMoveDirection.NextBlankLine:
                case CursorMoveDirection.WrappedLineStart:
                case CursorMoveDirection.WrappedLineFirstNonWhitespaceCharacter:
                case CursorMoveDirection.WrappedLineColumnCenter:
                case CursorMoveDirection.WrappedLineEnd:
                case CursorMoveDirection.WrappedLineLastNonWhitespaceCharacter:
                    return CursorMoveCommands.SimpleMove(viewModel, cursors, args.Direction, inSelectionMode, value, args.Unit);

                case CursorMoveDirection.ViewPortTop:
                case CursorMoveDirection.ViewPortBottom:
                case CursorMoveDirection.ViewPortCenter:
                case CursorMoveDirection.ViewPortIfOutside:
                    return CursorMoveCommands.ViewportMove(viewModel, cursors, args.Direction, inSelectionMode, value);
                default:
                    return null;
            }
        }
    }

    public static readonly CoreEditorCommand<CursorMoveRunArgs> CursorMove = new CursorMoveImpl();

    private sealed class CursorMoveBasedCommand(string id, CursorMoveDirection direction, CursorMoveUnit unit, bool select, int value)
        : CoreEditorCommand<CursorMoveCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, CursorMoveCommandArgs dynamicArgs)
        {
            int moveValue = value;
            if (moveValue == PageSizeMarker)
            {
                // -1 is a marker for page size; Monaco: 'dynamicArgs.pageSize || viewModel.cursorConfig.pageSize'
                moveValue = dynamicArgs.PageSize is > 0 ? dynamicArgs.PageSize.Value : viewModel.CursorConfig.PageSize;
            }

            viewModel.Model.PushStackElement();
            viewModel.SetCursorStates(
                dynamicArgs.Source,
                CursorChangeReason.Explicit,
                CursorMoveCommands.SimpleMove(viewModel, viewModel.GetCursorStates(), direction, select, moveValue, unit));
            viewModel.RevealAllCursors(dynamicArgs.Source, true);
        }
    }

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorLeft =
        new CursorMoveBasedCommand("cursorLeft", CursorMoveDirection.Left, CursorMoveUnit.None, false, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorLeftSelect =
        new CursorMoveBasedCommand("cursorLeftSelect", CursorMoveDirection.Left, CursorMoveUnit.None, true, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorRight =
        new CursorMoveBasedCommand("cursorRight", CursorMoveDirection.Right, CursorMoveUnit.None, false, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorRightSelect =
        new CursorMoveBasedCommand("cursorRightSelect", CursorMoveDirection.Right, CursorMoveUnit.None, true, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorUp =
        new CursorMoveBasedCommand("cursorUp", CursorMoveDirection.Up, CursorMoveUnit.WrappedLine, false, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorUpSelect =
        new CursorMoveBasedCommand("cursorUpSelect", CursorMoveDirection.Up, CursorMoveUnit.WrappedLine, true, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorPageUp =
        new CursorMoveBasedCommand("cursorPageUp", CursorMoveDirection.Up, CursorMoveUnit.WrappedLine, false, PageSizeMarker);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorPageUpSelect =
        new CursorMoveBasedCommand("cursorPageUpSelect", CursorMoveDirection.Up, CursorMoveUnit.WrappedLine, true, PageSizeMarker);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorDown =
        new CursorMoveBasedCommand("cursorDown", CursorMoveDirection.Down, CursorMoveUnit.WrappedLine, false, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorDownSelect =
        new CursorMoveBasedCommand("cursorDownSelect", CursorMoveDirection.Down, CursorMoveUnit.WrappedLine, true, 1);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorPageDown =
        new CursorMoveBasedCommand("cursorPageDown", CursorMoveDirection.Down, CursorMoveUnit.WrappedLine, false, PageSizeMarker);

    public static readonly CoreEditorCommand<CursorMoveCommandArgs> CursorPageDownSelect =
        new CursorMoveBasedCommand("cursorPageDownSelect", CursorMoveDirection.Down, CursorMoveUnit.WrappedLine, true, PageSizeMarker);
}
