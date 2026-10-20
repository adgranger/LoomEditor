// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/length.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// A non-negative length in lines and columns packed in one number: <c>lineCount * 2^26 + columnCount</c>
/// (Monaco: <c>Length</c>).
/// </summary>
/// <remarks>
/// <para>
/// Same limits as Monaco: no overflow check, columns must stay below 2^26. The add is not commutative
/// (see <see cref="Add"/>), so there is no <c>operator +</c>. Monaco's <c>lengthHash</c> is replaced by
/// <see cref="GetHashCode"/>.
/// </para>
/// <para>Threading: immutable value, safe to share between threads.</para>
/// </remarks>
internal readonly record struct Length
{
    private const long Factor = 1L << 26;

    private readonly long _value;

    private Length(long value)
    {
        _value = value;
    }

    /// <summary>The zero length (Monaco: <c>lengthZero</c>).</summary>
    public static Length Zero => default;

    /// <summary>True for the zero length.</summary>
    public bool IsZero => _value == 0;

    /// <summary>Creates a length (Monaco: <c>toLength</c>).</summary>
    public static Length Of(int lineCount, int columnCount)
    {
        return new Length(lineCount * Factor + columnCount);
    }

    /// <summary>Monaco: <c>lengthDiff</c>.</summary>
    public static Length Diff(int startLineCount, int startColumnCount, int endLineCount, int endColumnCount)
    {
        return startLineCount != endLineCount
            ? Of(endLineCount - startLineCount, endColumnCount)
            : Of(0, endColumnCount - startColumnCount);
    }

    /// <summary>Monaco: <c>lengthToObj</c>.</summary>
    public TextLength ToObj()
    {
        return new TextLength(GetLineCount(), (int)(_value % Factor));
    }

    /// <summary>Monaco: <c>lengthGetLineCount</c>.</summary>
    public int GetLineCount()
    {
        return (int)(_value / Factor);
    }

    /// <summary>The column count, assuming that the length does not span any line (Monaco: <c>lengthGetColumnCountIfZeroLineCount</c>).</summary>
    public int GetColumnCountIfZeroLineCount()
    {
        return (int)_value;
    }

    /// <summary>
    /// Adds two lengths. <c>[10 lines, 5 cols] + [0 lines, 3 cols] = [10 lines, 8 cols]</c>;
    /// <c>[10 lines, 5 cols] + [20 lines, 3 cols] = [30 lines, 3 cols]</c> (Monaco: <c>lengthAdd</c>).
    /// </summary>
    public static Length Add(Length a, Length b)
    {
        var r = a._value + b._value;
        if (b._value >= Factor)
        {
            r -= a._value % Factor;
        }

        return new Length(r);
    }

    /// <summary>Monaco: <c>sumLengths</c>.</summary>
    public static Length Sum<T>(IReadOnlyList<T> items, Func<T, Length> lengthFn)
    {
        var result = Zero;
        foreach (var item in items)
        {
            result = Add(result, lengthFn(item));
        }

        return result;
    }

    /// <summary>
    /// Returns the non-negative length <c>r</c> such that <c>Add(a, r) == b</c>, or zero if there is none
    /// (Monaco: <c>lengthDiffNonNegative</c>).
    /// </summary>
    public static Length DiffNonNegative(Length a, Length b)
    {
        if (b._value - a._value <= 0)
        {
            return Zero;
        }

        var lineCount1 = a._value / Factor;
        var lineCount2 = b._value / Factor;
        var colCount2 = b._value - lineCount2 * Factor;
        if (lineCount1 == lineCount2)
        {
            var colCount1 = a._value - lineCount1 * Factor;
            return Of(0, (int)(colCount2 - colCount1));
        }

        return Of((int)(lineCount2 - lineCount1), (int)colCount2);
    }

    /// <summary>Monaco: <c>lengthLessThan</c>.</summary>
    public static bool LessThan(Length a, Length b)
    {
        return a._value < b._value;
    }

    /// <summary>Monaco: <c>lengthLessThanEqual</c>.</summary>
    public static bool LessThanEqual(Length a, Length b)
    {
        return a._value <= b._value;
    }

    /// <summary>Monaco: <c>lengthGreaterThanEqual</c>.</summary>
    public static bool GreaterThanEqual(Length a, Length b)
    {
        return a._value >= b._value;
    }

    /// <summary>Compares two lengths; only the sign of the result is meaningful (Monaco: <c>lengthCompare</c>).</summary>
    public static int Compare(Length a, Length b)
    {
        return a._value.CompareTo(b._value);
    }

    /// <summary>Monaco: <c>lengthMax</c>.</summary>
    public static Length Max(Length a, Length b)
    {
        return a._value > b._value ? a : b;
    }

    /// <summary>Monaco: <c>lengthToPosition</c>.</summary>
    public Position ToPosition()
    {
        var o = ToObj();
        return new Position(o.LineCount + 1, o.ColumnCount + 1);
    }

    /// <summary>Monaco: <c>positionToLength</c>.</summary>
    public static Length FromPosition(Position position)
    {
        return Of(position.LineNumber - 1, position.Column - 1);
    }

    /// <summary>Monaco: <c>lengthsToRange</c>.</summary>
    public static Range ToRange(Length start, Length end)
    {
        return new Range(start.ToPosition(), end.ToPosition());
    }

    /// <summary>Monaco: <c>lengthOfRange</c>.</summary>
    public static TextLength OfRange(Range range)
    {
        return range.StartLineNumber == range.EndLineNumber
            ? new TextLength(0, range.EndColumn - range.StartColumn)
            : new TextLength(range.EndLineNumber - range.StartLineNumber, range.EndColumn - 1);
    }

    /// <summary>Monaco: <c>lengthOfString</c>.</summary>
    public static Length OfString(string str)
    {
        var lines = Strings.SplitLines(str);
        return Of(lines.Length - 1, lines[^1].Length);
    }

    /// <summary>Monaco: <c>lengthOfStringObj</c>.</summary>
    public static TextLength OfStringObj(string str)
    {
        var lines = Strings.SplitLines(str);
        return new TextLength(lines.Length - 1, lines[^1].Length);
    }

    /// <summary>For debugging only.</summary>
    public override string ToString()
    {
        return ToObj().ToString();
    }
}
