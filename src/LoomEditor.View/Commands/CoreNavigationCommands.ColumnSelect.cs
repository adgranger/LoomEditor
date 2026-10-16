// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>Monaco: <c>CoreNavigationCommands.ColumnSelectCommandOptions</c>.</summary>
internal sealed record ColumnSelectCommandArgs(
    Position Position,
    Position ViewPosition,
    int MouseColumn,
    bool DoColumnSelect,
    string? Source = null) : BaseCommandArgs(Source);

internal static partial class CoreNavigationCommands
{
    private abstract class ColumnSelectCommand<TArgs>(string id) : CoreEditorCommand<TArgs>(id)
        where TArgs : BaseCommandArgs
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, TArgs args)
        {
            viewModel.Model.PushStackElement();
            ColumnSelectResult result = GetColumnSelectResult(viewModel, viewModel.GetPrimaryCursorState(), viewModel.GetCursorColumnSelectData(), args);
            viewModel.SetCursorStates(
                args.Source,
                CursorChangeReason.Explicit,
                result.ViewStates.Select(viewState => (PartialCursorState)CursorState.FromViewState(viewState)).ToList());
            viewModel.SetCursorColumnSelectData(new ColumnSelectData(
                IsReal: true,
                FromViewLineNumber: result.FromLineNumber,
                FromViewVisualColumn: result.FromVisualColumn,
                ToViewLineNumber: result.ToLineNumber,
                ToViewVisualColumn: result.ToVisualColumn));
            if (result.Reversed)
            {
                viewModel.RevealTopMostCursor(args.Source);
            }
            else
            {
                viewModel.RevealBottomMostCursor(args.Source);
            }
        }

        // Monaco returns null for invalid arguments; the C# arguments cannot be undefined, so it never does.
        protected abstract ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, TArgs args);
    }

    private sealed class ColumnSelectImpl() : ColumnSelectCommand<ColumnSelectCommandArgs>("columnSelect")
    {
        protected override ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, ColumnSelectCommandArgs args)
        {
            // validate `args`
            Position validatedPosition = viewModel.Model.ValidatePosition(args.Position);
            Position validatedViewPosition = viewModel.CoordinatesConverter.ValidateViewPosition(args.ViewPosition, validatedPosition);

            int fromViewLineNumber = args.DoColumnSelect ? prevColumnSelectData.FromViewLineNumber : validatedViewPosition.LineNumber;
            int fromViewVisualColumn = args.DoColumnSelect ? prevColumnSelectData.FromViewVisualColumn : args.MouseColumn - 1;
            return ColumnSelection.ColumnSelect(
                viewModel.CursorConfig,
                viewModel,
                fromViewLineNumber,
                fromViewVisualColumn,
                validatedViewPosition.LineNumber,
                args.MouseColumn - 1);
        }
    }

    public static readonly CoreEditorCommand<ColumnSelectCommandArgs> ColumnSelect = new ColumnSelectImpl();

    private sealed class ColumnSelectLeftImpl() : ColumnSelectCommand<BaseCommandArgs>("cursorColumnSelectLeft")
    {
        protected override ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, BaseCommandArgs args) =>
            ColumnSelection.ColumnSelectLeft(viewModel.CursorConfig, viewModel, prevColumnSelectData);
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectLeft = new ColumnSelectLeftImpl();

    private sealed class ColumnSelectRightImpl() : ColumnSelectCommand<BaseCommandArgs>("cursorColumnSelectRight")
    {
        protected override ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, BaseCommandArgs args) =>
            ColumnSelection.ColumnSelectRight(viewModel.CursorConfig, viewModel, prevColumnSelectData);
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectRight = new ColumnSelectRightImpl();

    private sealed class ColumnSelectUpCommand(string id, bool isPaged) : ColumnSelectCommand<BaseCommandArgs>(id)
    {
        protected override ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, BaseCommandArgs args) =>
            ColumnSelection.ColumnSelectUp(viewModel.CursorConfig, viewModel, prevColumnSelectData, isPaged);
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectUp = new ColumnSelectUpCommand("cursorColumnSelectUp", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectPageUp = new ColumnSelectUpCommand("cursorColumnSelectPageUp", true);

    private sealed class ColumnSelectDownCommand(string id, bool isPaged) : ColumnSelectCommand<BaseCommandArgs>(id)
    {
        protected override ColumnSelectResult GetColumnSelectResult(ViewModel viewModel, CursorState primary, ColumnSelectData prevColumnSelectData, BaseCommandArgs args) =>
            ColumnSelection.ColumnSelectDown(viewModel.CursorConfig, viewModel, prevColumnSelectData, isPaged);
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectDown = new ColumnSelectDownCommand("cursorColumnSelectDown", false);

    public static readonly CoreEditorCommand<BaseCommandArgs> CursorColumnSelectPageDown = new ColumnSelectDownCommand("cursorColumnSelectPageDown", true);
}
