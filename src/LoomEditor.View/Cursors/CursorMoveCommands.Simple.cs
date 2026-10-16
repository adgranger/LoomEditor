// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

internal static partial class CursorMoveCommands
{
    /// <summary>Monaco's <c>Math.round</c> (a .5 rounds up) for the non-negative values the moves compute.</summary>
    private static int JsRound(double value) => (int)Math.Floor(value + 0.5);

    /// <summary>Moves in a direction by a unit; returns <see langword="null"/> for a direction that is not a simple move.</summary>
    public static List<PartialCursorState>? SimpleMove(ViewModel viewModel, IReadOnlyList<CursorState> cursors, CursorMoveDirection direction, bool inSelectionMode, int value, CursorMoveUnit unit)
    {
        switch (direction)
        {
            case CursorMoveDirection.Left:
                // Move left by half the current line length, or by `value` columns
                return unit == CursorMoveUnit.HalfLine
                    ? MoveHalfLineLeft(viewModel, cursors, inSelectionMode)
                    : MoveLeft(viewModel, cursors, inSelectionMode, value);
            case CursorMoveDirection.Right:
                return unit == CursorMoveUnit.HalfLine
                    ? MoveHalfLineRight(viewModel, cursors, inSelectionMode)
                    : MoveRight(viewModel, cursors, inSelectionMode, value);
            case CursorMoveDirection.Up:
                return unit switch
                {
                    // Move up by view lines
                    CursorMoveUnit.WrappedLine => MoveUpByViewLines(viewModel, cursors, inSelectionMode, value),

                    // Move up by model lines, skipping over folded regions
                    CursorMoveUnit.FoldedLine => MoveUpByFoldedLines(viewModel, cursors, inSelectionMode, value),

                    // Move up by model lines
                    _ => MoveUpByModelLines(viewModel, cursors, inSelectionMode, value),
                };
            case CursorMoveDirection.Down:
                return unit switch
                {
                    CursorMoveUnit.WrappedLine => MoveDownByViewLines(viewModel, cursors, inSelectionMode, value),
                    CursorMoveUnit.FoldedLine => MoveDownByFoldedLines(viewModel, cursors, inSelectionMode, value),
                    _ => MoveDownByModelLines(viewModel, cursors, inSelectionMode, value),
                };
            case CursorMoveDirection.PrevBlankLine:
                return unit == CursorMoveUnit.WrappedLine
                    ? MapCursors(cursors, c => CursorState.FromViewState(MoveOperations.MoveToPrevBlankLine(viewModel.CursorConfig, viewModel, c.ViewState, inSelectionMode)))
                    : MapCursors(cursors, c => CursorState.FromModelState(MoveOperations.MoveToPrevBlankLine(viewModel.CursorConfig, ModelOf(viewModel), c.ModelState, inSelectionMode)));
            case CursorMoveDirection.NextBlankLine:
                return unit == CursorMoveUnit.WrappedLine
                    ? MapCursors(cursors, c => CursorState.FromViewState(MoveOperations.MoveToNextBlankLine(viewModel.CursorConfig, viewModel, c.ViewState, inSelectionMode)))
                    : MapCursors(cursors, c => CursorState.FromModelState(MoveOperations.MoveToNextBlankLine(viewModel.CursorConfig, ModelOf(viewModel), c.ModelState, inSelectionMode)));
            case CursorMoveDirection.WrappedLineStart:
                // Move to the beginning of the current view line
                return MapCursors(cursors, c => MoveToViewPosition(c, inSelectionMode, c.ViewState.Position.LineNumber, viewModel.GetLineMinColumn(c.ViewState.Position.LineNumber)));
            case CursorMoveDirection.WrappedLineFirstNonWhitespaceCharacter:
                // Move to the first non-whitespace column of the current view line
                return MapCursors(cursors, c => MoveToViewPosition(c, inSelectionMode, c.ViewState.Position.LineNumber, viewModel.GetLineFirstNonWhitespaceColumn(c.ViewState.Position.LineNumber)));
            case CursorMoveDirection.WrappedLineColumnCenter:
                // Move to the "center" of the current view line
                return MapCursors(cursors, c =>
                {
                    int viewLineNumber = c.ViewState.Position.LineNumber;
                    int viewColumn = JsRound((viewModel.GetLineMaxColumn(viewLineNumber) + viewModel.GetLineMinColumn(viewLineNumber)) / 2.0);
                    return MoveToViewPosition(c, inSelectionMode, viewLineNumber, viewColumn);
                });
            case CursorMoveDirection.WrappedLineEnd:
                // Move to the end of the current view line
                return MapCursors(cursors, c => MoveToViewPosition(c, inSelectionMode, c.ViewState.Position.LineNumber, viewModel.GetLineMaxColumn(c.ViewState.Position.LineNumber)));
            case CursorMoveDirection.WrappedLineLastNonWhitespaceCharacter:
                // Move to the last non-whitespace column of the current view line
                return MapCursors(cursors, c => MoveToViewPosition(c, inSelectionMode, c.ViewState.Position.LineNumber, viewModel.GetLineLastNonWhitespaceColumn(c.ViewState.Position.LineNumber)));
            default:
                return null;
        }
    }

