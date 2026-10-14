// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/linesLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

// Whitespace queries: accumulated heights, lookups by line number or vertical offset, accessors by index.
internal sealed partial class LinesLayout
{
    /// <summary>Get the sum of all the whitespaces.</summary>
    public int GetWhitespacesTotalHeight()
    {
        if (_arr.Count == 0)
        {
            return 0;
        }

        return GetWhitespacesAccumulatedHeight(_arr.Count - 1);
    }

    /// <summary>
    /// Return the sum of the heights of the whitespaces at [0..index], including the one at <paramref name="index"/>.
    /// </summary>
    public int GetWhitespacesAccumulatedHeight(int index)
    {
        var startIndex = Math.Max(0, _prefixSumValidIndex + 1);
        if (startIndex == 0)
        {
            _arr[0].PrefixSum = _arr[0].Height;
            startIndex++;
        }

        for (var i = startIndex; i <= index; i++)
        {
            _arr[i].PrefixSum = _arr[i - 1].PrefixSum + _arr[i].Height;
        }

        _prefixSumValidIndex = Math.Max(_prefixSumValidIndex, index);
        return _arr[index].PrefixSum;
    }

    /// <summary>Returns the accumulated height of whitespaces before the given line number.</summary>
    public int GetWhitespaceAccumulatedHeightBeforeLineNumber(int lineNumber)
    {
        var lastWhitespaceBeforeLineNumber = FindLastWhitespaceBeforeLineNumber(lineNumber);

        if (lastWhitespaceBeforeLineNumber == -1)
        {
            return 0;
        }

        return GetWhitespacesAccumulatedHeight(lastWhitespaceBeforeLineNumber);
    }

    private int FindLastWhitespaceBeforeLineNumber(int lineNumber)
    {
        // Find the whitespace before line number
        var arr = _arr;
        var low = 0;
        var high = arr.Count - 1;

        while (low <= high)
        {
            var delta = high - low;
            var halfDelta = delta / 2;
            var mid = low + halfDelta;

            if (arr[mid].AfterLineNumber < lineNumber)
            {
                if (mid + 1 >= arr.Count || arr[mid + 1].AfterLineNumber >= lineNumber)
                {
                    return mid;
                }

                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return -1;
    }

    private int FindFirstWhitespaceAfterLineNumber(int lineNumber)
    {
        var lastWhitespaceBeforeLineNumber = FindLastWhitespaceBeforeLineNumber(lineNumber);
        var firstWhitespaceAfterLineNumber = lastWhitespaceBeforeLineNumber + 1;

        if (firstWhitespaceAfterLineNumber < _arr.Count)
        {
            return firstWhitespaceAfterLineNumber;
        }

        return -1;
    }

    /// <summary>
    /// Find the index of the first whitespace which has <c>AfterLineNumber</c> &gt;= <paramref name="lineNumber"/>.
    /// </summary>
    /// <returns>The index, or -1 if no whitespace is found.</returns>
    public int GetFirstWhitespaceIndexAfterLineNumber(int lineNumber)
    {
        return FindFirstWhitespaceAfterLineNumber(lineNumber);
    }

    /// <summary>Returns if there is any whitespace in the document.</summary>
    public bool HasWhitespace()
    {
        return GetWhitespacesCount() > 0;
    }

    /// <summary>The maximum min width for all whitespaces.</summary>
    public int GetWhitespaceMinWidth()
    {
        if (_minWidth == -1)
        {
            var minWidth = 0;
            for (var i = 0; i < _arr.Count; i++)
            {
                minWidth = Math.Max(minWidth, _arr[i].MinWidth);
            }

            _minWidth = minWidth;
        }

        return _minWidth;
    }

    public int GetVerticalOffsetForWhitespaceIndex(int whitespaceIndex)
    {
        var afterLineNumber = GetAfterLineNumberForWhitespaceIndex(whitespaceIndex);

        var previousLinesHeight = afterLineNumber >= 1
            ? _lineHeightsManager.GetAccumulatedLineHeightsIncludingLineNumber(afterLineNumber)
            : 0;

        var previousWhitespacesHeight = whitespaceIndex > 0
            ? GetWhitespacesAccumulatedHeight(whitespaceIndex - 1)
            : 0;

        return previousLinesHeight + previousWhitespacesHeight + _paddingTop;
    }

    public int GetWhitespaceIndexAtOrAfterVerticallOffset(int verticalOffset)
    {
        var minWhitespaceIndex = 0;
        var maxWhitespaceIndex = GetWhitespacesCount() - 1;

        if (maxWhitespaceIndex < 0)
        {
            return -1;
        }

        // Special case: nothing to be found
        var maxWhitespaceVerticalOffset = GetVerticalOffsetForWhitespaceIndex(maxWhitespaceIndex);
        var maxWhitespaceHeight = GetHeightForWhitespaceIndex(maxWhitespaceIndex);
        if (verticalOffset >= maxWhitespaceVerticalOffset + maxWhitespaceHeight)
        {
            return -1;
        }

        while (minWhitespaceIndex < maxWhitespaceIndex)
        {
            var midWhitespaceIndex = (minWhitespaceIndex + maxWhitespaceIndex) / 2;

            var midWhitespaceVerticalOffset = GetVerticalOffsetForWhitespaceIndex(midWhitespaceIndex);
            var midWhitespaceHeight = GetHeightForWhitespaceIndex(midWhitespaceIndex);

            if (verticalOffset >= midWhitespaceVerticalOffset + midWhitespaceHeight)
            {
                // vertical offset is after whitespace
                minWhitespaceIndex = midWhitespaceIndex + 1;
            }
            else if (verticalOffset >= midWhitespaceVerticalOffset)
            {
                // Hit
                return midWhitespaceIndex;
            }
            else
            {
                // vertical offset is before whitespace, but midWhitespaceIndex might still be what we're searching for
                maxWhitespaceIndex = midWhitespaceIndex;
            }
        }

        return minWhitespaceIndex;
    }

    /// <summary>Get all whitespaces.</summary>
    public IReadOnlyList<IEditorWhitespace> GetWhitespaces()
    {
        return _arr.ToArray();
    }

    /// <summary>The number of whitespaces.</summary>
    public int GetWhitespacesCount()
    {
        return _arr.Count;
    }

    /// <summary>Get the id for whitespace at index <paramref name="index"/>.</summary>
    public string GetIdForWhitespaceIndex(int index)
    {
        return _arr[index].Id;
    }

    /// <summary>Get the <c>AfterLineNumber</c> for whitespace at index <paramref name="index"/>.</summary>
    public int GetAfterLineNumberForWhitespaceIndex(int index)
    {
        return _arr[index].AfterLineNumber;
    }

    /// <summary>Get the height for whitespace at index <paramref name="index"/>.</summary>
    public int GetHeightForWhitespaceIndex(int index)
    {
        return _arr[index].Height;
    }
}
