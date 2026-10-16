// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The elementary cursor moves: left, right, up, down, to a blank line, to the edges of a line or the buffer.</summary>
/// <remarks>Threading: pure functions; thread-safe as long as the model is not modified concurrently.</remarks>
internal static partial class MoveOperations
{
    /// <summary>Monaco: <c>Constants.MAX_SAFE_SMALL_INTEGER</c> (<c>uint.ts</c>).</summary>
    private const int MaxSafeSmallInteger = 1 << 30;

    public static Position LeftPosition(ICursorSimpleModel model, Position position)
    {
        if (position.Column > model.GetLineMinColumn(position.LineNumber))
        {
            return position.Delta(deltaColumn: -Strings.PrevCharLength(model.GetLineContent(position.LineNumber), position.Column - 1));
        }

        if (position.LineNumber > 1)
        {
            int newLineNumber = position.LineNumber - 1;
            return new Position(newLineNumber, model.GetLineMaxColumn(newLineNumber));
        }

        return position;
    }

    private static Position LeftPositionAtomicSoftTabs(ICursorSimpleModel model, Position position, int tabSize)
    {
        if (position.Column <= model.GetLineIndentColumn(position.LineNumber))
        {
            int minColumn = model.GetLineMinColumn(position.LineNumber);
            string lineContent = model.GetLineContent(position.LineNumber);
            int newPosition = AtomicTabMoveOperations.AtomicPosition(lineContent, position.Column - 1, tabSize, AtomicMoveDirection.Left);
            if (newPosition != -1 && newPosition + 1 >= minColumn)
            {
                return new Position(position.LineNumber, newPosition + 1);
            }
        }

        return LeftPosition(model, position);
    }

    private static CursorPosition Left(CursorConfiguration config, ICursorSimpleModel model, Position position)
    {
        Position pos = config.StickyTabStops
            ? LeftPositionAtomicSoftTabs(model, position, config.TabSize)
            : LeftPosition(model, position);
        return new CursorPosition(pos.LineNumber, pos.Column, 0);
    }

    /// <param name="config">The cursor configuration.</param>
    /// <param name="model">The model the cursor moves in.</param>
    /// <param name="cursor">The cursor to move.</param>
    /// <param name="inSelectionMode">True to extend the selection.</param>
    /// <param name="noOfColumns">Must be either <c>1</c> or <c>Math.round(lineContent.length / 2)</c> (for half lines).</param>
    public static SingleCursorState MoveLeft(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, int noOfColumns)
    {
        int lineNumber;
        int column;

        if (cursor.HasSelection() && !inSelectionMode)
        {
            // If the user has a selection and does not want to extend it,
            // put the cursor at the beginning of the selection.
            lineNumber = cursor.Selection.Range.StartLineNumber;
            column = cursor.Selection.Range.StartColumn;
        }
        else
        {
            // This has no effect if noOfColumns == 1.
            // It is ok to do so in the half-line scenario.
            Position pos = cursor.Position.Delta(deltaColumn: -(noOfColumns - 1));

            // We clip the position before normalization, as normalization is not defined
            // for possibly negative columns.
            Position normalizedPos = model.NormalizePosition(ClipPositionColumn(pos, model), PositionAffinity.Left);
            CursorPosition p = Left(config, model, normalizedPos);

            lineNumber = p.LineNumber;
            column = p.Column;
        }

        return cursor.Move(inSelectionMode, lineNumber, column, 0);
    }

    /// <summary>Adjusts the column so that it is within min/max of the line.</summary>
    private static Position ClipPositionColumn(Position position, ICursorSimpleModel model) =>
        new(
            position.LineNumber,
            ClipRange(position.Column, model.GetLineMinColumn(position.LineNumber), model.GetLineMaxColumn(position.LineNumber)));

    private static int ClipRange(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    public static Position RightPosition(ICursorSimpleModel model, int lineNumber, int column)
    {
        if (column < model.GetLineMaxColumn(lineNumber))
        {
            column += Strings.NextCharLength(model.GetLineContent(lineNumber), column - 1);
        }
        else if (lineNumber < model.GetLineCount())
        {
            lineNumber++;
            column = model.GetLineMinColumn(lineNumber);
        }

        return model.NormalizePosition(new Position(lineNumber, column), PositionAffinity.Right);
    }

    // Monaco also takes an unused indentSize parameter; it is not ported.
    public static Position RightPositionAtomicSoftTabs(ICursorSimpleModel model, int lineNumber, int column, int tabSize)
    {
        if (column < model.GetLineIndentColumn(lineNumber))
        {
            string lineContent = model.GetLineContent(lineNumber);
            int newPosition = AtomicTabMoveOperations.AtomicPosition(lineContent, column - 1, tabSize, AtomicMoveDirection.Right);
            if (newPosition != -1)
            {
                return new Position(lineNumber, newPosition + 1);
            }
        }

        return RightPosition(model, lineNumber, column);
    }

    public static CursorPosition Right(CursorConfiguration config, ICursorSimpleModel model, Position position)
    {
        Position pos = config.StickyTabStops
            ? RightPositionAtomicSoftTabs(model, position.LineNumber, position.Column, config.TabSize)
            : RightPosition(model, position.LineNumber, position.Column);
        return new CursorPosition(pos.LineNumber, pos.Column, 0);
    }

    public static SingleCursorState MoveRight(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, int noOfColumns)
    {
        int lineNumber;
        int column;

        if (cursor.HasSelection() && !inSelectionMode)
        {
            // If we are in selection mode, move right without selection cancels selection and puts cursor at the end of the selection
            lineNumber = cursor.Selection.Range.EndLineNumber;
            column = cursor.Selection.Range.EndColumn;
        }
        else
        {
            Position pos = cursor.Position.Delta(deltaColumn: noOfColumns - 1);
            Position normalizedPos = model.NormalizePosition(ClipPositionColumn(pos, model), PositionAffinity.Right);
            CursorPosition r = Right(config, model, normalizedPos);
            lineNumber = r.LineNumber;
            column = r.Column;
        }

        return cursor.Move(inSelectionMode, lineNumber, column, 0);
    }
}
