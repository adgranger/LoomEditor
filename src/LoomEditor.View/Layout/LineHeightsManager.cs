// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/lineHeights.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Manages line heights, with support for custom line heights from decorations. Monaco: <c>LineHeightsManager</c>.
/// </summary>
/// <remarks>
/// Lines with a custom height are kept in an array ordered by line number, each with a prefix sum, so that the
/// accumulated height up to a line is a binary search. Changes are queued and committed on the next read.
/// Threading: not thread-safe; UI thread only.
/// </remarks>
internal sealed partial class LineHeightsManager
{
    private const int InvalidIndexNone = int.MaxValue;

    private ArrayMap<string, CustomLine> _decorationIdToCustomLine = new();
    private List<CustomLine> _orderedCustomLines = [];
    private List<PendingChange> _pendingChanges = [];
    private int _invalidIndex = InvalidIndexNone;
    private bool _hasPending;

    public LineHeightsManager(int defaultLineHeight, IReadOnlyList<CustomLineHeightData> customLineHeightData)
    {
        DefaultLineHeight = defaultLineHeight;
        foreach (var data in customLineHeightData)
        {
            InsertOrChangeCustomLineHeight(data.DecorationId, data.StartLineNumber, data.EndLineNumber, data.LineHeight);
        }
    }

    public int DefaultLineHeight { get; set; }

    public void RemoveCustomLineHeight(string decorationId)
    {
        _pendingChanges.Add(new RemoveChange(decorationId));
        _hasPending = true;
    }

    public void InsertOrChangeCustomLineHeight(string decorationId, int startLineNumber, int endLineNumber, int lineHeight)
    {
        _pendingChanges.Add(new InsertOrChangeChange(decorationId, startLineNumber, endLineNumber, lineHeight));
        _hasPending = true;
    }

    public int HeightForLineNumber(int lineNumber)
    {
        Commit();
        var searchIndex = BinarySearchOverOrderedCustomLinesArray(lineNumber);
        if (searchIndex >= 0)
        {
            return _orderedCustomLines[searchIndex].MaximumSpecialHeight;
        }

        return DefaultLineHeight;
    }

    public int GetAccumulatedLineHeightsIncludingLineNumber(int lineNumber)
    {
        Commit();
        var searchIndex = BinarySearchOverOrderedCustomLinesArray(lineNumber);
        if (searchIndex >= 0)
        {
            return _orderedCustomLines[searchIndex].PrefixSum + _orderedCustomLines[searchIndex].MaximumSpecialHeight;
        }

        if (searchIndex == -1)
        {
            return DefaultLineHeight * lineNumber;
        }

        var modifiedIndex = -(searchIndex + 1);
        var previousSpecialLine = _orderedCustomLines[modifiedIndex - 1];
        return previousSpecialLine.PrefixSum + previousSpecialLine.MaximumSpecialHeight
            + (DefaultLineHeight * (lineNumber - previousSpecialLine.LineNumber));
    }

    public void OnLinesDeleted(int fromLineNumber, int toLineNumber)
    {
        _pendingChanges.Add(new LinesDeletedChange(fromLineNumber, toLineNumber));
        _hasPending = true;
    }

    public void OnLinesInserted(int fromLineNumber, int toLineNumber)
    {
        _pendingChanges.Add(new LinesInsertedChange(fromLineNumber, toLineNumber));
        _hasPending = true;
    }

    private void Commit()
    {
        if (!_hasPending)
        {
            return;
        }

        var changes = _pendingChanges;
        _pendingChanges = [];
        _hasPending = false;

        var stagedInserts = new List<CustomLine>();
        var stagedIdMap = new ArrayMap<string, CustomLine>();
        foreach (var change in changes)
        {
            switch (change)
            {
                case RemoveChange remove:
                    DoRemoveCustomLineHeight(remove.DecorationId, stagedIdMap);
                    break;
                case InsertOrChangeChange insert:
                    DoInsertOrChangeCustomLineHeight(insert.DecorationId, insert.StartLineNumber, insert.EndLineNumber, insert.LineHeight, stagedInserts, stagedIdMap);
                    break;
                case LinesDeletedChange deleted:
                    FlushStagedDecorationChanges(stagedInserts, stagedIdMap);
                    DoLinesDeleted(deleted.FromLineNumber, deleted.ToLineNumber);
                    break;
                case LinesInsertedChange inserted:
                    FlushStagedDecorationChanges(stagedInserts, stagedIdMap);
                    DoLinesInserted(inserted.FromLineNumber, inserted.ToLineNumber, stagedInserts, stagedIdMap);
                    break;
            }
        }

        FlushStagedDecorationChanges(stagedInserts, stagedIdMap);
    }

