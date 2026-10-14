// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/lineHeights.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

// Document changes (lines deleted / inserted), the binary search and the small helper types.
internal sealed partial class LineHeightsManager
{
    private void DoLinesDeleted(int fromLineNumber, int toLineNumber)
    {
        var deleteCount = toLineNumber - fromLineNumber + 1;
        var numberOfCustomLines = _orderedCustomLines.Count;
        var candidateStartIndexOfDeletion = BinarySearchOverOrderedCustomLinesArray(fromLineNumber);
        int startIndexOfDeletion;
        if (candidateStartIndexOfDeletion >= 0)
        {
            startIndexOfDeletion = candidateStartIndexOfDeletion;
            for (var i = candidateStartIndexOfDeletion - 1; i >= 0; i--)
            {
                if (_orderedCustomLines[i].LineNumber == fromLineNumber)
                {
                    startIndexOfDeletion--;
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            startIndexOfDeletion = candidateStartIndexOfDeletion == -(numberOfCustomLines + 1) && candidateStartIndexOfDeletion != -1
                ? numberOfCustomLines - 1
                : -(candidateStartIndexOfDeletion + 1);
        }

        var candidateEndIndexOfDeletion = BinarySearchOverOrderedCustomLinesArray(toLineNumber);
        int endIndexOfDeletion;
        if (candidateEndIndexOfDeletion >= 0)
        {
            endIndexOfDeletion = candidateEndIndexOfDeletion;
            for (var i = candidateEndIndexOfDeletion + 1; i < numberOfCustomLines; i++)
            {
                if (_orderedCustomLines[i].LineNumber == toLineNumber)
                {
                    endIndexOfDeletion++;
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            endIndexOfDeletion = candidateEndIndexOfDeletion == -(numberOfCustomLines + 1) && candidateEndIndexOfDeletion != -1
                ? numberOfCustomLines - 1
                : -(candidateEndIndexOfDeletion + 1);
        }

        var isEndIndexBiggerThanStartIndex = endIndexOfDeletion > startIndexOfDeletion;
        var isEndIndexEqualToStartIndexAndCoversCustomLine = endIndexOfDeletion == startIndexOfDeletion
            && startIndexOfDeletion < numberOfCustomLines
            && _orderedCustomLines[startIndexOfDeletion].LineNumber >= fromLineNumber
            && _orderedCustomLines[startIndexOfDeletion].LineNumber <= toLineNumber;

        if (isEndIndexBiggerThanStartIndex || isEndIndexEqualToStartIndexAndCoversCustomLine)
        {
            var maximumSpecialHeightOnDeletedInterval = 0;
            for (var i = startIndexOfDeletion; i <= endIndexOfDeletion; i++)
            {
                maximumSpecialHeightOnDeletedInterval = Math.Max(maximumSpecialHeightOnDeletedInterval, _orderedCustomLines[i].MaximumSpecialHeight);
            }

            int prefixSumOnDeletedInterval;
            if (startIndexOfDeletion > 0)
            {
                var previousSpecialLine = _orderedCustomLines[startIndexOfDeletion - 1];
                prefixSumOnDeletedInterval = previousSpecialLine.PrefixSum + previousSpecialLine.MaximumSpecialHeight
                    + (DefaultLineHeight * (fromLineNumber - previousSpecialLine.LineNumber - 1));
            }
            else
            {
                prefixSumOnDeletedInterval = fromLineNumber > 0 ? (fromLineNumber - 1) * DefaultLineHeight : 0;
            }

            var firstSpecialLineDeleted = _orderedCustomLines[startIndexOfDeletion];
            var lastSpecialLineDeleted = _orderedCustomLines[endIndexOfDeletion];
            var firstSpecialLineAfterDeletion = endIndexOfDeletion + 1 < numberOfCustomLines ? _orderedCustomLines[endIndexOfDeletion + 1] : null;
            var heightOfFirstLineAfterDeletion = firstSpecialLineAfterDeletion is not null && firstSpecialLineAfterDeletion.LineNumber == toLineNumber + 1
                ? firstSpecialLineAfterDeletion.MaximumSpecialHeight
                : DefaultLineHeight;
            var totalHeightDeleted = lastSpecialLineDeleted.PrefixSum
                + lastSpecialLineDeleted.MaximumSpecialHeight
                - firstSpecialLineDeleted.PrefixSum
                + (DefaultLineHeight * (toLineNumber - lastSpecialLineDeleted.LineNumber))
                + (DefaultLineHeight * (firstSpecialLineDeleted.LineNumber - fromLineNumber))
                + heightOfFirstLineAfterDeletion - maximumSpecialHeightOnDeletedInterval;

            var decorationIdsSeen = new HashSet<string>();
            var newOrderedCustomLines = new List<CustomLine>();
            var newDecorationIdToSpecialLine = new ArrayMap<string, CustomLine>();
            var numberOfDeletions = 0;
            for (var i = 0; i < _orderedCustomLines.Count; i++)
            {
                var customLine = _orderedCustomLines[i];
                if (i < startIndexOfDeletion)
                {
                    newOrderedCustomLines.Add(customLine);
                    newDecorationIdToSpecialLine.Add(customLine.DecorationId, customLine);
                }
                else if (i >= startIndexOfDeletion && i <= endIndexOfDeletion)
                {
                    var decorationId = customLine.DecorationId;
                    if (!decorationIdsSeen.Contains(decorationId))
                    {
                        customLine.Index -= numberOfDeletions;
                        customLine.LineNumber = fromLineNumber;
                        customLine.PrefixSum = prefixSumOnDeletedInterval;
                        customLine.MaximumSpecialHeight = maximumSpecialHeightOnDeletedInterval;
                        newOrderedCustomLines.Add(customLine);
                        newDecorationIdToSpecialLine.Add(customLine.DecorationId, customLine);
                    }
                    else
                    {
                        numberOfDeletions++;
                    }
                }
                else if (i > endIndexOfDeletion)
                {
                    customLine.Index -= numberOfDeletions;
                    customLine.LineNumber -= deleteCount;
                    customLine.PrefixSum -= totalHeightDeleted;
                    newOrderedCustomLines.Add(customLine);
                    newDecorationIdToSpecialLine.Add(customLine.DecorationId, customLine);
                }

                decorationIdsSeen.Add(customLine.DecorationId);
            }

            _orderedCustomLines = newOrderedCustomLines;
            _decorationIdToCustomLine = newDecorationIdToSpecialLine;
        }
        else
        {
            var totalHeightDeleted = deleteCount * DefaultLineHeight;
            for (var i = endIndexOfDeletion; i < _orderedCustomLines.Count; i++)
            {
                var customLine = _orderedCustomLines[i];
                if (customLine.LineNumber > toLineNumber)
                {
                    customLine.LineNumber -= deleteCount;
                    customLine.PrefixSum -= totalHeightDeleted;
                }
            }
        }
    }

    private void DoLinesInserted(int fromLineNumber, int toLineNumber, List<CustomLine> stagedInserts, ArrayMap<string, CustomLine> stagedIdMap)
    {
        var insertCount = toLineNumber - fromLineNumber + 1;
        var candidateStartIndexOfInsertion = BinarySearchOverOrderedCustomLinesArray(fromLineNumber);
        int startIndexOfInsertion;
        if (candidateStartIndexOfInsertion >= 0)
        {
            startIndexOfInsertion = candidateStartIndexOfInsertion;
            for (var i = candidateStartIndexOfInsertion - 1; i >= 0; i--)
            {
                if (_orderedCustomLines[i].LineNumber == fromLineNumber)
                {
                    startIndexOfInsertion--;
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            startIndexOfInsertion = -(candidateStartIndexOfInsertion + 1);
        }

        var toReAdd = new List<CustomLineHeightData>();
        var decorationsImmediatelyAfter = new HashSet<string>();
        for (var i = startIndexOfInsertion; i < _orderedCustomLines.Count; i++)
        {
            if (_orderedCustomLines[i].LineNumber == fromLineNumber)
            {
                decorationsImmediatelyAfter.Add(_orderedCustomLines[i].DecorationId);
            }
        }

        var decorationsImmediatelyBefore = new HashSet<string>();
        for (var i = startIndexOfInsertion - 1; i >= 0; i--)
        {
            if (_orderedCustomLines[i].LineNumber == fromLineNumber - 1)
            {
                decorationsImmediatelyBefore.Add(_orderedCustomLines[i].DecorationId);
            }
        }

        // Monaco: intersection(before, after), in the iteration order of `before`.
        var decorationsWithGaps = new List<string>();
        foreach (var decorationId in decorationsImmediatelyBefore)
        {
            if (decorationsImmediatelyAfter.Contains(decorationId))
            {
                decorationsWithGaps.Add(decorationId);
            }
        }

        var prefixSumToAdd = insertCount * DefaultLineHeight;
        for (var i = startIndexOfInsertion; i < _orderedCustomLines.Count; i++)
        {
            _orderedCustomLines[i].LineNumber += insertCount;
            _orderedCustomLines[i].PrefixSum += prefixSumToAdd;
        }

        if (decorationsWithGaps.Count > 0)
        {
            foreach (var decorationId in decorationsWithGaps)
            {
                var decoration = _decorationIdToCustomLine.Get(decorationId);
                if (decoration is not null)
                {
                    var startLineNumber = fromLineNumber;
                    var endLineNumber = fromLineNumber;
                    var lineHeight = 0;
                    foreach (var line in decoration)
                    {
                        startLineNumber = Math.Min(startLineNumber, line.LineNumber);
                        endLineNumber = Math.Max(endLineNumber, line.LineNumber);
                        lineHeight = Math.Max(lineHeight, line.SpecialHeight);
                    }

                    toReAdd.Add(new CustomLineHeightData(decorationId, startLineNumber, endLineNumber, lineHeight));
                }
            }

            foreach (var dec in toReAdd)
            {
                DoInsertOrChangeCustomLineHeight(dec.DecorationId, dec.StartLineNumber, dec.EndLineNumber, dec.LineHeight, stagedInserts, stagedIdMap);
            }
        }
    }

    private int BinarySearchOverOrderedCustomLinesArray(int lineNumber)
    {
        return BinarySearch2(_orderedCustomLines.Count, index =>
        {
            var line = _orderedCustomLines[index];
            if (line.LineNumber == lineNumber)
            {
                return 0;
            }

            return line.LineNumber < lineNumber ? -1 : 1;
        });
    }

    // Ported from arrays.ts: binarySearch2. Returns -(insertionIndex + 1) when not found (not ~index like Array.BinarySearch).
    private static int BinarySearch2(int length, Func<int, int> compareToKey)
    {
        var low = 0;
        var high = length - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var comp = compareToKey(mid);
            if (comp < 0)
            {
                low = mid + 1;
            }
            else if (comp > 0)
            {
                high = mid - 1;
            }
            else
            {
                return mid;
            }
        }

        return -(low + 1);
    }

    private abstract record PendingChange;

    private sealed record InsertOrChangeChange(string DecorationId, int StartLineNumber, int EndLineNumber, int LineHeight) : PendingChange;

    private sealed record RemoveChange(string DecorationId) : PendingChange;

    private sealed record LinesDeletedChange(int FromLineNumber, int ToLineNumber) : PendingChange;

    private sealed record LinesInsertedChange(int FromLineNumber, int ToLineNumber) : PendingChange;

    private sealed class ArrayMap<TKey, T>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, List<T>> _map = [];

        public void Add(TKey key, T value)
        {
            if (!_map.TryGetValue(key, out var list))
            {
                _map[key] = [value];
            }
            else
            {
                list.Add(value);
            }
        }

        public List<T>? Get(TKey key) => _map.GetValueOrDefault(key);

        public void Delete(TKey key) => _map.Remove(key);

        public void Clear() => _map.Clear();
    }
}
