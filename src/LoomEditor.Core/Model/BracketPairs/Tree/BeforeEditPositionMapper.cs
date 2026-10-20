// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/beforeEditPositionMapper.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// Maps offsets of the text after a list of edits to offsets of the text before them, and tells how far the next
/// edit is. The offsets passed to its methods must not decrease (Monaco: <c>BeforeEditPositionMapper</c>).
/// </summary>
/// <remarks>Threading: not thread-safe; used by one parser on the UI thread.</remarks>
internal sealed class BeforeEditPositionMapper
{
    private int _nextEditIdx;
    private int _deltaOldToNewLineCount;
    private int _deltaOldToNewColumnCount;
    private int _deltaLineIdxInOld = -1;
    private readonly TextEditInfoCache[] _edits;

    /// <param name="edits">Must be sorted by offset in ascending order.</param>
    public BeforeEditPositionMapper(IReadOnlyList<TextEditInfo> edits)
    {
        ArgumentNullException.ThrowIfNull(edits);

        _edits = new TextEditInfoCache[edits.Count];
        for (var i = 0; i < edits.Count; i++)
        {
            _edits[i] = TextEditInfoCache.From(edits[i]);
        }
    }

    /// <param name="offset">Must be equal to or greater than the last offset this method has been called with.</param>
    public Length GetOffsetBeforeChange(Length offset)
    {
        AdjustNextEdit(offset);
        return TranslateCurToOld(offset);
    }

    /// <param name="offset">Must be equal to or greater than the last offset this method has been called with.</param>
    /// <returns>The distance to the next edit, or <see langword="null"/> if there is no edit any more.</returns>
    public Length? GetDistanceToNextChange(Length offset)
    {
        AdjustNextEdit(offset);

        if (_nextEditIdx >= _edits.Length)
        {
            return null;
        }

        var nextChangeOffset = TranslateOldToCur(_edits[_nextEditIdx].OffsetObj);
        return Length.DiffNonNegative(offset, nextChangeOffset);
    }

    private Length TranslateOldToCur(TextLength oldOffsetObj)
    {
        if (oldOffsetObj.LineCount == _deltaLineIdxInOld)
        {
            return Length.Of(oldOffsetObj.LineCount + _deltaOldToNewLineCount, oldOffsetObj.ColumnCount + _deltaOldToNewColumnCount);
        }

        return Length.Of(oldOffsetObj.LineCount + _deltaOldToNewLineCount, oldOffsetObj.ColumnCount);
    }

    private Length TranslateCurToOld(Length newOffset)
    {
        var offsetObj = newOffset.ToObj();
        if (offsetObj.LineCount - _deltaOldToNewLineCount == _deltaLineIdxInOld)
        {
            return Length.Of(offsetObj.LineCount - _deltaOldToNewLineCount, offsetObj.ColumnCount - _deltaOldToNewColumnCount);
        }

        return Length.Of(offsetObj.LineCount - _deltaOldToNewLineCount, offsetObj.ColumnCount);
    }

    private void AdjustNextEdit(Length offset)
    {
        while (_nextEditIdx < _edits.Length)
        {
            var nextEdit = _edits[_nextEditIdx];

            // After applying the edit, what is its end offset (considering all previous edits)?
            var nextEditEndOffsetInCur = TranslateOldToCur(nextEdit.EndOffsetAfterObj);

            if (Length.LessThanEqual(nextEditEndOffsetInCur, offset))
            {
                // We are after the edit, skip it
                _nextEditIdx++;

                var nextEditEndOffsetInCurObj = nextEditEndOffsetInCur.ToObj();

                // Before applying the edit, what is its end offset (considering all previous edits)?
                var nextEditEndOffsetBeforeInCurObj = TranslateOldToCur(nextEdit.EndOffsetBeforeObj).ToObj();

                var lineDelta = nextEditEndOffsetInCurObj.LineCount - nextEditEndOffsetBeforeInCurObj.LineCount;
                _deltaOldToNewLineCount += lineDelta;

                var previousColumnDelta = _deltaLineIdxInOld == nextEdit.EndOffsetBeforeObj.LineCount ? _deltaOldToNewColumnCount : 0;
                var columnDelta = nextEditEndOffsetInCurObj.ColumnCount - nextEditEndOffsetBeforeInCurObj.ColumnCount;
                _deltaOldToNewColumnCount = previousColumnDelta + columnDelta;
                _deltaLineIdxInOld = nextEdit.EndOffsetBeforeObj.LineCount;
            }
            else
            {
                // We are in or before the edit.
                break;
            }
        }
    }

    // Monaco: TextEditInfoCache
    private sealed class TextEditInfoCache
    {
        private TextEditInfoCache(Length startOffset, Length endOffset, Length textLength)
        {
            EndOffsetBeforeObj = endOffset.ToObj();
            EndOffsetAfterObj = Length.Add(startOffset, textLength).ToObj();
            OffsetObj = startOffset.ToObj();
        }

        public TextLength EndOffsetBeforeObj { get; }

        public TextLength EndOffsetAfterObj { get; }

        public TextLength OffsetObj { get; }

        public static TextEditInfoCache From(TextEditInfo edit)
        {
            return new TextEditInfoCache(edit.StartOffset, edit.EndOffset, edit.NewLength);
        }
    }
}
