// Ported from microsoft/vscode: src/vs/editor/common/core/ranges/lineRange.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>A sorted set of disjoint, non-touching line ranges (Monaco: <c>LineRangeSet</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class LineRangeSet
{
    // Sorted by start line number; no two ranges touch or intersect.
    private readonly List<LineRange> _normalizedRanges;

    /// <summary>Creates a set from already normalized ranges (sorted, disjoint, not touching); the list is used as is.</summary>
    public LineRangeSet(List<LineRange>? normalizedRanges = null)
    {
        _normalizedRanges = normalizedRanges ?? [];
    }

    /// <summary><c>ranges</c>.</summary>
    public IReadOnlyList<LineRange> Ranges => _normalizedRanges;

    // Index of the first range that touches `range` or is after it.
    private int FirstTouchingOrAfter(LineRange range)
    {
        int lo = 0;
        int hi = _normalizedRanges.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >>> 1;
            if (_normalizedRanges[mid].EndLineNumberExclusive >= range.StartLineNumber)
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return lo;
    }

    // Index of the last range whose start is <= `value` (or < `value` when not inclusive); -1 if none.
    private int LastStartingBefore(int value, bool inclusive)
    {
        int lo = 0;
        int hi = _normalizedRanges.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >>> 1;
            int start = _normalizedRanges[mid].StartLineNumber;
            if (inclusive ? start <= value : start < value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo - 1;
    }

    /// <summary><c>addRange</c>: adds a range, merging the ranges it touches.</summary>
    public void AddRange(LineRange range)
    {
        if (range.Length == 0)
        {
            return;
        }

        int startIdx = FirstTouchingOrAfter(range);
        int endIdxExclusive = LastStartingBefore(range.EndLineNumberExclusive, inclusive: true) + 1;

        if (startIdx == endIdxExclusive)
        {
            _normalizedRanges.Insert(startIdx, range);
        }
        else if (startIdx == endIdxExclusive - 1)
        {
            _normalizedRanges[startIdx] = _normalizedRanges[startIdx].Join(range);
        }
        else
        {
            LineRange joined = _normalizedRanges[startIdx].Join(_normalizedRanges[endIdxExclusive - 1]).Join(range);
            _normalizedRanges.RemoveRange(startIdx, endIdxExclusive - startIdx);
            _normalizedRanges.Insert(startIdx, joined);
        }
    }

    /// <summary><c>contains</c>.</summary>
    public bool Contains(int lineNumber)
    {
        int idx = LastStartingBefore(lineNumber, inclusive: true);
        return idx >= 0 && _normalizedRanges[idx].EndLineNumberExclusive > lineNumber;
    }

    /// <summary><c>intersects</c>.</summary>
    public bool Intersects(LineRange range)
    {
        int idx = LastStartingBefore(range.EndLineNumberExclusive, inclusive: false);
        return idx >= 0 && _normalizedRanges[idx].EndLineNumberExclusive > range.StartLineNumber;
    }

    /// <summary><c>getUnion</c>. Returns one of the operands when the other is empty (Monaco does too).</summary>
    public LineRangeSet GetUnion(LineRangeSet other)
    {
        if (_normalizedRanges.Count == 0)
        {
            return other;
        }

        if (other._normalizedRanges.Count == 0)
        {
            return this;
        }

        List<LineRange> result = [];
        int i1 = 0;
        int i2 = 0;
        LineRange? current = null;
        while (i1 < _normalizedRanges.Count || i2 < other._normalizedRanges.Count)
        {
            LineRange next;
            if (i1 < _normalizedRanges.Count && i2 < other._normalizedRanges.Count)
            {
                LineRange r1 = _normalizedRanges[i1];
                LineRange r2 = other._normalizedRanges[i2];
                if (r1.StartLineNumber < r2.StartLineNumber)
                {
                    next = r1;
                    i1++;
                }
                else
                {
                    next = r2;
                    i2++;
                }
            }
            else if (i1 < _normalizedRanges.Count)
            {
                next = _normalizedRanges[i1++];
            }
            else
            {
                next = other._normalizedRanges[i2++];
            }

            if (current is not { } c)
            {
                current = next;
            }
            else if (c.EndLineNumberExclusive >= next.StartLineNumber)
            {
                current = new LineRange(c.StartLineNumber, Math.Max(c.EndLineNumberExclusive, next.EndLineNumberExclusive));
            }
            else
            {
                result.Add(c);
                current = next;
            }
        }

        if (current is { } last)
        {
            result.Add(last);
        }

        return new LineRangeSet(result);
    }

    /// <summary><c>subtractFrom</c>: subtracts all ranges of this set from <paramref name="range"/>.</summary>
    public LineRangeSet SubtractFrom(LineRange range)
    {
        int startIdx = FirstTouchingOrAfter(range);
        int endIdxExclusive = LastStartingBefore(range.EndLineNumberExclusive, inclusive: true) + 1;

        if (startIdx == endIdxExclusive)
        {
            return new LineRangeSet([range]);
        }

        List<LineRange> result = [];
        int startLineNumber = range.StartLineNumber;
        for (int i = startIdx; i < endIdxExclusive; i++)
        {
            LineRange r = _normalizedRanges[i];
            if (r.StartLineNumber > startLineNumber)
            {
                result.Add(new LineRange(startLineNumber, r.StartLineNumber));
            }

            startLineNumber = r.EndLineNumberExclusive;
        }

        if (startLineNumber < range.EndLineNumberExclusive)
        {
            result.Add(new LineRange(startLineNumber, range.EndLineNumberExclusive));
        }

        return new LineRangeSet(result);
    }

    /// <summary><c>getIntersection</c>.</summary>
    public LineRangeSet GetIntersection(LineRangeSet other)
    {
        List<LineRange> result = [];
        int i1 = 0;
        int i2 = 0;
        while (i1 < _normalizedRanges.Count && i2 < other._normalizedRanges.Count)
        {
            LineRange r1 = _normalizedRanges[i1];
            LineRange r2 = other._normalizedRanges[i2];

            if (r1.Intersect(r2) is { IsEmpty: false } i)
            {
                result.Add(i);
            }

            if (r1.EndLineNumberExclusive < r2.EndLineNumberExclusive)
            {
                i1++;
            }
            else
            {
                i2++;
            }
        }

        return new LineRangeSet(result);
    }

    /// <summary><c>getWithDelta</c>.</summary>
    public LineRangeSet GetWithDelta(int value)
    {
        List<LineRange> result = new(_normalizedRanges.Count);
        foreach (LineRange r in _normalizedRanges)
        {
            result.Add(r.Delta(value));
        }

        return new LineRangeSet(result);
    }

    /// <summary><c>toString</c>: ranges joined with <c>", "</c>.</summary>
    public override string ToString() => string.Join(", ", _normalizedRanges);
}
