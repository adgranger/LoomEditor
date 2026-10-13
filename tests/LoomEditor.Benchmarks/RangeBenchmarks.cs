using BenchmarkDotNet.Attributes;

namespace LoomEditor.Benchmarks;

/// <summary>
/// Baseline for the value types: they are used millions of times per second once the editor runs,
/// so they must not allocate. Phase 1 adds the piece tree benchmarks listed in docs/performance.md.
/// </summary>
[MemoryDiagnoser]
public class RangeBenchmarks
{
    private readonly Range[] _ranges = CreateRanges(1_000);

    [Benchmark]
    public int SortUsingStarts()
    {
        Range[] copy = (Range[])_ranges.Clone();
        Array.Sort(copy, Range.CompareUsingStarts);
        return copy[0].StartLineNumber;
    }

    [Benchmark]
    public int IntersectAll()
    {
        int count = 0;
        for (int i = 1; i < _ranges.Length; i++)
        {
            if (_ranges[i].Intersect(_ranges[i - 1]) is not null)
            {
                count++;
            }
        }

        return count;
    }

    private static Range[] CreateRanges(int count)
    {
        var random = new Random(42);
        var ranges = new Range[count];
        for (int i = 0; i < count; i++)
        {
            ranges[i] = new Range(random.Next(1, 1000), random.Next(1, 120), random.Next(1, 1000), random.Next(1, 120));
        }

        return ranges;
    }
}
