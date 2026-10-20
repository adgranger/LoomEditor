// Ported from microsoft/vscode: src/vs/editor/common/core/ranges/offsetRange.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Text;

/// <summary>
/// A range of offsets (0-based), end exclusive (Monaco: <c>OffsetRange</c>; also used where Monaco has a
/// <c>LineRange</c>, with line numbers). Only the members the tokenization code needs are ported; add the others when
/// a later card needs them.
/// </summary>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct OffsetRange
{
    /// <summary>Creates a range; throws when <paramref name="start"/> is greater than <paramref name="endExclusive"/>.</summary>
    public OffsetRange(int start, int endExclusive)
    {
        if (start > endExclusive)
        {
            throw new ArgumentException($"Invalid range: [{start}, {endExclusive})");
        }

        Start = start;
        EndExclusive = endExclusive;
    }

    /// <summary>The first offset of the range.</summary>
    public int Start { get; }

    /// <summary>The offset after the last offset of the range.</summary>
    public int EndExclusive { get; }

    /// <summary><c>isEmpty</c>.</summary>
    public bool IsEmpty => Start == EndExclusive;

    /// <summary><c>length</c>.</summary>
    public int Length => EndExclusive - Start;

    /// <summary><c>fromTo</c>.</summary>
    public static OffsetRange FromTo(int start, int endExclusive) => new(start, endExclusive);

    /// <summary><c>ofStartAndLength</c>.</summary>
    public static OffsetRange OfStartAndLength(int start, int length) => new(start, start + length);

    /// <summary><c>emptyAt</c>.</summary>
    public static OffsetRange EmptyAt(int offset) => new(offset, offset);

    /// <summary><c>addRange</c>: adds <paramref name="range"/> to <paramref name="sortedRanges"/>, merging overlapping or touching ranges.</summary>
    public static void AddRange(OffsetRange range, List<OffsetRange> sortedRanges)
    {
        int i = 0;
        while (i < sortedRanges.Count && sortedRanges[i].EndExclusive < range.Start)
        {
            i++;
        }

        int j = i;
        while (j < sortedRanges.Count && sortedRanges[j].Start <= range.EndExclusive)
        {
            j++;
        }

        if (i == j)
        {
            sortedRanges.Insert(i, range);
        }
        else
        {
            int start = Math.Min(range.Start, sortedRanges[i].Start);
            int end = Math.Max(range.EndExclusive, sortedRanges[j - 1].EndExclusive);
            sortedRanges.RemoveRange(i, j - i);
            sortedRanges.Insert(i, new OffsetRange(start, end));
        }
    }

    /// <summary><c>delta</c>.</summary>
    public OffsetRange Delta(int offset) => new(Start + offset, EndExclusive + offset);

    /// <summary><c>contains</c>.</summary>
    public bool Contains(int offset) => Start <= offset && offset < EndExclusive;

    /// <summary><c>join</c>: the smallest range that contains both ranges.</summary>
    public OffsetRange Join(OffsetRange other) => new(Math.Min(Start, other.Start), Math.Max(EndExclusive, other.EndExclusive));

    /// <summary><c>joinRightTouching</c>: <paramref name="other"/> must start where this range ends.</summary>
    /// <exception cref="ArgumentException">The ranges do not touch.</exception>
    public OffsetRange JoinRightTouching(OffsetRange other) =>
        EndExclusive == other.Start ? new OffsetRange(Start, other.EndExclusive) : throw new ArgumentException($"Invalid join: {this} and {other}");

    /// <summary><c>intersect</c>: empty when the ranges only touch, <c>null</c> when they do not even touch.</summary>
    public OffsetRange? Intersect(OffsetRange other)
    {
        int start = Math.Max(Start, other.Start);
        int end = Math.Min(EndExclusive, other.EndExclusive);
        return start <= end ? new OffsetRange(start, end) : null;
    }

    /// <summary><c>intersects</c>: always false when one range is empty.</summary>
    public bool Intersects(OffsetRange other) => Math.Max(Start, other.Start) < Math.Min(EndExclusive, other.EndExclusive);

    /// <summary><c>toString</c>: <c>[start, end)</c>.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{Start}, {EndExclusive})");
}
