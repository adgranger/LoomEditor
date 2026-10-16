// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

internal static partial class CursorMoveCommands
{
    /// <summary>Moves relative to the viewport; returns <see langword="null"/> for a direction that is not a viewport move.</summary>
    public static List<PartialCursorState>? ViewportMove(ViewModel viewModel, IReadOnlyList<CursorState> cursors, CursorMoveDirection direction, bool inSelectionMode, int value)
    {
        Range visibleViewRange = viewModel.GetCompletelyVisibleViewRange();
        Range visibleModelRange = viewModel.CoordinatesConverter.ConvertViewRangeToModelRange(visibleViewRange);
        switch (direction)
        {
            case CursorMoveDirection.ViewPortTop:
            {
                // Move to the nth line start in the viewport (from the top)
                int modelLineNumber = FirstLineNumberInRange(ModelOf(viewModel), visibleModelRange, value);
                int modelColumn = viewModel.Model.GetLineFirstNonWhitespaceColumn(modelLineNumber);
                return [MoveToModelPosition(cursors[0], inSelectionMode, modelLineNumber, modelColumn)];
            }

            case CursorMoveDirection.ViewPortBottom:
            {
                // Move to the nth line start in the viewport (from the bottom)
                int modelLineNumber = LastLineNumberInRange(ModelOf(viewModel), visibleModelRange, value);
                int modelColumn = viewModel.Model.GetLineFirstNonWhitespaceColumn(modelLineNumber);
                return [MoveToModelPosition(cursors[0], inSelectionMode, modelLineNumber, modelColumn)];
            }

            case CursorMoveDirection.ViewPortCenter:
            {
                // Move to the line start in the viewport center
                int modelLineNumber = JsRound((visibleModelRange.StartLineNumber + visibleModelRange.EndLineNumber) / 2.0);
                int modelColumn = viewModel.Model.GetLineFirstNonWhitespaceColumn(modelLineNumber);
                return [MoveToModelPosition(cursors[0], inSelectionMode, modelLineNumber, modelColumn)];
            }

            case CursorMoveDirection.ViewPortIfOutside:
                // Move to a position inside the viewport
                return MapCursors(cursors, cursor => FindPositionInViewportIfOutside(viewModel, cursor, visibleViewRange, inSelectionMode));
            default:
                return null;
        }
    }

    public static PartialCursorState FindPositionInViewportIfOutside(ViewModel viewModel, CursorState cursor, Range visibleViewRange, bool inSelectionMode)
    {
        int viewLineNumber = cursor.ViewState.Position.LineNumber;

        if (visibleViewRange.StartLineNumber <= viewLineNumber && viewLineNumber <= visibleViewRange.EndLineNumber - 1)
        {
            // Nothing to do, cursor is in viewport
            return new CursorState(cursor.ModelState, cursor.ViewState);
        }

        int newViewLineNumber;
        if (viewLineNumber > visibleViewRange.EndLineNumber - 1)
        {
            newViewLineNumber = visibleViewRange.EndLineNumber - 1;
        }
        else if (viewLineNumber < visibleViewRange.StartLineNumber)
        {
            newViewLineNumber = visibleViewRange.StartLineNumber;
        }
        else
        {
            newViewLineNumber = viewLineNumber;
        }

        CursorPosition position = MoveOperations.Vertical(
            viewModel.CursorConfig,
            viewModel,
            viewLineNumber,
            cursor.ViewState.Position.Column,
            cursor.ViewState.LeftoverVisibleColumns,
            newViewLineNumber,
            false);
        return CursorState.FromViewState(cursor.ViewState.Move(inSelectionMode, position.LineNumber, position.Column, position.LeftoverVisibleColumns));
    }

    /// <summary>Find the nth line start included in the range (from the start).</summary>
    private static int FirstLineNumberInRange(TextModelCursorSimpleModel model, Range range, int count)
    {
        int startLineNumber = range.StartLineNumber;
        if (range.StartColumn != model.GetLineMinColumn(startLineNumber))
        {
            // Move on to the second line if the first line start is not included in the range
            startLineNumber++;
        }

        return Math.Min(range.EndLineNumber, startLineNumber + count - 1);
    }

    /// <summary>Find the nth line start included in the range (from the end).</summary>
    private static int LastLineNumberInRange(TextModelCursorSimpleModel model, Range range, int count)
    {
        int startLineNumber = range.StartLineNumber;
        if (range.StartColumn != model.GetLineMinColumn(startLineNumber))
        {
            // Move on to the second line if the first line start is not included in the range
            startLineNumber++;
        }

        return Math.Max(startLineNumber, range.EndLineNumber - count + 1);
    }
}
