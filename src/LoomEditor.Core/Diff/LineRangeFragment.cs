// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/utils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>
/// The character-to-histogram-index map shared by the fragments of one <c>ComputeMovedLines</c> call.
/// </summary>
/// <remarks>
/// Behavior difference: none. Monaco keeps this map in a static field of <c>LineRangeFragment</c>; the diff runs on a worker
/// thread, so here the caller owns it. Histogram indices are only compared between fragments of the same call.
/// Threading: not thread-safe; one instance per call.
/// </remarks>
internal sealed class LineRangeFragmentContext
{
    /// <summary>The index of each character seen so far.</summary>
    public Dictionary<char, int> Keys { get; } = [];

    /// <summary><c>getKey</c>.</summary>
    public int GetKey(char chr)
    {
        if (!Keys.TryGetValue(chr, out int key))
        {
            key = Keys.Count;
            Keys[chr] = key;
        }

        return key;
    }
}

/// <summary>A range of lines with a character histogram, to measure how similar two blocks of lines are (Monaco: <c>LineRangeFragment</c>).</summary>
/// <remarks>Threading: immutable after construction.</remarks>
internal sealed class LineRangeFragment
{
    private readonly int _totalCount;
    private readonly List<int> _histogram = [];

    public LineRangeFragment(LineRange range, string[] lines, DetailedLineRangeMapping source, LineRangeFragmentContext context)
    {
        Range = range;
        Lines = lines;
        Source = source;

        int counter = 0;
        for (int i = range.StartLineNumber - 1; i < range.EndLineNumberExclusive - 1; i++)
        {
            foreach (char chr in lines[i])
            {
                counter++;
                Increment(context.GetKey(chr));
            }

            counter++;
            Increment(context.GetKey('\n'));
        }

        _totalCount = counter;
    }

    /// <summary>The lines covered.</summary>
    public LineRange Range { get; }

    /// <summary>All lines of the text.</summary>
    public string[] Lines { get; }

    /// <summary>The change this fragment comes from.</summary>
    public DetailedLineRangeMapping Source { get; }

    /// <summary><c>computeSimilarity</c>: 1 for identical histograms, 0 for disjoint ones.</summary>
    public double ComputeSimilarity(LineRangeFragment other)
    {
        int sumDifferences = 0;
        int maxLength = Math.Max(_histogram.Count, other._histogram.Count);
        for (int i = 0; i < maxLength; i++)
        {
            sumDifferences += Math.Abs((i < _histogram.Count ? _histogram[i] : 0) - (i < other._histogram.Count ? other._histogram[i] : 0));
        }

        return 1 - ((double)sumDifferences / (_totalCount + other._totalCount));
    }

    /// <summary><c>isSpace</c>.</summary>
    public static bool IsSpace(int charCode) => charCode is ' ' or '\t';

    private void Increment(int key)
    {
        while (_histogram.Count <= key)
        {
            _histogram.Add(0);
        }

        _histogram[key]++;
    }
}
