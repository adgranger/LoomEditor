// Ported from microsoft/vscode: src/vs/editor/common/core/position.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;

namespace LoomEditor.Core.Text;

/// <summary>
/// A position in the editor: a 1-based line number and a 1-based column.
/// </summary>
/// <remarks>
/// <para>
/// Column 1 is <em>before</em> the first character of the line; column <c>length + 1</c> is after the last one.
/// The column counts UTF-16 code units, exactly like Monaco (and like <see cref="string.Length"/>).
/// </para>
/// <para>
/// <c>default(Position)</c> is (0, 0) and is <strong>not</strong> a valid position; always use the constructor.
/// </para>
/// <para>Thread safety: immutable value, safe to share between threads.</para>
/// </remarks>
public readonly record struct Position : IComparable<Position>
{
    /// <summary>Creates a position.</summary>
    /// <param name="lineNumber">1-based line number.</param>
    /// <param name="column">1-based column.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is lower than 1.</exception>
    public Position(int lineNumber, int column)
    {
        LineNumber = Guard.IsOneBased(lineNumber);
        Column = Guard.IsOneBased(column);
    }

    /// <summary>Line number (starts at 1).</summary>
    public int LineNumber { get; }

    /// <summary>Column (starts at 1). The first character of a line is between column 1 and column 2.</summary>
    public int Column { get; }

    /// <summary>Returns a copy of this position with a different line number and/or column.</summary>
    /// <remarks>Monaco: <c>Position.with</c>.</remarks>
    public Position With(int? lineNumber = null, int? column = null) =>
        new(lineNumber ?? LineNumber, column ?? Column);

    /// <summary>
    /// Returns a copy moved by the given number of lines and columns. The result is clamped to (1, 1).
    /// </summary>
    /// <remarks>Monaco: <c>Position.delta</c>.</remarks>
    public Position Delta(int deltaLineNumber = 0, int deltaColumn = 0) =>
        new(Math.Max(1, LineNumber + deltaLineNumber), Math.Max(1, Column + deltaColumn));

    /// <summary>True if this position is strictly before <paramref name="other"/>.</summary>
    public bool IsBefore(Position other) => CompareTo(other) < 0;

    /// <summary>True if this position is before <paramref name="other"/> or equal to it.</summary>
    public bool IsBeforeOrEqual(Position other) => CompareTo(other) <= 0;

    /// <summary>Orders positions by line number, then by column.</summary>
    /// <remarks>Monaco: <c>Position.compare</c>.</remarks>
    public int CompareTo(Position other)
    {
        int byLine = LineNumber.CompareTo(other.LineNumber);
        return byLine != 0 ? byLine : Column.CompareTo(other.Column);
    }

    /// <summary>Returns the earlier of two positions.</summary>
    public static Position Min(Position a, Position b) => a <= b ? a : b;

    /// <summary>Returns the later of two positions.</summary>
    public static Position Max(Position a, Position b) => a >= b ? a : b;

    /// <summary>Formats the position like Monaco does: <c>(line,column)</c>.</summary>
    public override string ToString() => $"({LineNumber},{Column})";

    /// <summary>True if <paramref name="left"/> is strictly before <paramref name="right"/>.</summary>
    public static bool operator <(Position left, Position right) => left.CompareTo(right) < 0;

    /// <summary>True if <paramref name="left"/> is before or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(Position left, Position right) => left.CompareTo(right) <= 0;

    /// <summary>True if <paramref name="left"/> is strictly after <paramref name="right"/>.</summary>
    public static bool operator >(Position left, Position right) => left.CompareTo(right) > 0;

    /// <summary>True if <paramref name="left"/> is after or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(Position left, Position right) => left.CompareTo(right) >= 0;
}
