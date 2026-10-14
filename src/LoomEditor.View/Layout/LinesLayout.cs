// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/linesLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Layouting of objects that take vertical space (by having a height) and push down other objects.
/// These objects are either text (lines) or spaces between those lines (whitespaces).
/// </summary>
/// <remarks>Threading: not thread-safe; use from the UI thread only. Only the instance counter is shared and thread-safe.</remarks>
internal sealed partial class LinesLayout
{
    private static int _instanceCount;

    private readonly string _instanceId;
    private readonly PendingChanges _pendingChanges;
    private int _lastWhitespaceId;
    private List<EditorWhitespace> _arr;
    private int _prefixSumValidIndex;
    private int _minWidth;
    private int _lineCount;
    private int _paddingTop;
    private int _paddingBottom;
    private LineHeightsManager _lineHeightsManager;

    public LinesLayout(int lineCount, int defaultLineHeight, int paddingTop, int paddingBottom, IReadOnlyList<CustomLineHeightData> customLineHeightData)
    {
        _instanceId = SingleLetterHash(Interlocked.Increment(ref _instanceCount));
        _pendingChanges = new PendingChanges();
        _lastWhitespaceId = 0;
        _arr = [];
        _prefixSumValidIndex = -1;
        _minWidth = -1; // marker for not being computed
        _lineCount = lineCount;
        _paddingTop = paddingTop;
        _paddingBottom = paddingBottom;
        _lineHeightsManager = new LineHeightsManager(defaultLineHeight, customLineHeightData);
    }

    // Monaco: strings.singleLetterHash. Produces 'a'-'z', followed by 'A'-'Z'... followed by 'a'-'z', etc.
    private static string SingleLetterHash(int n)
    {
        const int LettersCount = 'Z' - 'A' + 1;

        n %= 2 * LettersCount;
        return n < LettersCount
            ? ((char)('a' + n)).ToString()
            : ((char)('A' + n - LettersCount)).ToString();
    }

