// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

internal static partial class MoveOperations
{
    public static CursorPosition Vertical(
        CursorConfiguration config,
        ICursorSimpleModel model,
        int lineNumber,
        int column,
        int leftoverVisibleColumns,
        int newLineNumber,
        bool allowMoveOnEdgeLine,
        PositionAffinity? normalizationAffinity = null)
    {
        int currentVisibleColumn = CursorColumns.VisibleColumnFromColumn(model.GetLineContent(lineNumber), column, config.TabSize) + leftoverVisibleColumns;
        int lineCount = model.GetLineCount();
        bool wasOnFirstPosition = lineNumber == 1 && column == 1;
        bool wasOnLastPosition = lineNumber == lineCount && column == model.GetLineMaxColumn(lineNumber);
        bool wasAtEdgePosition = newLineNumber < lineNumber ? wasOnFirstPosition : wasOnLastPosition;

        lineNumber = newLineNumber;
        if (lineNumber < 1)
        {
            lineNumber = 1;
            column = allowMoveOnEdgeLine
                ? model.GetLineMinColumn(lineNumber)
                : Math.Min(model.GetLineMaxColumn(lineNumber), column);
        }
        else if (lineNumber > lineCount)
        {
            lineNumber = lineCount;
            column = allowMoveOnEdgeLine
                ? model.GetLineMaxColumn(lineNumber)
                : Math.Min(model.GetLineMaxColumn(lineNumber), column);
        }
        else
        {
            column = config.ColumnFromVisibleColumn(model, lineNumber, currentVisibleColumn);
        }

        if (wasAtEdgePosition)
        {
            leftoverVisibleColumns = 0;
        }
        else
        {
            leftoverVisibleColumns = currentVisibleColumn - CursorColumns.VisibleColumnFromColumn(model.GetLineContent(lineNumber), column, config.TabSize);
        }

        if (normalizationAffinity is { } affinity)
        {
            Position newPosition = model.NormalizePosition(new Position(lineNumber, column), affinity);
            leftoverVisibleColumns += column - newPosition.Column;
            lineNumber = newPosition.LineNumber;
            column = newPosition.Column;
        }

        return new CursorPosition(lineNumber, column, leftoverVisibleColumns);
    }

    public static CursorPosition Down(CursorConfiguration config, ICursorSimpleModel model, int lineNumber, int column, int leftoverVisibleColumns, int count, bool allowMoveOnLastLine) =>
        Vertical(config, model, lineNumber, column, leftoverVisibleColumns, lineNumber + count, allowMoveOnLastLine, PositionAffinity.RightOfInjectedText);

    public static SingleCursorState MoveDown(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, int linesCount)
    {
        int lineNumber;
        int column;

        if (cursor.HasSelection() && !inSelectionMode)
        {
            // If we are in selection mode, move down acts relative to the end of selection
            lineNumber = cursor.Selection.Range.EndLineNumber;
            column = cursor.Selection.Range.EndColumn;
        }
        else
        {
            lineNumber = cursor.Position.LineNumber;
            column = cursor.Position.Column;
        }

        int i = 0;
        CursorPosition r;
        do
        {
            r = Down(config, model, lineNumber + i, column, cursor.LeftoverVisibleColumns, linesCount, true);
            Position np = model.NormalizePosition(new Position(r.LineNumber, r.Column), PositionAffinity.None);
            if (np.LineNumber > lineNumber)
            {
                break;
            }
        }
        while (i++ < 10 && lineNumber + i < model.GetLineCount());

        return cursor.Move(inSelectionMode, r.LineNumber, r.Column, r.LeftoverVisibleColumns);
    }

    public static SingleCursorState TranslateDown(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor)
    {
        Selection selection = cursor.Selection;

        CursorPosition selectionStart = Down(config, model, selection.SelectionStartLineNumber, selection.SelectionStartColumn, cursor.SelectionStartLeftoverVisibleColumns, 1, false);
        CursorPosition position = Down(config, model, selection.PositionLineNumber, selection.PositionColumn, cursor.LeftoverVisibleColumns, 1, false);

        return TranslatedState(selectionStart, position);
    }

    public static CursorPosition Up(CursorConfiguration config, ICursorSimpleModel model, int lineNumber, int column, int leftoverVisibleColumns, int count, bool allowMoveOnFirstLine) =>
        Vertical(config, model, lineNumber, column, leftoverVisibleColumns, lineNumber - count, allowMoveOnFirstLine, PositionAffinity.LeftOfInjectedText);

    public static SingleCursorState MoveUp(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, int linesCount)
    {
        int lineNumber;
        int column;

        if (cursor.HasSelection() && !inSelectionMode)
        {
            // If we are in selection mode, move up acts relative to the beginning of selection
            lineNumber = cursor.Selection.Range.StartLineNumber;
            column = cursor.Selection.Range.StartColumn;
        }
        else
        {
            lineNumber = cursor.Position.LineNumber;
            column = cursor.Position.Column;
        }

        CursorPosition r = Up(config, model, lineNumber, column, cursor.LeftoverVisibleColumns, linesCount, true);

        return cursor.Move(inSelectionMode, r.LineNumber, r.Column, r.LeftoverVisibleColumns);
    }

    public static SingleCursorState TranslateUp(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor)
    {
        Selection selection = cursor.Selection;

        CursorPosition selectionStart = Up(config, model, selection.SelectionStartLineNumber, selection.SelectionStartColumn, cursor.SelectionStartLeftoverVisibleColumns, 1, false);
        CursorPosition position = Up(config, model, selection.PositionLineNumber, selection.PositionColumn, cursor.LeftoverVisibleColumns, 1, false);

        return TranslatedState(selectionStart, position);
    }

    private static SingleCursorState TranslatedState(CursorPosition selectionStart, CursorPosition position) =>
        new(
            new Range(selectionStart.LineNumber, selectionStart.Column, selectionStart.LineNumber, selectionStart.Column),
            SelectionStartKind.Simple,
            selectionStart.LeftoverVisibleColumns,
            new Position(position.LineNumber, position.Column),
            position.LeftoverVisibleColumns);
}
