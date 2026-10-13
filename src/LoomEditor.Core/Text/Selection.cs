// Ported from microsoft/vscode: src/vs/editor/common/core/selection.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;

namespace LoomEditor.Core.Text;

/// <summary>
/// The direction of a <see cref="Selection"/>: where the caret is compared to the anchor.
/// </summary>
public enum SelectionDirection
{
    /// <summary>The caret is at the end of the selection (the user selected from left to right).</summary>
    LeftToRight = 0,

    /// <summary>The caret is at the start of the selection (the user selected from right to left).</summary>
    RightToLeft = 1,
}

/// <summary>
/// A selection in the editor: a range that also remembers <em>where the user started</em> (the anchor,
/// called <see cref="SelectionStart"/>) and <em>where the caret is</em> (<see cref="Position"/>).
/// </summary>
/// <remarks>
/// <para>
/// Example: the user clicks at (1,10) and shift-clicks at (1,3). The selection is
/// <c>SelectionStart = (1,10)</c>, <c>Position = (1,3)</c>; its <see cref="Range"/> is <c>[1,3 -> 1,10]</c>
/// and its <see cref="Direction"/> is <see cref="SelectionDirection.RightToLeft"/>.
/// </para>
/// <para>
/// Monaco's <c>Selection</c> inherits from <c>Range</c>. C# record structs cannot inherit, so
/// <see cref="Range"/> is exposed as a property instead.
/// </para>
/// <para>Thread safety: immutable value, safe to share between threads.</para>
/// </remarks>
public readonly record struct Selection
{
    /// <summary>Creates a selection from an anchor and a caret position (1-based).</summary>
    /// <param name="selectionStartLineNumber">Line of the anchor (where the selection started).</param>
    /// <param name="selectionStartColumn">Column of the anchor.</param>
    /// <param name="positionLineNumber">Line of the caret.</param>
    /// <param name="positionColumn">Column of the caret.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is lower than 1.</exception>
    public Selection(int selectionStartLineNumber, int selectionStartColumn, int positionLineNumber, int positionColumn)
    {
        SelectionStartLineNumber = Guard.IsOneBased(selectionStartLineNumber);
        SelectionStartColumn = Guard.IsOneBased(selectionStartColumn);
        PositionLineNumber = Guard.IsOneBased(positionLineNumber);
        PositionColumn = Guard.IsOneBased(positionColumn);
    }

    /// <summary>Creates a selection from an anchor and a caret position.</summary>
    /// <remarks>Monaco: <c>Selection.fromPositions</c>.</remarks>
    public Selection(Position selectionStart, Position position)
        : this(selectionStart.LineNumber, selectionStart.Column, position.LineNumber, position.Column)
    {
    }

    /// <summary>Line of the anchor, i.e. where the user started selecting.</summary>
    public int SelectionStartLineNumber { get; }

    /// <summary>Column of the anchor.</summary>
    public int SelectionStartColumn { get; }

    /// <summary>Line of the caret.</summary>
    public int PositionLineNumber { get; }

    /// <summary>Column of the caret.</summary>
    public int PositionColumn { get; }

    /// <summary>The anchor: where the user started selecting.</summary>
    public Position SelectionStart => new(SelectionStartLineNumber, SelectionStartColumn);

    /// <summary>The caret position.</summary>
    public Position Position => new(PositionLineNumber, PositionColumn);

    /// <summary>The selected range, normalized so that start &lt;= end.</summary>
    public Range Range => new(SelectionStart, Position);

    /// <summary>True if nothing is selected (anchor == caret).</summary>
    public bool IsEmpty => SelectionStart == Position;

    /// <summary>
    /// <see cref="SelectionDirection.LeftToRight"/> when the anchor is the start of the range,
    /// <see cref="SelectionDirection.RightToLeft"/> otherwise.
    /// </summary>
    /// <remarks>Monaco: <c>Selection.getDirection</c>.</remarks>
    public SelectionDirection Direction =>
        SelectionStart == Range.Start ? SelectionDirection.LeftToRight : SelectionDirection.RightToLeft;

    /// <summary>Creates an empty selection (a caret) at <paramref name="position"/>.</summary>
    public static Selection Collapsed(Position position) => new(position, position);

    /// <summary>Creates a selection covering <paramref name="range"/> with the given direction.</summary>
    /// <remarks>Monaco: <c>Selection.fromRange</c>.</remarks>
    public static Selection FromRange(Range range, SelectionDirection direction) =>
        direction == SelectionDirection.LeftToRight
            ? new Selection(range.Start, range.End)
            : new Selection(range.End, range.Start);

    /// <summary>Returns a copy whose caret is moved to <paramref name="position"/>; the anchor does not move.</summary>
    public Selection WithPosition(Position position) => new(SelectionStart, position);

    /// <summary>
    /// Returns a copy whose range end is moved to <paramref name="end"/>. The range start does not move.
    /// </summary>
    /// <remarks>
    /// Monaco: <c>Selection.setEndPosition</c>. Like Monaco, if <paramref name="end"/> is before the
    /// start, the direction of the result flips; it is not re-normalized.
    /// </remarks>
    public Selection WithEnd(Position end) =>
        Direction == SelectionDirection.LeftToRight
            ? new Selection(Range.Start, end)
            : new Selection(end, Range.Start);

    /// <summary>
    /// Returns a copy whose range start is moved to <paramref name="start"/>. The range end does not move.
    /// </summary>
    /// <remarks>
    /// Monaco: <c>Selection.setStartPosition</c>. Like Monaco, if <paramref name="start"/> is after the
    /// end, the direction of the result flips; it is not re-normalized.
    /// </remarks>
    public Selection WithStart(Position start) =>
        Direction == SelectionDirection.LeftToRight
            ? new Selection(start, Range.End)
            : new Selection(Range.End, start);

    /// <summary>
    /// Formats the selection like Monaco does: <c>[anchorLine,anchorColumn -> caretLine,caretColumn]</c>.
    /// </summary>
    public override string ToString() =>
        $"[{SelectionStartLineNumber},{SelectionStartColumn} -> {PositionLineNumber},{PositionColumn}]";
}