    /// <summary>
    /// Find the insertion index for a new value inside a sorted array of values.
    /// If the value is already present in the sorted array, the insertion index will be after the already existing value.
    /// </summary>
    public static int FindInsertionIndex(IReadOnlyList<EditorWhitespace> arr, int afterLineNumber, int ordinal)
    {
        var low = 0;
        var high = arr.Count;

        while (low < high)
        {
            var mid = (low + high) >>> 1;

            if (afterLineNumber == arr[mid].AfterLineNumber)
            {
                if (ordinal < arr[mid].Ordinal)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }
            else if (afterLineNumber < arr[mid].AfterLineNumber)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }

    /// <summary>Change the height of a line in pixels.</summary>
    public void SetDefaultLineHeight(int lineHeight)
    {
        _lineHeightsManager.DefaultLineHeight = lineHeight;
    }

    /// <summary>Changes the padding used to calculate vertical offsets.</summary>
    public void SetPadding(int paddingTop, int paddingBottom)
    {
        _paddingTop = paddingTop;
        _paddingBottom = paddingBottom;
    }

    /// <summary>Set the number of lines.</summary>
    public void OnFlushed(int lineCount, IReadOnlyList<CustomLineHeightData> customLineHeightData)
    {
        _lineCount = lineCount;
        _lineHeightsManager = new LineHeightsManager(_lineHeightsManager.DefaultLineHeight, customLineHeightData);
    }

    public bool ChangeLineHeights(Action<ILineHeightChangeAccessor> callback)
    {
        var accessor = new LineHeightChangeAccessor(_lineHeightsManager);
        callback(accessor);
        return accessor.HadAChange;
    }

    public bool ChangeWhitespace(Action<IWhitespaceChangeAccessor> callback)
    {
        var accessor = new WhitespaceChangeAccessor(this);
        try
        {
            callback(accessor);
        }
        finally
        {
            _pendingChanges.Commit(this);
        }

        return accessor.HadAChange;
    }

    internal void CommitPendingChanges(List<EditorWhitespace> inserts, List<PendingChange> changes, List<PendingRemove> removes)
    {
        if (inserts.Count > 0 || removes.Count > 0)
        {
            _minWidth = -1; // marker for not being computed
        }

        if (inserts.Count + changes.Count + removes.Count <= 1)
        {
            // when only one thing happened, handle it "delicately"
            foreach (var insert in inserts)
            {
                InsertWhitespaceCore(insert);
            }

            foreach (var change in changes)
            {
                ChangeOneWhitespaceCore(change.Id, change.NewAfterLineNumber, change.NewHeight);
            }

            foreach (var remove in removes)
            {
                var index = FindWhitespaceIndex(remove.Id);
                if (index == -1)
                {
                    continue;
                }

                RemoveWhitespaceCore(index);
            }

            return;
        }

        // simply rebuild the entire datastructure
        var toRemove = new HashSet<string>();
        foreach (var remove in removes)
        {
            toRemove.Add(remove.Id);
        }

        var toChange = new Dictionary<string, PendingChange>();
        foreach (var change in changes)
        {
            toChange[change.Id] = change;
        }

        List<EditorWhitespace> ApplyRemoveAndChange(List<EditorWhitespace> whitespaces)
        {
            var result = new List<EditorWhitespace>();
            foreach (var whitespace in whitespaces)
            {
                if (toRemove.Contains(whitespace.Id))
                {
                    continue;
                }

                if (toChange.TryGetValue(whitespace.Id, out var change))
                {
                    whitespace.AfterLineNumber = change.NewAfterLineNumber;
                    whitespace.Height = change.NewHeight;
                }

                result.Add(whitespace);
            }

            return result;
        }

        var merged = ApplyRemoveAndChange(_arr);
        merged.AddRange(ApplyRemoveAndChange(inserts));

        // OrderBy is stable, like Array.prototype.sort.
        _arr = [.. merged.OrderBy(static w => w.AfterLineNumber).ThenBy(static w => w.Ordinal)];
        _prefixSumValidIndex = -1;
    }

    private void InsertWhitespaceCore(EditorWhitespace whitespace)
    {
        var insertIndex = FindInsertionIndex(_arr, whitespace.AfterLineNumber, whitespace.Ordinal);
        _arr.Insert(insertIndex, whitespace);
        _prefixSumValidIndex = Math.Min(_prefixSumValidIndex, insertIndex - 1);
    }

    private int FindWhitespaceIndex(string id)
    {
        var arr = _arr;
        for (var i = 0; i < arr.Count; i++)
        {
            if (arr[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void ChangeOneWhitespaceCore(string id, int newAfterLineNumber, int newHeight)
    {
        var index = FindWhitespaceIndex(id);
        if (index == -1)
        {
            return;
        }

        if (_arr[index].Height != newHeight)
        {
            _arr[index].Height = newHeight;
            _prefixSumValidIndex = Math.Min(_prefixSumValidIndex, index - 1);
        }

        if (_arr[index].AfterLineNumber != newAfterLineNumber)
        {
            // `AfterLineNumber` changed for this whitespace
            var whitespace = _arr[index];

            // Since changing `AfterLineNumber` can trigger a reordering, we're gonna remove this whitespace
            RemoveWhitespaceCore(index);

            whitespace.AfterLineNumber = newAfterLineNumber;

            // And add it again
            InsertWhitespaceCore(whitespace);
        }
    }

    private void RemoveWhitespaceCore(int removeIndex)
    {
        _arr.RemoveAt(removeIndex);
        _prefixSumValidIndex = Math.Min(_prefixSumValidIndex, removeIndex - 1);
    }

    /// <summary>
    /// Notify the layouter that lines have been deleted (a continuous zone of lines).
    /// </summary>
    /// <param name="fromLineNumber">The line number at which the deletion started, inclusive.</param>
    /// <param name="toLineNumber">The line number at which the deletion ended, inclusive.</param>
    public void OnLinesDeleted(int fromLineNumber, int toLineNumber)
    {
        _lineCount -= toLineNumber - fromLineNumber + 1;
        for (var i = 0; i < _arr.Count; i++)
        {
            var afterLineNumber = _arr[i].AfterLineNumber;

            if (fromLineNumber <= afterLineNumber && afterLineNumber <= toLineNumber)
            {
                // The line this whitespace was after has been deleted
                //  => move whitespace to before first deleted line
                _arr[i].AfterLineNumber = fromLineNumber - 1;
            }
            else if (afterLineNumber > toLineNumber)
            {
                // The line this whitespace was after has been moved up
                //  => move whitespace up
                _arr[i].AfterLineNumber -= toLineNumber - fromLineNumber + 1;
            }
        }

        _lineHeightsManager.OnLinesDeleted(fromLineNumber, toLineNumber);
    }

    /// <summary>
    /// Notify the layouter that lines have been inserted (a continuous zone of lines).
    /// </summary>
    /// <param name="fromLineNumber">The line number at which the insertion started, inclusive.</param>
    /// <param name="toLineNumber">The line number at which the insertion ended, inclusive.</param>
    public void OnLinesInserted(int fromLineNumber, int toLineNumber)
    {
        _lineCount += toLineNumber - fromLineNumber + 1;
        for (var i = 0; i < _arr.Count; i++)
        {
            var afterLineNumber = _arr[i].AfterLineNumber;

            if (fromLineNumber <= afterLineNumber)
            {
                _arr[i].AfterLineNumber += toLineNumber - fromLineNumber + 1;
            }
        }

        _lineHeightsManager.OnLinesInserted(fromLineNumber, toLineNumber);
    }

    /// <summary>Get the sum of heights for all objects.</summary>
    public int GetLinesTotalHeight()
    {
        var linesHeight = _lineHeightsManager.GetAccumulatedLineHeightsIncludingLineNumber(_lineCount);
        var whitespacesHeight = GetWhitespacesTotalHeight();

        return linesHeight + whitespacesHeight + _paddingTop + _paddingBottom;
    }

    /// <summary>
    /// Get the vertical offset (the sum of heights for all objects above) a certain line number.
    /// </summary>
    public int GetVerticalOffsetForLineNumber(int lineNumber, bool includeViewZones = false)
    {
        var previousLinesHeight = lineNumber > 1
            ? _lineHeightsManager.GetAccumulatedLineHeightsIncludingLineNumber(lineNumber - 1)
            : 0;

        var previousWhitespacesHeight = GetWhitespaceAccumulatedHeightBeforeLineNumber(lineNumber - (includeViewZones ? 1 : 0));

        return previousLinesHeight + previousWhitespacesHeight + _paddingTop;
    }

    public int GetLineHeightForLineNumber(int lineNumber)
    {
        return _lineHeightsManager.HeightForLineNumber(lineNumber);
    }

    /// <summary>
    /// Get the vertical offset (the sum of heights for all objects above) just after a certain line number.
    /// </summary>
    public int GetVerticalOffsetAfterLineNumber(int lineNumber, bool includeViewZones = false)
    {
        var previousLinesHeight = _lineHeightsManager.GetAccumulatedLineHeightsIncludingLineNumber(lineNumber);
        var previousWhitespacesHeight = GetWhitespaceAccumulatedHeightBeforeLineNumber(lineNumber + (includeViewZones ? 1 : 0));
        return previousLinesHeight + previousWhitespacesHeight + _paddingTop;
    }

    /// <summary>Check if <paramref name="verticalOffset"/> is below all lines.</summary>
    public bool IsAfterLines(int verticalOffset)
    {
        var totalHeight = GetLinesTotalHeight();
        return verticalOffset > totalHeight;
    }

    public bool IsInTopPadding(int verticalOffset)
    {
        if (_paddingTop == 0)
        {
            return false;
        }

        return verticalOffset < _paddingTop;
    }

    public bool IsInBottomPadding(int verticalOffset)
    {
        if (_paddingBottom == 0)
        {
            return false;
        }

        var totalHeight = GetLinesTotalHeight();
        return verticalOffset >= totalHeight - _paddingBottom;
    }

    /// <summary>
    /// Find the first line number that is at or after vertical offset <paramref name="verticalOffset"/>.
    /// i.e. if GetVerticalOffsetForLineNumber(line) is x and GetVerticalOffsetForLineNumber(line + 1) is y, then
    /// GetLineNumberAtOrAfterVerticalOffset(i) = line, x &lt;= i &lt; y.
    /// </summary>
    public int GetLineNumberAtOrAfterVerticalOffset(int verticalOffset)
    {
        if (verticalOffset < 0)
        {
            return 1;
        }

        var linesCount = _lineCount;
        var minLineNumber = 1;
        var maxLineNumber = linesCount;

        while (minLineNumber < maxLineNumber)
        {
            var midLineNumber = (minLineNumber + maxLineNumber) / 2;

            var lineHeight = GetLineHeightForLineNumber(midLineNumber);
            var midLineNumberVerticalOffset = GetVerticalOffsetForLineNumber(midLineNumber);

            if (verticalOffset >= midLineNumberVerticalOffset + lineHeight)
            {
                // vertical offset is after mid line number
                minLineNumber = midLineNumber + 1;
            }
            else if (verticalOffset >= midLineNumberVerticalOffset)
            {
                // Hit
                return midLineNumber;
            }
            else
            {
                // vertical offset is before mid line number, but mid line number could still be what we're searching for
                maxLineNumber = midLineNumber;
            }
        }

        if (minLineNumber > linesCount)
        {
            return linesCount;
        }

        return minLineNumber;
    }
}
