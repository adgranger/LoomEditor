// Ported from microsoft/vscode: src/vs/editor/common/core/range.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;

namespace LoomEditor.Core.Text;

/// <summary>
/// A range in the editor, from a start position to an end position (both 1-based).
/// The start is always before or equal to the end: the constructor swaps them if needed.
/// </summary>
/// <remarks>
/// <para>
/// This type keeps Monaco's name on purpose, so it clashes with <see cref="System.Range"/> when both
/// namespaces are imported. Projects of this repository declare a global alias
/// (<c>global using Range = LoomEditor.Core.Text.Range;</c>); add the same alias in your own code.
/// See docs/adr/0003-naming-and-namespaces.md.
/// </para>
/// <para>
/// A range is <em>empty</em> when start == end: it then represents a caret position, not a span of text.
/// </para>
/// <para>Thread safety: immutable value, safe to share between threads.</para>
/// </remarks>
public readonly record struct Range
{
    /// <summary>
    /// Creates a range. If the end is before the start, the two positions are swapped
    /// (same behavior as Monaco).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is lower than 1.</exception>
    public Range(int startLineNumber, int startColumn, int endLineNumber, int endColumn)
    {
        Guard.IsOneBased(startLineNumber);
        Guard.IsOneBased(startColumn);
        Guard.IsOneBased(endLineNumber);
        Guard.IsOneBased(endColumn);

        bool isReversed = startLineNumber > endLineNumber
            || (startLineNumber == endLineNumber && startColumn > endColumn);

        if (isReversed)
        {
            (startLineNumber, endLineNumber) = (endLineNumber, startLineNumber);
            (startColumn, endColumn) = (endColumn, startColumn);
        }

        StartLineNumber = startLineNumber;
        StartColumn = startColumn;
        EndLineNumber = endLineNumber;
        EndColumn = endColumn;
    }

    /// <summary>Creates a range between two positions (given in any order).</summary>
    /// <remarks>Monaco: <c>Range.fromPositions</c>.</remarks>
    public Range(Position start, Position end)
        : this(start.LineNumber, start.Column, end.LineNumber, end.Column)
    {
    }

    /// <summary>Line number on which the range starts (starts at 1).</summary>
    public int StartLineNumber { get; }

    /// <summary>Column on which the range starts in <see cref="StartLineNumber"/> (starts at 1).</summary>
    public int StartColumn { get; }

    /// <summary>Line number on which the range ends.</summary>
    public int EndLineNumber { get; }

    /// <summary>Column on which the range ends in <see cref="EndLineNumber"/>.</summary>
    public int EndColumn { get; }

    /// <summary>The start position of the range.</summary>
    public Position Start => new(StartLineNumber, StartColumn);

    /// <summary>The end position of the range.</summary>
    public Position End => new(EndLineNumber, EndColumn);

    /// <summary>True if the range is empty (start == end), i.e. it is just a caret position.</summary>
    public bool IsEmpty => StartLineNumber == EndLineNumber && StartColumn == EndColumn;

    /// <summary>True if the range covers more than one line.</summary>
    public bool SpansMultipleLines => EndLineNumber > StartLineNumber;

    /// <summary>Creates an empty range at <paramref name="position"/>.</summary>
    public static Range Collapsed(Position position) => new(position, position);

    /// <summary>True if <paramref name="position"/> is inside the range or on its edges.</summary>
    /// <remarks>Monaco: <c>Range.containsPosition</c>.</remarks>
    public bool Contains(Position position) => Start <= position && position <= End;

    /// <summary>True if <paramref name="position"/> is inside the range and <em>not</em> on its edges.</summary>
    /// <remarks>Monaco: <c>Range.strictContainsPosition</c>.</remarks>
    public bool StrictContains(Position position) => Start < position && position < End;

    /// <summary>True if <paramref name="other"/> is inside this range, edges included.</summary>
    /// <remarks>Monaco: <c>Range.containsRange</c>.</remarks>
    public bool Contains(Range other) => Contains(other.Start) && Contains(other.End);

    /// <summary>True if <paramref name="other"/> is inside this range and does not touch its edges.</summary>
    /// <remarks>Monaco: <c>Range.strictContainsRange</c>.</remarks>
    public bool StrictContains(Range other) => StrictContains(other.Start) && StrictContains(other.End);

    /// <summary>Returns the smallest range that covers both this range and <paramref name="other"/>.</summary>
    /// <remarks>Monaco: <c>Range.plusRange</c>.</remarks>
    public Range Plus(Range other) => new(Position.Min(Start, other.Start), Position.Max(End, other.End));

    /// <summary>
    /// Returns the overlapping part of this range and <paramref name="other"/>, or <see langword="null"/>
    /// if they do not overlap. Ranges that only touch produce an empty range.
    /// </summary>
    /// <remarks>Monaco: <c>Range.intersectRanges</c>.</remarks>
    public Range? Intersect(Range other)
    {
        Position start = Position.Max(Start, other.Start);
        Position end = Position.Min(End, other.End);
        return start <= end ? new Range(start, end) : null;
    }

    /// <summary>True if the two ranges overlap or touch (one ends exactly where the other starts).</summary>
    /// <remarks>Monaco: <c>Range.areIntersectingOrTouching</c>.</remarks>
    public bool IntersectsOrTouches(Range other) => End >= other.Start && other.End >= Start;

    /// <summary>True if the two ranges overlap. Ranges that only touch do <em>not</em> intersect.</summary>
    /// <remarks>Monaco: <c>Range.areIntersecting</c>.</remarks>
    public bool Intersects(Range other) => End > other.Start && other.End > Start;

    /// <summary>Returns an empty range at the start of this range.</summary>
    public Range CollapseToStart() => Collapsed(Start);

    /// <summary>Returns an empty range at the end of this range.</summary>
    public Range CollapseToEnd() => Collapsed(End);

    /// <summary>Returns a copy with a different start position. The result is normalized.</summary>
    /// <remarks>Monaco: <c>Range.setStartPosition</c>.</remarks>
    public Range WithStart(Position start) => new(start, End);

    /// <summary>Returns a copy with a different end position. The result is normalized.</summary>
    /// <remarks>Monaco: <c>Range.setEndPosition</c>.</remarks>
    public Range WithEnd(Position end) => new(Start, end);

    /// <summary>Returns a copy moved down (positive) or up (negative) by <paramref name="lineCount"/> lines.</summary>
    /// <remarks>Monaco: <c>Range.delta</c>.</remarks>
    public Range Delta(int lineCount) =>
        new(StartLineNumber + lineCount, StartColumn, EndLineNumber + lineCount, EndColumn);

    /// <summary>Orders ranges by start position, then by end position. Use it to sort ranges.</summary>
    /// <remarks>Monaco: <c>Range.compareRangesUsingStarts</c>.</remarks>
    public static int CompareUsingStarts(Range a, Range b)
    {
        int byStart = a.Start.CompareTo(b.Start);
        return byStart != 0 ? byStart : a.End.CompareTo(b.End);
    }

    /// <summary>Orders ranges by end position, then by start position.</summary>
    /// <remarks>Monaco: <c>Range.compareRangesUsingEnds</c>.</remarks>
    public static int CompareUsingEnds(Range a, Range b)
    {
        int byEnd = a.End.CompareTo(b.End);
        return byEnd != 0 ? byEnd : a.Start.CompareTo(b.Start);
    }

    /// <summary>Formats the range like Monaco does: <c>[startLine,startColumn -> endLine,endColumn]</c>.</summary>
    public override string ToString() => $"[{StartLineNumber},{StartColumn} -> {EndLineNumber},{EndColumn}]";
}
