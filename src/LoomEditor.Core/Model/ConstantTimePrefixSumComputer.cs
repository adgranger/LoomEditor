// Ported from microsoft/vscode: src/vs/editor/common/model/prefixSumComputer.ts (ConstantTimePrefixSumComputer)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>
/// Sums of the first values of an array that is edited, with <see cref="GetIndexOf"/> in amortized constant time
/// (Monaco: <c>ConstantTimePrefixSumComputer</c>). <see cref="PrefixSumComputer.GetIndexOf"/> is O(log n).
/// </summary>
/// <remarks>
/// Differences from <see cref="PrefixSumComputer"/>: values are <c>int</c>, and <see cref="GetPrefixSum"/> takes a count
/// (the sum of the first <c>count</c> values), not an index. Like JavaScript's <c>splice</c>, <see cref="RemoveValues"/>
/// clamps its range.
/// Threading: not thread-safe.
/// </remarks>
internal sealed class ConstantTimePrefixSumComputer
{
    private readonly List<int> _values;
    private bool _isValid;
    private int _validEndIndex = -1;

    // _prefixSum[i] = SUM(values[j]), 0 <= j <= i
    private readonly List<int> _prefixSum = [];

    // _indexBySum[sum] = idx => _prefixSum[idx - 1] <= sum < _prefixSum[idx]
    private readonly List<int> _indexBySum = [];

    /// <summary>Creates a computer over <paramref name="values"/> (not copied).</summary>
    public ConstantTimePrefixSumComputer(List<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
    }

    /// <summary>SUM(0 &lt;= j &lt; values.length, values[j]).</summary>
    public int GetTotalSum()
    {
        EnsureValid();
        return _indexBySum.Count;
    }

    /// <summary>The sum of the first <paramref name="count"/> many items.</summary>
    public int GetPrefixSum(int count)
    {
        EnsureValid();
        return count == 0 ? 0 : _prefixSum[count - 1];
    }

    /// <summary>The result such that <c>GetPrefixSum(result.Index) + result.Remainder = sum</c>.</summary>
    public PrefixSumIndexOfResult GetIndexOf(int sum)
    {
        EnsureValid();
        if ((uint)sum >= (uint)_indexBySum.Count)
        {
            // sum does not have a direct entry in _indexBySum (e.g. sum >= GetTotalSum() or the array is empty / all values are zero)
            int lastIdx = Math.Max(0, _values.Count - 1);
            int lastPrefixSum = lastIdx > 0 ? _prefixSum[lastIdx - 1] : 0;
            return new PrefixSumIndexOfResult(lastIdx, sum - lastPrefixSum);
        }

        int idx = _indexBySum[sum];
        int viewLinesAbove = idx > 0 ? _prefixSum[idx - 1] : 0;
        return new PrefixSumIndexOfResult(idx, sum - viewLinesAbove);
    }

    /// <summary>Removes <paramref name="deleteCount"/> values from <paramref name="start"/>.</summary>
    public void RemoveValues(int start, int deleteCount)
    {
        start = Math.Min(start, _values.Count);
        _values.RemoveRange(start, Math.Min(deleteCount, _values.Count - start));
        Invalidate(start);
    }

    /// <summary>Inserts <paramref name="insertArr"/> before <paramref name="insertIndex"/>.</summary>
    public void InsertValues(int insertIndex, IReadOnlyList<int> insertArr)
    {
        ArgumentNullException.ThrowIfNull(insertArr);
        _values.InsertRange(insertIndex, insertArr);
        Invalidate(insertIndex);
    }

    /// <summary>Sets the value at <paramref name="index"/>.</summary>
    public void SetValue(int index, int value)
    {
        if (_values[index] == value)
        {
            // no change
            return;
        }

        _values[index] = value;
        Invalidate(index);
    }

    private void Invalidate(int index)
    {
        _isValid = false;
        _validEndIndex = Math.Min(_validEndIndex, index - 1);
    }

    private static void SetAt(List<int> list, int index, int value)
    {
        // JavaScript arrays grow when written past their end; the writes here are always contiguous.
        if (index == list.Count)
        {
            list.Add(value);
        }
        else
        {
            list[index] = value;
        }
    }

    private static void SetLength(List<int> list, int length)
    {
        if (list.Count > length)
        {
            list.RemoveRange(length, list.Count - length);
        }
    }

    private void EnsureValid()
    {
        if (_isValid)
        {
            return;
        }

        for (int i = _validEndIndex + 1, len = _values.Count; i < len; i++)
        {
            int value = _values[i];
            int sumAbove = i > 0 ? _prefixSum[i - 1] : 0;

            SetAt(_prefixSum, i, sumAbove + value);
            for (int j = 0; j < value; j++)
            {
                SetAt(_indexBySum, sumAbove + j, i);
            }
        }

        // trim things
        SetLength(_prefixSum, _values.Count);
        SetLength(_indexBySum, _values.Count > 0 ? _prefixSum[_values.Count - 1] : 0);

        // mark as valid
        _isValid = true;
        _validEndIndex = _values.Count - 1;
    }
}