    private void DoRemoveCustomLineHeight(string decorationId, ArrayMap<string, CustomLine> stagedIdMap)
    {
        var customLines = _decorationIdToCustomLine.Get(decorationId);
        if (customLines is not null)
        {
            _decorationIdToCustomLine.Delete(decorationId);
            foreach (var customLine in customLines)
            {
                customLine.Deleted = true;
                _invalidIndex = Math.Min(_invalidIndex, customLine.Index);
            }
        }

        var stagedLines = stagedIdMap.Get(decorationId);
        if (stagedLines is not null)
        {
            stagedIdMap.Delete(decorationId);
            foreach (var line in stagedLines)
            {
                line.Deleted = true;
            }
        }
    }

    private void DoInsertOrChangeCustomLineHeight(string decorationId, int startLineNumber, int endLineNumber, int lineHeight, List<CustomLine> stagedInserts, ArrayMap<string, CustomLine> stagedIdMap)
    {
        DoRemoveCustomLineHeight(decorationId, stagedIdMap);
        for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            var customLine = new CustomLine(decorationId, -1, lineNumber, lineHeight, 0);
            stagedInserts.Add(customLine);
            stagedIdMap.Add(decorationId, customLine);
        }
    }

    private void FlushStagedDecorationChanges(List<CustomLine> stagedInserts, ArrayMap<string, CustomLine> stagedIdMap)
    {
        if (stagedInserts.Count == 0 && _invalidIndex == InvalidIndexNone)
        {
            return;
        }

        foreach (var pendingChange in stagedInserts)
        {
            if (pendingChange.Deleted)
            {
                continue;
            }

            var candidateInsertionIndex = BinarySearchOverOrderedCustomLinesArray(pendingChange.LineNumber);
            var insertionIndex = candidateInsertionIndex >= 0 ? candidateInsertionIndex : -(candidateInsertionIndex + 1);
            _orderedCustomLines.Insert(insertionIndex, pendingChange);
            _invalidIndex = Math.Min(_invalidIndex, insertionIndex);
        }

        stagedInserts.Clear();
        stagedIdMap.Clear();
        if (_invalidIndex == InvalidIndexNone)
        {
            return;
        }

        var newDecorationIdToSpecialLine = new ArrayMap<string, CustomLine>();
        var newOrderedSpecialLines = new List<CustomLine>();

        for (var i = 0; i < _invalidIndex; i++)
        {
            var customLine = _orderedCustomLines[i];
            newOrderedSpecialLines.Add(customLine);
            newDecorationIdToSpecialLine.Add(customLine.DecorationId, customLine);
        }

        var numberOfDeletions = 0;
        CustomLine? previousSpecialLine = _invalidIndex > 0 ? newOrderedSpecialLines[_invalidIndex - 1] : null;
        for (var i = _invalidIndex; i < _orderedCustomLines.Count; i++)
        {
            var customLine = _orderedCustomLines[i];
            if (customLine.Deleted)
            {
                numberOfDeletions++;
                continue;
            }

            customLine.Index = i - numberOfDeletions;
            if (previousSpecialLine is not null && previousSpecialLine.LineNumber == customLine.LineNumber)
            {
                customLine.MaximumSpecialHeight = previousSpecialLine.MaximumSpecialHeight;
                customLine.PrefixSum = previousSpecialLine.PrefixSum;
            }
            else
            {
                var maximumSpecialHeight = customLine.SpecialHeight;
                for (var j = i; j < _orderedCustomLines.Count; j++)
                {
                    var nextSpecialLine = _orderedCustomLines[j];
                    if (nextSpecialLine.Deleted)
                    {
                        continue;
                    }

                    if (nextSpecialLine.LineNumber != customLine.LineNumber)
                    {
                        break;
                    }

                    maximumSpecialHeight = Math.Max(maximumSpecialHeight, nextSpecialLine.SpecialHeight);
                }

                customLine.MaximumSpecialHeight = maximumSpecialHeight;

                int prefixSum;
                if (previousSpecialLine is not null)
                {
                    prefixSum = previousSpecialLine.PrefixSum + previousSpecialLine.MaximumSpecialHeight
                        + (DefaultLineHeight * (customLine.LineNumber - previousSpecialLine.LineNumber - 1));
                }
                else
                {
                    prefixSum = DefaultLineHeight * (customLine.LineNumber - 1);
                }

                customLine.PrefixSum = prefixSum;
            }

            previousSpecialLine = customLine;
            newOrderedSpecialLines.Add(customLine);
            newDecorationIdToSpecialLine.Add(customLine.DecorationId, customLine);
        }

        _orderedCustomLines = newOrderedSpecialLines;
        _decorationIdToCustomLine = newDecorationIdToSpecialLine;
        _invalidIndex = InvalidIndexNone;
    }
}
