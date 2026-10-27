// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/myersDiffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>An O(ND) diff algorithm that has a quadratic space worst-case complexity (Monaco: <c>MyersDiffAlgorithm</c>).</summary>
/// <remarks>Threading: stateless; safe to call from any thread, one call per instance at a time is not required.</remarks>
internal sealed class MyersDiffAlgorithm : IDiffAlgorithm
{
    /// <inheritdoc />
    public DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout = null)
    {
        timeout ??= InfiniteTimeout.Instance;

        // These are common special cases. The early return improves performance dramatically.
        if (sequence1.Length == 0 || sequence2.Length == 0)
        {
            return DiffAlgorithmResult.Trivial(sequence1, sequence2);
        }

        ISequence seqX = sequence1; // Text on the x axis
        ISequence seqY = sequence2; // Text on the y axis

        int GetXAfterSnake(int x, int y)
        {
            while (x < seqX.Length && y < seqY.Length && seqX.GetElement(x) == seqY.GetElement(y))
            {
                x++;
                y++;
            }

            return x;
        }

        int d = 0;

        // V[k]: X value of longest d-line that ends in diagonal k.
        // d-line: path from (0,0) to (x,y) that uses exactly d non-diagonals.
        // diagonal k: Set of points (x,y) with x-y = k.
        var v = new FastInt32Array();
        v.Set(0, GetXAfterSnake(0, 0));

        var paths = new FastArrayNegativeIndices<SnakePath?>();
        paths.Set(0, v.Get(0) == 0 ? null : new SnakePath(null, 0, 0, v.Get(0)));

        int k = 0;

        while (true)
        {
            d++;
            if (!timeout.IsValid())
            {
                return DiffAlgorithmResult.TrivialTimedOut(seqX, seqY);
            }

            // The paper has `for (k = -d; k <= d; k += 2)`, but we can ignore diagonals that cannot influence the result.
            int lowerBound = -Math.Min(d, seqY.Length + (d % 2));
            int upperBound = Math.Min(d, seqX.Length + (d % 2));
            bool done = false;
            for (k = lowerBound; k <= upperBound; k += 2)
            {
                // We can use the X values of (d-1)-lines to compute X value of the longest d-lines.
                int maxXofDLineTop = k == upperBound ? -1 : v.Get(k + 1); // vertical non-diagonal (add a symbol in seqX)
                int maxXofDLineLeft = k == lowerBound ? -1 : v.Get(k - 1) + 1; // horizontal non-diagonal (delete a symbol in seqX)
                int x = Math.Min(Math.Max(maxXofDLineTop, maxXofDLineLeft), seqX.Length);
                int y = x - k;
                if (x > seqX.Length || y > seqY.Length)
                {
                    // This diagonal is irrelevant for the result.
                    continue;
                }

                int newMaxX = GetXAfterSnake(x, y);
                v.Set(k, newMaxX);
                SnakePath? lastPath = x == maxXofDLineTop ? paths.Get(k + 1) : paths.Get(k - 1);
                paths.Set(k, newMaxX != x ? new SnakePath(lastPath, x, y, newMaxX - x) : lastPath);

                if (v.Get(k) == seqX.Length && v.Get(k) - k == seqY.Length)
                {
                    done = true;
                    break;
                }
            }

            if (done)
            {
                break;
            }
        }

        SnakePath? path = paths.Get(k);
        var result = new List<SequenceDiff>();
        int lastAligningPosS1 = seqX.Length;
        int lastAligningPosS2 = seqY.Length;

        while (true)
        {
            int endX = path is not null ? path.X + path.Length : 0;
            int endY = path is not null ? path.Y + path.Length : 0;

            if (endX != lastAligningPosS1 || endY != lastAligningPosS2)
            {
                result.Add(new SequenceDiff(
                    new OffsetRange(endX, lastAligningPosS1),
                    new OffsetRange(endY, lastAligningPosS2)));
            }

            if (path is null)
            {
                break;
            }

            lastAligningPosS1 = path.X;
            lastAligningPosS2 = path.Y;

            path = path.Prev;
        }

        result.Reverse();
        return new DiffAlgorithmResult([.. result], false);
    }

    private sealed class SnakePath(SnakePath? prev, int x, int y, int length)
    {
        public SnakePath? Prev { get; } = prev;

        public int X { get; } = x;

        public int Y { get; } = y;

        public int Length { get; } = length;
    }

    /// <summary>An array that supports fast negative indices.</summary>
    private sealed class FastInt32Array
    {
        private int[] _positiveArr = new int[10];
        private int[] _negativeArr = new int[10];

        /// <summary>A never-written slot reads 0 (a typed array in JS would give <c>undefined</c> past its end).</summary>
        public int Get(int idx)
        {
            int[] arr = idx < 0 ? _negativeArr : _positiveArr;
            int i = idx < 0 ? -idx - 1 : idx;
            return i < arr.Length ? arr[i] : 0;
        }

        public void Set(int idx, int value)
        {
            if (idx < 0)
            {
                idx = -idx - 1;
                while (idx >= _negativeArr.Length)
                {
                    Array.Resize(ref _negativeArr, _negativeArr.Length * 2);
                }

                _negativeArr[idx] = value;
            }
            else
            {
                while (idx >= _positiveArr.Length)
                {
                    Array.Resize(ref _positiveArr, _positiveArr.Length * 2);
                }

                _positiveArr[idx] = value;
            }
        }
    }

    /// <summary>An array that supports fast negative indices.</summary>
    private sealed class FastArrayNegativeIndices<T>
    {
        private readonly List<T> _positiveArr = [];
        private readonly List<T> _negativeArr = [];

        public T Get(int idx)
        {
            List<T> list = idx < 0 ? _negativeArr : _positiveArr;
            int i = idx < 0 ? -idx - 1 : idx;

            // JS returns undefined (falsy, like null) for a slot that was never set.
            return i < list.Count ? list[i] : default!;
        }

        public void Set(int idx, T value)
        {
            List<T> list = idx < 0 ? _negativeArr : _positiveArr;
            int i = idx < 0 ? -idx - 1 : idx;

            // JS arrays grow on assignment past the end; a List needs the gap filled.
            while (list.Count <= i)
            {
                list.Add(default!);
            }

            list[i] = value;
        }
    }
}
