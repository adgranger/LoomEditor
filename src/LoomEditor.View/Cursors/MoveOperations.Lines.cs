// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

internal static partial class MoveOperations
{
    // Empty or contains only whitespace.
    private static bool IsBlankLine(ICursorSimpleModel model, int lineNumber) =>
        model.GetLineFirstNonWhitespaceColumn(lineNumber) == 0;

    public static SingleCursorState MoveToPrevBlankLine(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode)
    {
        int lineNumber = cursor.Position.LineNumber;

        // If our current line is blank, move to the previous non-blank line
        while (lineNumber > 1 && IsBlankLine(model, lineNumber))
        {
            lineNumber--;
        }

        // Find the previous blank line
        while (lineNumber > 1 && !IsBlankLine(model, lineNumber))
        {
            lineNumber--;
        }

        return cursor.Move(inSelectionMode, lineNumber, model.GetLineMinColumn(lineNumber), 0);
    }

    public static SingleCursorState MoveToNextBlankLine(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode)
    {
        int lineCount = model.GetLineCount();
        int lineNumber = cursor.Position.LineNumber;

        // If our current line is blank, move to the next non-blank line
        while (lineNumber < lineCount && IsBlankLine(model, lineNumber))
        {
            lineNumber++;
        }

        // Find the next blank line
        while (lineNumber < lineCount && !IsBlankLine(model, lineNumber))
        {
            lineNumber++;
        }

        return cursor.Move(inSelectionMode, lineNumber, model.GetLineMinColumn(lineNumber), 0);
    }

    public static SingleCursorState MoveToBeginningOfLine(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode)
    {
        int lineNumber = cursor.Position.LineNumber;
        int minColumn = model.GetLineMinColumn(lineNumber);
        int firstNonWhitespaceColumn = model.GetLineFirstNonWhitespaceColumn(lineNumber);

        // Monaco: '|| minColumn' (0 means a blank line).
        int firstNonBlankColumn = firstNonWhitespaceColumn == 0 ? minColumn : firstNonWhitespaceColumn;

        int column = cursor.Position.Column == firstNonBlankColumn ? minColumn : firstNonBlankColumn;

        return cursor.Move(inSelectionMode, lineNumber, column, 0);
    }

    public static SingleCursorState MoveToEndOfLine(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, bool sticky)
    {
        int lineNumber = cursor.Position.LineNumber;
        int maxColumn = model.GetLineMaxColumn(lineNumber);
        return cursor.Move(inSelectionMode, lineNumber, maxColumn, sticky ? MaxSafeSmallInteger - maxColumn : 0);
    }

    public static SingleCursorState MoveToBeginningOfBuffer(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode) =>
        cursor.Move(inSelectionMode, 1, 1, 0);

    public static SingleCursorState MoveToEndOfBuffer(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode)
    {
        int lastLineNumber = model.GetLineCount();
        int lastColumn = model.GetLineMaxColumn(lastLineNumber);

        return cursor.Move(inSelectionMode, lastLineNumber, lastColumn, 0);
    }
}
