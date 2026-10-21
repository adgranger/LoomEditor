// Ported from microsoft/vscode: src/vs/editor/common/model/prefixSumComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The result of <see cref="PrefixSumComputer.GetIndexOf"/> (Monaco: <c>PrefixSumIndexOfResult</c>).</summary>
/// <param name="Index">The index of the value that contains the sum.</param>
/// <param name="Remainder">The sum minus the sum of the values before <paramref name="Index"/>.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct PrefixSumIndexOfResult(int Index, int Remainder);

/// <summary>
/// Sums of the first values of an array that is edited (Monaco: <c>PrefixSumComputer</c>). Values are
/// <c>uint</c> like Monaco's <c>Uint32Array</c>; sums are returned as <c>int</c>.
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class PrefixSumComputer
{
    // _values[i] is the value at index i.
    private uint[] _values;

    // _prefixSum[i] = SUM(_values[j]), 0 <= j <= i.
    private uint[] _prefixSum;

    // _prefixSum[i], 0 <= i <= _prefixSumValidIndex can be trusted.
    private int _prefixSumValidIndex = -1;

    /// <summary>Creates a computer over <paramref name="values"/> (not copied).</summary>
    public PrefixSumComputer(uint[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
        _prefixSum = new uint[values.Length];
    }

    /// <summary><c>getCount</c>.</summary>
    public int GetCount() => _values.Length;

    /// <summary><c>insertValues</c>: returns false when nothing was inserted.</summary>
    public bool InsertValues(int insertIndex, uint[] insertValues)
    {
        ArgumentNullException.ThrowIfNull(insertValues);
        uint[] oldValues = _values;
        uint[] oldPrefixSum = _prefixSum;
        int insertValuesLen = insertValues.Length;

        if (insertValuesLen == 0)
        {
            return false;
        }

        _values = new uint[oldValues.Length + insertValuesLen];
        Array.Copy(oldValues, 0, _values, 0, insertIndex);
        Array.Copy(oldValues, insertIndex, _values, insertIndex + insertValuesLen, oldValues.Length - insertIndex);
        Array.Copy(insertValues, 0, _values, insertIndex, insertValuesLen);

        if (insertIndex - 1 < _prefixSumValidIndex)
        {
            _prefixSumValidIndex = insertIndex - 1;
        }

        _prefixSum = new uint[_values.Length];
        if (_prefixSumValidIndex >= 0)
        {
            Array.Copy(oldPrefixSum, _prefixSum, _prefixSumValidIndex + 1);
        }

        return true;
    }

    /// <summary><c>setValue</c>: returns false when the value did not change.</summary>
    public bool SetValue(int index, int value)
    {
        uint newValue = unchecked((uint)value);
        if (_values[index] == newValue)
        {
            return false;
        }

        _values[index] = newValue;
        if (index - 1 < _prefixSumValidIndex)
        {
            _prefixSumValidIndex = index - 1;
        }

        return true;
    }

    /// <summary><c>removeValues</c>: returns false when nothing was removed.</summary>
    public bool RemoveValues(int startIndex, int count)
    {
        uint[] oldValues = _values;
        uint[] oldPrefixSum = _prefixSum;

        if (startIndex >= oldValues.Length)
        {
            return false;
        }

        int maxCount = oldValues.Length - startIndex;
        if (count >= maxCount)
        {
            count = maxCount;
        }

        if (count == 0)
        {
            return false;
        }

        _values = new uint[oldValues.Length - count];
        Array.Copy(oldValues, 0, _values, 0, startIndex);
        Array.Copy(oldValues, startIndex + count, _values, startIndex, oldValues.Length - startIndex - count);

        _prefixSum = new uint[_values.Length];
        if (startIndex - 1 < _prefixSumValidIndex)
        {
            _prefixSumValidIndex = startIndex - 1;
        }

        if (_prefixSumValidIndex >= 0)
        {
            Array.Copy(oldPrefixSum, _prefixSum, _prefixSumValidIndex + 1);
        }

        return true;
    }

    /// <summary><c>getTotalSum</c>.</summary>
    public int GetTotalSum() => _values.Length == 0 ? 0 : GetPrefixSumCore(_values.Length - 1);

    /// <summary><c>getPrefixSum</c>: the sum of the first <c>index + 1</c> values, <c>SUM(0 &lt;= j &lt;= index, values[j])</c>.</summary>
    public int GetPrefixSum(int index) => index < 0 ? 0 : GetPrefixSumCore(index);

    private int GetPrefixSumCore(int index)
    {
        if (index <= _prefixSumValidIndex)
        {
            return unchecked((int)_prefixSum[index]);
        }

        int startIndex = _prefixSumValidIndex + 1;
        if (startIndex == 0)
        {
            _prefixSum[0] = _values[0];
            startIndex++;
        }

        if (index >= _values.Length)
        {
            index = _values.Length - 1;
        }

        for (int i = startIndex; i <= index; i++)
        {
            _prefixSum[i] = unchecked(_prefixSum[i - 1] + _values[i]);
        }

        _prefixSumValidIndex = Math.Max(_prefixSumValidIndex, index);
        return unchecked((int)_prefixSum[index]);
    }

    /// <summary><c>getIndexOf</c>: binary search of the value that contains <paramref name="sum"/>.</summary>
    public PrefixSumIndexOfResult GetIndexOf(int sum)
    {
        // Compute all sums (to get a fully valid prefixSum)
        GetTotalSum();

        int low = 0;
        int high = _values.Length - 1;
        int mid = 0;
        uint midStop = 0;
        uint midStart = 0;

        while (low <= high)
        {
            mid = low + ((high - low) / 2);

            midStop = _prefixSum[mid];
            midStart = unchecked(midStop - _values[mid]);

            if (sum < midStart)
            {
                high = mid - 1;
            }
            else if (sum >= midStop)
            {
                low = mid + 1;
            }
            else
            {
                break;
            }
        }

        return new PrefixSumIndexOfResult(mid, unchecked(sum - (int)midStart));
    }
}
