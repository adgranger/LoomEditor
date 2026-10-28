// Ported from microsoft/vscode: src/vs/editor/common/core/ranges/lineRange.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>
/// A range of lines (1-based line numbers), end exclusive (Monaco: <c>LineRange</c>). The constructor throws
/// <see cref="ArgumentException"/> when the start is after the end (Monaco: <c>BugIndicatingError</c>).
/// </summary>
/// <remarks>Threading: a value type, immutable.</remarks>
internal readonly record struct LineRange
{
    /// <summary>Creates a range; throws when <paramref name="startLineNumber"/> is greater than <paramref name="endLineNumberExclusive"/>.</summary>
    public LineRange(int startLineNumber, int endLineNumberExclusive)
    {
        if (startLineNumber > endLineNumberExclusive)
        {
            throw new ArgumentException($"startLineNumber {startLineNumber} cannot be after endLineNumberExclusive {endLineNumberExclusive}");
        }

        StartLineNumber = startLineNumber;
        EndLineNumberExclusive = endLineNumberExclusive;
    }

    /// <summary>The start line number.</summary>
    public int StartLineNumber { get; }

    /// <summary>The end line number (exclusive).</summary>
    public int EndLineNumberExclusive { get; }

    /// <summary><c>isEmpty</c>.</summary>
    public bool IsEmpty => StartLineNumber == EndLineNumberExclusive;

    /// <summary><c>length</c>: the number of lines this range spans.</summary>
    public int Length => EndLineNumberExclusive - StartLineNumber;

    /// <summary><c>ofLength</c>.</summary>
    public static LineRange OfLength(int startLineNumber, int length) => new(startLineNumber, startLineNumber + length);

    /// <summary><c>fromRange</c>.</summary>
    public static LineRange FromRange(Range range) => new(range.StartLineNumber, range.EndLineNumber);

    /// <summary><c>fromRangeInclusive</c>.</summary>
    public static LineRange FromRangeInclusive(Range range) => new(range.StartLineNumber, range.EndLineNumber + 1);

    /// <summary><c>compareByStart</c>.</summary>
    public static int CompareByStart(LineRange a, LineRange b) => a.StartLineNumber.CompareTo(b.StartLineNumber);

    /// <summary><c>subtract</c>: the parts of <paramref name="a"/> not covered by <paramref name="b"/> (<c>null</c> subtracts nothing).</summary>
    public static LineRange[] Subtract(LineRange a, LineRange? b)
    {
        if (b is not { } bb)
        {
            return [a];
        }

        if (a.StartLineNumber < bb.StartLineNumber && bb.EndLineNumberExclusive < a.EndLineNumberExclusive)
        {
            return [new(a.StartLineNumber, bb.StartLineNumber), new(bb.EndLineNumberExclusive, a.EndLineNumberExclusive)];
        }

        if (bb.StartLineNumber <= a.StartLineNumber && a.EndLineNumberExclusive <= bb.EndLineNumberExclusive)
        {
            return [];
        }

        if (bb.EndLineNumberExclusive < a.EndLineNumberExclusive)
        {
            return [new(Math.Max(bb.EndLineNumberExclusive, a.StartLineNumber), a.EndLineNumberExclusive)];
        }

        return [new(a.StartLineNumber, Math.Min(bb.StartLineNumber, a.EndLineNumberExclusive))];
    }

    /// <summary><c>joinMany</c>: the union of several arrays of sorted line ranges.</summary>
    public static IReadOnlyList<LineRange> JoinMany(IReadOnlyList<IReadOnlyList<LineRange>> lineRanges)
    {
        if (lineRanges.Count == 0)
        {
            return [];
        }

        LineRangeSet result = new([.. lineRanges[0]]);
        for (int i = 1; i < lineRanges.Count; i++)
        {
            result = result.GetUnion(new LineRangeSet([.. lineRanges[i]]));
        }

        return result.Ranges;
    }

    /// <summary><c>join</c>: the smallest range containing all <paramref name="lineRanges"/> (throws when empty).</summary>
    public static LineRange Join(IReadOnlyList<LineRange> lineRanges)
    {
        if (lineRanges.Count == 0)
        {
            throw new ArgumentException("lineRanges cannot be empty", nameof(lineRanges));
        }

        int start = lineRanges[0].StartLineNumber;
        int end = lineRanges[0].EndLineNumberExclusive;
        for (int i = 1; i < lineRanges.Count; i++)
        {
            start = Math.Min(start, lineRanges[i].StartLineNumber);
            end = Math.Max(end, lineRanges[i].EndLineNumberExclusive);
        }

        return new LineRange(start, end);
    }

    /// <summary><c>deserialize</c>.</summary>
    public static LineRange Deserialize((int StartLineNumber, int EndLineNumberExclusive) lineRange) =>
        new(lineRange.StartLineNumber, lineRange.EndLineNumberExclusive);

    /// <summary><c>serialize</c>.</summary>
    public (int StartLineNumber, int EndLineNumberExclusive) Serialize() => (StartLineNumber, EndLineNumberExclusive);

    /// <summary><c>contains</c>: whether <paramref name="lineNumber"/> is in this range.</summary>
    public bool Contains(int lineNumber) => StartLineNumber <= lineNumber && lineNumber < EndLineNumberExclusive;

    /// <summary><c>containsRange</c>.</summary>
    public bool ContainsRange(LineRange range) =>
        StartLineNumber <= range.StartLineNumber && range.EndLineNumberExclusive <= EndLineNumberExclusive;

    /// <summary><c>delta</c>: moves the range by <paramref name="offset"/> lines.</summary>
    public LineRange Delta(int offset) => new(StartLineNumber + offset, EndLineNumberExclusive + offset);

    /// <summary><c>deltaLength</c>: moves the end by <paramref name="offset"/> lines.</summary>
    public LineRange DeltaLength(int offset) => new(StartLineNumber, EndLineNumberExclusive + offset);

    /// <summary><c>join</c>: the range containing this and <paramref name="other"/>.</summary>
    public LineRange Join(LineRange other) =>
        new(Math.Min(StartLineNumber, other.StartLineNumber), Math.Max(EndLineNumberExclusive, other.EndLineNumberExclusive));

    /// <summary>
    /// <c>intersect</c>: empty when the ranges only touch; <c>null</c> when they do not even touch.
    /// </summary>
    public LineRange? Intersect(LineRange other)
    {
        int start = Math.Max(StartLineNumber, other.StartLineNumber);
        int end = Math.Min(EndLineNumberExclusive, other.EndLineNumberExclusive);
        return start <= end ? new LineRange(start, end) : null;
    }

    /// <summary><c>intersectsStrict</c>.</summary>
    public bool IntersectsStrict(LineRange other) =>
        StartLineNumber < other.EndLineNumberExclusive && other.StartLineNumber < EndLineNumberExclusive;

    /// <summary><c>intersectsOrTouches</c> (the card's <c>overlapOrTouch</c>).</summary>
    public bool IntersectsOrTouches(LineRange other) =>
        StartLineNumber <= other.EndLineNumberExclusive && other.StartLineNumber <= EndLineNumberExclusive;

    /// <summary><c>toInclusiveRange</c>: <c>null</c> when empty; the end column is <see cref="int.MaxValue"/>.</summary>
    public Range? ToInclusiveRange() =>
        IsEmpty ? null : new Range(StartLineNumber, 1, EndLineNumberExclusive - 1, int.MaxValue);

    /// <summary><c>toExclusiveRange</c>: the end position is not guaranteed to be a valid position.</summary>
    public Range ToExclusiveRange() => new(StartLineNumber, 1, EndLineNumberExclusive, 1);

    /// <summary><c>mapToLineArray</c>.</summary>
    public T[] MapToLineArray<T>(Func<int, T> f)
    {
        T[] result = new T[Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = f(StartLineNumber + i);
        }

        return result;
    }

    /// <summary><c>forEach</c>.</summary>
    public void ForEach(Action<int> f)
    {
        for (int lineNumber = StartLineNumber; lineNumber < EndLineNumberExclusive; lineNumber++)
        {
            f(lineNumber);
        }
    }

    /// <summary><c>toOffsetRange</c>: converts to a 0-based offset range (subtracts 1).</summary>
    public OffsetRange ToOffsetRange() => new(StartLineNumber - 1, EndLineNumberExclusive - 1);

    /// <summary><c>distanceToRange</c>.</summary>
    public int DistanceToRange(LineRange other)
    {
        if (EndLineNumberExclusive <= other.StartLineNumber)
        {
            return other.StartLineNumber - EndLineNumberExclusive;
        }

        if (other.EndLineNumberExclusive <= StartLineNumber)
        {
            return StartLineNumber - other.EndLineNumberExclusive;
        }

        return 0;
    }

    /// <summary><c>distanceToLine</c>.</summary>
    public int DistanceToLine(int lineNumber)
    {
        if (Contains(lineNumber))
        {
            return 0;
        }

        return lineNumber < StartLineNumber ? StartLineNumber - lineNumber : lineNumber - EndLineNumberExclusive;
    }

    /// <summary><c>addMargin</c>.</summary>
    public LineRange AddMargin(int marginTop, int marginBottom) =>
        new(StartLineNumber - marginTop, EndLineNumberExclusive + marginBottom);

    /// <summary><c>toString</c>: <c>[start,end)</c>.</summary>
    public override string ToString() => $"[{StartLineNumber},{EndLineNumberExclusive})";
}