    private static List<PartialCursorState> MapCursors(IReadOnlyList<CursorState> cursors, Func<CursorState, PartialCursorState> map)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            result.Add(map(cursor));
        }

        return result;
    }

    private static List<PartialCursorState> MoveLeft(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int noOfColumns) =>
        MapCursors(cursors, cursor =>
        {
            // Phase 3: the text direction is always LTR, so the RTL swap is never taken.
            bool isRtl = ViewModel.GetTextDirection(cursor.ViewState.Position.LineNumber) == TextDirection.RTL;

            return CursorState.FromViewState(
                isRtl
                    ? MoveOperations.MoveRight(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, noOfColumns)
                    : MoveOperations.MoveLeft(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, noOfColumns));
        });

    private static List<PartialCursorState> MoveHalfLineLeft(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode) =>
        MapCursors(cursors, cursor =>
        {
            int viewLineNumber = cursor.ViewState.Position.LineNumber;
            int halfLine = JsRound(viewModel.GetLineLength(viewLineNumber) / 2.0);
            return CursorState.FromViewState(MoveOperations.MoveLeft(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, halfLine));
        });

    private static List<PartialCursorState> MoveRight(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int noOfColumns) =>
        MapCursors(cursors, cursor =>
        {
            bool isRtl = ViewModel.GetTextDirection(cursor.ViewState.Position.LineNumber) == TextDirection.RTL;

            return CursorState.FromViewState(
                isRtl
                    ? MoveOperations.MoveLeft(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, noOfColumns)
                    : MoveOperations.MoveRight(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, noOfColumns));
        });

    private static List<PartialCursorState> MoveHalfLineRight(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode) =>
        MapCursors(cursors, cursor =>
        {
            int viewLineNumber = cursor.ViewState.Position.LineNumber;
            int halfLine = JsRound(viewModel.GetLineLength(viewLineNumber) / 2.0);
            return CursorState.FromViewState(MoveOperations.MoveRight(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, halfLine));
        });

    private static List<PartialCursorState> MoveDownByViewLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int linesCount) =>
        MapCursors(cursors, cursor => CursorState.FromViewState(MoveOperations.MoveDown(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, linesCount)));

    private static List<PartialCursorState> MoveDownByModelLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int linesCount) =>
        MapCursors(cursors, cursor => CursorState.FromModelState(MoveOperations.MoveDown(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode, linesCount)));

    private static List<PartialCursorState> MoveUpByViewLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int linesCount) =>
        MapCursors(cursors, cursor => CursorState.FromViewState(MoveOperations.MoveUp(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, linesCount)));

    private static List<PartialCursorState> MoveUpByModelLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int linesCount) =>
        MapCursors(cursors, cursor => CursorState.FromModelState(MoveOperations.MoveUp(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode, linesCount)));

    private static List<PartialCursorState> MoveDownByFoldedLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int count)
    {
        TextModelCursorSimpleModel model = ModelOf(viewModel);
        int lineCount = model.GetLineCount();
        IReadOnlyList<Range> hiddenAreas = ViewModel.GetHiddenAreas();

        return MapCursors(cursors, cursor =>
        {
            int startLine = cursor.ModelState.HasSelection() && !inSelectionMode
                ? cursor.ModelState.Selection.Range.EndLineNumber
                : cursor.ModelState.Position.LineNumber;

            int targetLine = TargetFoldedDown(startLine, count, hiddenAreas, lineCount);
            int delta = targetLine - startLine;
            if (delta == 0)
            {
                return CursorState.FromModelState(cursor.ModelState);
            }

            return CursorState.FromModelState(MoveOperations.MoveDown(viewModel.CursorConfig, model, cursor.ModelState, inSelectionMode, delta));
        });
    }

    private static List<PartialCursorState> MoveUpByFoldedLines(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, int count)
    {
        TextModelCursorSimpleModel model = ModelOf(viewModel);
        IReadOnlyList<Range> hiddenAreas = ViewModel.GetHiddenAreas();

        return MapCursors(cursors, cursor =>
        {
            int startLine = cursor.ModelState.HasSelection() && !inSelectionMode
                ? cursor.ModelState.Selection.Range.StartLineNumber
                : cursor.ModelState.Position.LineNumber;

            int targetLine = TargetFoldedUp(startLine, count, hiddenAreas);
            int delta = startLine - targetLine;
            if (delta == 0)
            {
                return CursorState.FromModelState(cursor.ModelState);
            }

            return CursorState.FromModelState(MoveOperations.MoveUp(viewModel.CursorConfig, model, cursor.ModelState, inSelectionMode, delta));
        });
    }

    // Compute the target line after moving `count` steps downward from `startLine`,
    // treating each folded region as a single step.
    private static int TargetFoldedDown(int startLine, int count, IReadOnlyList<Range> hiddenAreas, int lineCount)
    {
        int line = startLine;
        int i = 0;

        while (i < hiddenAreas.Count && hiddenAreas[i].EndLineNumber < line + 1)
        {
            i++;
        }

        for (int step = 0; step < count; step++)
        {
            if (line >= lineCount)
            {
                return lineCount;
            }

            int candidate = line + 1;
            while (i < hiddenAreas.Count && hiddenAreas[i].EndLineNumber < candidate)
            {
                i++;
            }

            if (i < hiddenAreas.Count && hiddenAreas[i].StartLineNumber <= candidate)
            {
                candidate = hiddenAreas[i].EndLineNumber + 1;
            }

            if (candidate > lineCount)
            {
                // The next visible line does not exist (e.g. a fold reaches EOF).
                return line;
            }

            line = candidate;
        }

        return line;
    }

    // Compute the target line after moving `count` steps upward from `startLine`,
    // treating each folded region as a single step.
    private static int TargetFoldedUp(int startLine, int count, IReadOnlyList<Range> hiddenAreas)
    {
        int line = startLine;
        int i = hiddenAreas.Count - 1;

        while (i >= 0 && hiddenAreas[i].StartLineNumber > line - 1)
        {
            i--;
        }

        for (int step = 0; step < count; step++)
        {
            if (line <= 1)
            {
                return 1;
            }

            int candidate = line - 1;
            while (i >= 0 && hiddenAreas[i].StartLineNumber > candidate)
            {
                i--;
            }

            if (i >= 0 && hiddenAreas[i].EndLineNumber >= candidate)
            {
                candidate = hiddenAreas[i].StartLineNumber - 1;
            }

            if (candidate < 1)
            {
                // The previous visible line does not exist (e.g. a fold reaches BOF).
                return line;
            }

            line = candidate;
        }

        return line;
    }
}
