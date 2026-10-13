// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    // Monaco: StringOffsetValidationType
    private enum StringOffsetValidationType
    {
        /// <summary>A position may sit between the two halves of a surrogate pair.</summary>
        Relaxed,

        /// <summary>A position is moved out of the middle of a surrogate pair.</summary>
        SurrogatePairs,
    }

    /// <summary>Returns the valid position nearest to <paramref name="position"/> (Monaco: <c>validatePosition</c>).</summary>
    /// <remarks>A position in the middle of a surrogate pair is moved before the pair.</remarks>
    public Position ValidatePosition(Position position)
    {
        // Avoid computing a new position in the most likely case
        if (IsValidPosition(position.LineNumber, position.Column, StringOffsetValidationType.SurrogatePairs))
        {
            return position;
        }

        return ValidatePositionCore(position.LineNumber, position.Column, StringOffsetValidationType.SurrogatePairs);
    }

    /// <summary>Returns the valid position nearest to a line number and column that may be outside the text (e.g. 0 or negative).</summary>
    /// <remarks>
    /// Difference from Monaco: <see cref="Position"/> cannot hold values below 1, so Monaco's <c>new Position(0, 0)</c>
    /// is expressed with this overload.
    /// </remarks>
    public Position ValidatePosition(int lineNumber, int column) =>
        ValidatePositionCore(lineNumber, column, StringOffsetValidationType.SurrogatePairs);

    /// <summary>True if <paramref name="range"/> is inside the text and does not cut a surrogate pair (Monaco: <c>isValidRange</c>).</summary>
    public bool IsValidRange(Range range) => IsValidRange(range, StringOffsetValidationType.SurrogatePairs);

    /// <summary>Returns the valid range nearest to <paramref name="range"/> (Monaco: <c>validateRange</c>).</summary>
    /// <remarks>A range that cuts a surrogate pair is expanded to include it; a collapsed one is moved before the pair.</remarks>
    public Range ValidateRange(Range range)
    {
        // Avoid computing a new range in the most likely case
        if (IsValidRange(range, StringOffsetValidationType.SurrogatePairs))
        {
            return range;
        }

        return ValidateRange(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn);
    }

    /// <summary>Returns the valid range nearest to coordinates that may be outside the text (e.g. 0 or negative).</summary>
    /// <remarks>
    /// Difference from Monaco: <see cref="Range"/> cannot hold values below 1, so Monaco's <c>new Range(0, 0, 0, 1)</c>
    /// is expressed with this overload. Like <see cref="Range"/>, a reversed range is swapped.
    /// </remarks>
    public Range ValidateRange(int startLineNumber, int startColumn, int endLineNumber, int endColumn)
    {
        Position start = ValidatePositionCore(startLineNumber, startColumn, StringOffsetValidationType.Relaxed);
        Position end = ValidatePositionCore(endLineNumber, endColumn, StringOffsetValidationType.Relaxed);

        int startLine = start.LineNumber;
        int startCol = start.Column;
        int endLine = end.LineNumber;
        int endCol = end.Column;

        bool startInsideSurrogatePair = IsInsideSurrogatePairAtStart(startLine, startCol);
        bool endInsideSurrogatePair = IsInsideSurrogatePairAtEnd(endLine, endCol);

        if (!startInsideSurrogatePair && !endInsideSurrogatePair)
        {
            return new Range(startLine, startCol, endLine, endCol);
        }

        if (startLine == endLine && startCol == endCol)
        {
            // do not expand a collapsed range, simply move it to a valid location
            return new Range(startLine, startCol - 1, endLine, endCol - 1);
        }

        if (startInsideSurrogatePair && endInsideSurrogatePair)
        {
            // expand range at both ends
            return new Range(startLine, startCol - 1, endLine, endCol + 1);
        }

        if (startInsideSurrogatePair)
        {
            // only expand range at the start
            return new Range(startLine, startCol - 1, endLine, endCol);
        }

        // only expand range at the end
        return new Range(startLine, startCol, endLine, endCol + 1);
    }

    /// <summary>
    /// The position <paramref name="offset"/> characters after (or before, if negative) <paramref name="position"/>,
    /// clamped to the text (Monaco: <c>modifyPosition</c>).
    /// </summary>
    public Position ModifyPosition(Position position, int offset)
    {
        long candidate = (long)GetOffsetAt(position) + offset;
        return GetPositionAt((int)Math.Min(_buffer.GetLength(), Math.Max(0, candidate)));
    }

    private bool IsValidPosition(int lineNumber, int column, StringOffsetValidationType validationType)
    {
        if (lineNumber < 1 || column < 1 || lineNumber > _buffer.GetLineCount())
        {
            return false;
        }

        if (column == 1)
        {
            return true;
        }

        if (column > GetLineMaxColumn(lineNumber))
        {
            return false;
        }

        // At this point, column > 1
        return validationType != StringOffsetValidationType.SurrogatePairs
            || !char.IsHighSurrogate(_buffer.GetLineCharCode(lineNumber, column - 2));
    }

    private Position ValidatePositionCore(int lineNumber, int column, StringOffsetValidationType validationType)
    {
        int lineCount = _buffer.GetLineCount();

        if (lineNumber < 1)
        {
            return new Position(1, 1);
        }

        if (lineNumber > lineCount)
        {
            return new Position(lineCount, GetLineMaxColumn(lineCount));
        }

        if (column <= 1)
        {
            return new Position(lineNumber, 1);
        }

        int maxColumn = GetLineMaxColumn(lineNumber);
        if (column >= maxColumn)
        {
            return new Position(lineNumber, maxColumn);
        }

        if (validationType == StringOffsetValidationType.SurrogatePairs
            && char.IsHighSurrogate(_buffer.GetLineCharCode(lineNumber, column - 2)))
        {
            // The position would end up in the middle of a high-low surrogate pair: move it before the pair
            return new Position(lineNumber, column - 1);
        }

        return new Position(lineNumber, column);
    }

    private bool IsValidRange(Range range, StringOffsetValidationType validationType)
    {
        if (!IsValidPosition(range.StartLineNumber, range.StartColumn, StringOffsetValidationType.Relaxed)
            || !IsValidPosition(range.EndLineNumber, range.EndColumn, StringOffsetValidationType.Relaxed))
        {
            return false;
        }

        return validationType != StringOffsetValidationType.SurrogatePairs
            || (!IsInsideSurrogatePairAtStart(range.StartLineNumber, range.StartColumn)
                && !IsInsideSurrogatePairAtEnd(range.EndLineNumber, range.EndColumn));
    }

    private bool IsInsideSurrogatePairAtStart(int lineNumber, int column) =>
        column > 1 && char.IsHighSurrogate(_buffer.GetLineCharCode(lineNumber, column - 2));

    // Monaco: the end is only checked when it is before the end of the line (endColumn <= getLineLength)
    private bool IsInsideSurrogatePairAtEnd(int lineNumber, int column) =>
        column > 1 && column <= _buffer.GetLineLength(lineNumber)
        && char.IsHighSurrogate(_buffer.GetLineCharCode(lineNumber, column - 2));
}
