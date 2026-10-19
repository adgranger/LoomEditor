// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// A sorted set of disjoint, non-touching ranges of numbers, read from the smallest (Monaco: <c>RangePriorityQueueImpl</c>;
/// its private <c>RangePriorityQueue</c> interface is not ported).
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class RangePriorityQueue
{
    private readonly List<OffsetRange> _ranges = [];

    /// <summary><c>getRanges</c>.</summary>
    public List<OffsetRange> GetRanges() => _ranges;

    /// <summary><c>min</c>: the smallest number, or <c>null</c> when empty.</summary>
    public int? Min => _ranges.Count == 0 ? null : _ranges[0].Start;

    /// <summary><c>removeMin</c>.</summary>
    public int? RemoveMin()
    {
        if (_ranges.Count == 0)
        {
            return null;
        }

        OffsetRange range = _ranges[0];
        if (range.Start + 1 == range.EndExclusive)
        {
            _ranges.RemoveAt(0);
        }
        else
        {
            _ranges[0] = new OffsetRange(range.Start + 1, range.EndExclusive);
        }

        return range.Start;
    }

    /// <summary><c>delete</c>.</summary>
    public void Delete(int value)
    {
        int idx = _ranges.FindIndex(r => r.Contains(value));
        if (idx == -1)
        {
            return;
        }

        OffsetRange range = _ranges[idx];
        if (range.Start == value)
        {
            if (range.EndExclusive == value + 1)
            {
                _ranges.RemoveAt(idx);
            }
            else
            {
                _ranges[idx] = new OffsetRange(value + 1, range.EndExclusive);
            }
        }
        else if (range.EndExclusive == value + 1)
        {
            _ranges[idx] = new OffsetRange(range.Start, value);
        }
        else
        {
            _ranges[idx] = new OffsetRange(range.Start, value);
            _ranges.Insert(idx + 1, new OffsetRange(value + 1, range.EndExclusive));
        }
    }

    /// <summary><c>addRange</c>.</summary>
    public void AddRange(OffsetRange range) => OffsetRange.AddRange(range, _ranges);

    /// <summary><c>addRangeAndResize</c>: replaces <paramref name="range"/> by a range of <paramref name="newLength"/>, shifting what follows.</summary>
    public void AddRangeAndResize(OffsetRange range, int newLength)
    {
        int idxFirstMightBeIntersecting = 0;
        while (!(idxFirstMightBeIntersecting >= _ranges.Count || range.Start <= _ranges[idxFirstMightBeIntersecting].EndExclusive))
        {
            idxFirstMightBeIntersecting++;
        }

        int idxFirstIsAfter = idxFirstMightBeIntersecting;
        while (!(idxFirstIsAfter >= _ranges.Count || range.EndExclusive < _ranges[idxFirstIsAfter].Start))
        {
            idxFirstIsAfter++;
        }

        int delta = newLength - range.Length;

        for (int i = idxFirstIsAfter; i < _ranges.Count; i++)
        {
            _ranges[i] = _ranges[i].Delta(delta);
        }

        if (idxFirstMightBeIntersecting == idxFirstIsAfter)
        {
            var newRange = new OffsetRange(range.Start, range.Start + newLength);
            if (!newRange.IsEmpty)
            {
                _ranges.Insert(idxFirstMightBeIntersecting, newRange);
            }
        }
        else
        {
            int start = Math.Min(range.Start, _ranges[idxFirstMightBeIntersecting].Start);
            int endEx = Math.Max(range.EndExclusive, _ranges[idxFirstIsAfter - 1].EndExclusive);

            var newRange = new OffsetRange(start, endEx + delta);
            _ranges.RemoveRange(idxFirstMightBeIntersecting, idxFirstIsAfter - idxFirstMightBeIntersecting);
            if (!newRange.IsEmpty)
            {
                _ranges.Insert(idxFirstMightBeIntersecting, newRange);
            }
        }
    }

    /// <summary><c>toString</c>.</summary>
    public override string ToString() => string.Join(" + ", _ranges);
}
