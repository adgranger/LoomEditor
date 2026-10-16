// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

/// <summary>Turns a user intention (go to the line start, add a cursor below, select a line...) into new cursor states.</summary>
/// <remarks>
/// <para>
/// Line, buffer, select all, add cursor and move to (this file); <c>simpleMove</c> and
/// <c>viewportMove</c> are in the other partial files; <c>word</c> is here too.
/// </para>
/// <para>
/// Monaco passes an <c>IViewModel</c> that is also its own <c>ICursorSimpleModel</c> and exposes <c>model</c>. Here the
/// functions take the concrete <see cref="ViewModel"/>, and the model side is read through a
/// <see cref="TextModelCursorSimpleModel"/>.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal static partial class CursorMoveCommands
{
    private static TextModelCursorSimpleModel ModelOf(ViewModel viewModel) => new(viewModel.Model);

    public static List<PartialCursorState> AddCursorDown(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool useLogicalLine)
    {
        var result = new List<PartialCursorState>(cursors.Count * 2);
        foreach (CursorState cursor in cursors)
        {
            result.Add(new CursorState(cursor.ModelState, cursor.ViewState));
            result.Add(useLogicalLine
                ? CursorState.FromModelState(MoveOperations.TranslateDown(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState))
                : CursorState.FromViewState(MoveOperations.TranslateDown(viewModel.CursorConfig, viewModel, cursor.ViewState)));
        }

        return result;
    }

    public static List<PartialCursorState> AddCursorUp(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool useLogicalLine)
    {
        var result = new List<PartialCursorState>(cursors.Count * 2);
        foreach (CursorState cursor in cursors)
        {
            result.Add(new CursorState(cursor.ModelState, cursor.ViewState));
            result.Add(useLogicalLine
                ? CursorState.FromModelState(MoveOperations.TranslateUp(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState))
                : CursorState.FromViewState(MoveOperations.TranslateUp(viewModel.CursorConfig, viewModel, cursor.ViewState)));
        }

        return result;
    }

    public static List<PartialCursorState> MoveToBeginningOfLine(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            result.Add(MoveToLineStart(viewModel, cursor, inSelectionMode));
        }

        return result;
    }

    private static PartialCursorState MoveToLineStart(ViewModel viewModel, CursorState cursor, bool inSelectionMode)
    {
        int currentViewStateColumn = cursor.ViewState.Position.Column;
        int currentModelStateColumn = cursor.ModelState.Position.Column;
        bool isFirstLineOfWrappedLine = currentViewStateColumn == currentModelStateColumn;

        int currentViewStateLineNumber = cursor.ViewState.Position.LineNumber;
        int firstNonBlankColumn = viewModel.GetLineFirstNonWhitespaceColumn(currentViewStateLineNumber);
        bool isBeginningOfViewLine = currentViewStateColumn == firstNonBlankColumn;

        if (!isFirstLineOfWrappedLine && !isBeginningOfViewLine)
        {
            return MoveToLineStartByView(viewModel, cursor, inSelectionMode);
        }

        return MoveToLineStartByModel(viewModel, cursor, inSelectionMode);
    }

    private static PartialViewCursorState MoveToLineStartByView(ViewModel viewModel, CursorState cursor, bool inSelectionMode) =>
        CursorState.FromViewState(MoveOperations.MoveToBeginningOfLine(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode));

    private static PartialModelCursorState MoveToLineStartByModel(ViewModel viewModel, CursorState cursor, bool inSelectionMode) =>
        CursorState.FromModelState(MoveOperations.MoveToBeginningOfLine(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode));

    public static List<PartialCursorState> MoveToEndOfLine(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode, bool sticky)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            result.Add(MoveToLineEnd(viewModel, cursor, inSelectionMode, sticky));
        }

        return result;
    }

    private static PartialCursorState MoveToLineEnd(ViewModel viewModel, CursorState cursor, bool inSelectionMode, bool sticky)
    {
        Position viewStatePosition = cursor.ViewState.Position;
        int viewModelMaxColumn = viewModel.GetLineMaxColumn(viewStatePosition.LineNumber);
        bool isEndOfViewLine = viewStatePosition.Column == viewModelMaxColumn;

        Position modelStatePosition = cursor.ModelState.Position;
        int modelMaxColumn = viewModel.Model.GetLineMaxColumn(modelStatePosition.LineNumber);
        bool isEndLineOfWrappedLine = viewModelMaxColumn - viewStatePosition.Column == modelMaxColumn - modelStatePosition.Column;

        if (isEndOfViewLine || isEndLineOfWrappedLine)
        {
            return MoveToLineEndByModel(viewModel, cursor, inSelectionMode, sticky);
        }

        return MoveToLineEndByView(viewModel, cursor, inSelectionMode, sticky);
    }

    private static PartialViewCursorState MoveToLineEndByView(ViewModel viewModel, CursorState cursor, bool inSelectionMode, bool sticky) =>
        CursorState.FromViewState(MoveOperations.MoveToEndOfLine(viewModel.CursorConfig, viewModel, cursor.ViewState, inSelectionMode, sticky));

    private static PartialModelCursorState MoveToLineEndByModel(ViewModel viewModel, CursorState cursor, bool inSelectionMode, bool sticky) =>
        CursorState.FromModelState(MoveOperations.MoveToEndOfLine(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode, sticky));

    public static List<PartialCursorState> ExpandLineSelection(ViewModel viewModel, IReadOnlyList<CursorState> cursors)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            int startLineNumber = cursor.ModelState.Selection.Range.StartLineNumber;
            int lineCount = viewModel.Model.LineCount;

            int endLineNumber = cursor.ModelState.Selection.Range.EndLineNumber;
            int endColumn;
            if (endLineNumber == lineCount)
            {
                endColumn = viewModel.Model.GetLineMaxColumn(lineCount);
            }
            else
            {
                endLineNumber++;
                endColumn = 1;
            }

            result.Add(CursorState.FromModelState(new SingleCursorState(
                new Range(startLineNumber, 1, startLineNumber, 1),
                SelectionStartKind.Simple,
                0,
                new Position(endLineNumber, endColumn),
                0)));
        }

        return result;
    }

    public static List<PartialCursorState> MoveToBeginningOfBuffer(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            result.Add(CursorState.FromModelState(MoveOperations.MoveToBeginningOfBuffer(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode)));
        }

        return result;
    }

    public static List<PartialCursorState> MoveToEndOfBuffer(ViewModel viewModel, IReadOnlyList<CursorState> cursors, bool inSelectionMode)
    {
        var result = new List<PartialCursorState>(cursors.Count);
        foreach (CursorState cursor in cursors)
        {
            result.Add(CursorState.FromModelState(MoveOperations.MoveToEndOfBuffer(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode)));
        }

        return result;
    }

    // Monaco: the 'cursor' parameter is unused; it is kept so the signature stays Monaco's.
    public static PartialCursorState SelectAll(ViewModel viewModel, CursorState cursor)
    {
        int lineCount = viewModel.Model.LineCount;
        int maxColumn = viewModel.Model.GetLineMaxColumn(lineCount);

        return CursorState.FromModelState(new SingleCursorState(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new Position(lineCount, maxColumn),
            0));
    }

    /// <summary>Line selection (by mouse). <paramref name="viewPosition"/> is Monaco's optional <c>_viewPosition</c>.</summary>
    public static PartialCursorState Line(ViewModel viewModel, CursorState cursor, bool inSelectionMode, Position inputPosition, Position? viewPosition)
    {
        Position position = viewModel.Model.ValidatePosition(inputPosition);
        Position validViewPosition = ToViewPosition(viewModel, position, viewPosition);

        if (!inSelectionMode)
        {
            // Entering line selection for the first time
            int lineCount = viewModel.Model.LineCount;

            int selectToLineNumber = position.LineNumber + 1;
            int selectToColumn = 1;
            if (selectToLineNumber > lineCount)
            {
                selectToLineNumber = lineCount;
                selectToColumn = viewModel.Model.GetLineMaxColumn(selectToLineNumber);
            }

            return CursorState.FromModelState(new SingleCursorState(
                new Range(position.LineNumber, 1, selectToLineNumber, selectToColumn),
                SelectionStartKind.Line,
                0,
                new Position(selectToLineNumber, selectToColumn),
                0));
        }

        // Continuing line selection
        int enteringLineNumber = cursor.ModelState.SelectionStart.Start.LineNumber;

        if (position.LineNumber < enteringLineNumber)
        {
            return CursorState.FromViewState(cursor.ViewState.Move(true, validViewPosition.LineNumber, 1, 0));
        }

        if (position.LineNumber > enteringLineNumber)
        {
            int lineCount = viewModel.GetLineCount();

            int selectToViewLineNumber = validViewPosition.LineNumber + 1;
            int selectToViewColumn = 1;
            if (selectToViewLineNumber > lineCount)
            {
                selectToViewLineNumber = lineCount;
                selectToViewColumn = viewModel.GetLineMaxColumn(selectToViewLineNumber);
            }

            return CursorState.FromViewState(cursor.ViewState.Move(true, selectToViewLineNumber, selectToViewColumn, 0));
        }

        Position endPositionOfSelectionStart = cursor.ModelState.SelectionStart.End;
        return CursorState.FromModelState(cursor.ModelState.Move(true, endPositionOfSelectionStart.LineNumber, endPositionOfSelectionStart.Column, 0));
    }

    public static PartialCursorState Word(ViewModel viewModel, CursorState cursor, bool inSelectionMode, Position inputPosition)
    {
        Position position = viewModel.Model.ValidatePosition(inputPosition);
        return CursorState.FromModelState(WordOperations.Word(viewModel.CursorConfig, ModelOf(viewModel), cursor.ModelState, inSelectionMode, position));
    }

    public static PartialCursorState CancelSelection(ViewModel viewModel, CursorState cursor)
    {
        if (!cursor.ModelState.HasSelection())
        {
            return new CursorState(cursor.ModelState, cursor.ViewState);
        }

        int lineNumber = cursor.ViewState.Position.LineNumber;
        int column = cursor.ViewState.Position.Column;

        return CursorState.FromViewState(new SingleCursorState(
            new Range(lineNumber, column, lineNumber, column),
            SelectionStartKind.Simple,
            0,
            new Position(lineNumber, column),
            0));
    }

    public static PartialCursorState MoveTo(ViewModel viewModel, CursorState cursor, bool inSelectionMode, Position inputPosition, Position? viewPosition)
    {
        if (inSelectionMode)
        {
            if (cursor.ModelState.SelectionStartKind == SelectionStartKind.Word)
            {
                return Word(viewModel, cursor, inSelectionMode, inputPosition);
            }

            if (cursor.ModelState.SelectionStartKind == SelectionStartKind.Line)
            {
                return Line(viewModel, cursor, inSelectionMode, inputPosition, viewPosition);
            }
        }

        Position position = viewModel.Model.ValidatePosition(inputPosition);
        Position validViewPosition = ToViewPosition(viewModel, position, viewPosition);
        return CursorState.FromViewState(cursor.ViewState.Move(inSelectionMode, validViewPosition.LineNumber, validViewPosition.Column, 0));
    }

    private static Position ToViewPosition(ViewModel viewModel, Position position, Position? viewPosition) =>
        viewPosition is { } view
            ? viewModel.CoordinatesConverter.ValidateViewPosition(view, position)
            : viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(position);

    private static PartialViewCursorState MoveToViewPosition(CursorState cursor, bool inSelectionMode, int toViewLineNumber, int toViewColumn) =>
        CursorState.FromViewState(cursor.ViewState.Move(inSelectionMode, toViewLineNumber, toViewColumn, 0));

    private static PartialModelCursorState MoveToModelPosition(CursorState cursor, bool inSelectionMode, int toModelLineNumber, int toModelColumn) =>
        CursorState.FromModelState(cursor.ModelState.Move(inSelectionMode, toModelLineNumber, toModelColumn, 0));
}
