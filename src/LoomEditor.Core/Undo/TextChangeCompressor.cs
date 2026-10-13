// Ported from microsoft/vscode: src/vs/editor/common/core/textChange.ts (compressConsecutiveTextChanges, TextChangeCompressor)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Undo;

/// <summary>
/// Compresses two consecutive batches of <see cref="TextChange"/> into one, so that typing 100 characters
/// stores one change instead of 100.
/// </summary>
/// <remarks>Threading: one instance compresses one pair of batches; not thread-safe, not shared.</remarks>
internal sealed class TextChangeCompressor
{
    private readonly IReadOnlyList<TextChange> _prevEdits;
    private readonly IReadOnlyList<TextChange> _currEdits;

    private readonly List<TextChange> _result = [];

    private readonly int _prevLen;
    private int _prevDeltaOffset;

    private readonly int _currLen;
    private int _currDeltaOffset;

    private TextChangeCompressor(IReadOnlyList<TextChange> prevEdits, IReadOnlyList<TextChange> currEdits)
    {
        _prevEdits = prevEdits;
        _currEdits = currEdits;
        _prevLen = prevEdits.Count;
        _currLen = currEdits.Count;
    }

    /// <summary>
    /// Compresses <paramref name="currEdits"/> (expressed on the text after <paramref name="prevEdits"/>) with
    /// <paramref name="prevEdits"/>. Returns <paramref name="currEdits"/> itself if there are no previous edits.
    /// </summary>
    public static IReadOnlyList<TextChange> CompressConsecutiveTextChanges(
        IReadOnlyList<TextChange>? prevEdits, IReadOnlyList<TextChange> currEdits)
    {
        ArgumentNullException.ThrowIfNull(currEdits);
        if (prevEdits is null || prevEdits.Count == 0)
        {
            return currEdits;
        }

        return new TextChangeCompressor(prevEdits, currEdits).Compress();
    }

    private List<TextChange> Compress()
    {
        var prevIndex = 0;
        var currIndex = 0;

        var prevEdit = GetPrev(prevIndex);
        var currEdit = GetCurr(currIndex);

        while (prevIndex < _prevLen || currIndex < _currLen)
        {
            if (prevEdit is null)
            {
                AcceptCurr(currEdit!);
                currEdit = GetCurr(++currIndex);
                continue;
            }

            if (currEdit is null)
            {
                AcceptPrev(prevEdit);
                prevEdit = GetPrev(++prevIndex);
                continue;
            }

            if (currEdit.OldEnd <= prevEdit.NewPosition)
            {
                AcceptCurr(currEdit);
                currEdit = GetCurr(++currIndex);
                continue;
            }

            if (prevEdit.NewEnd <= currEdit.OldPosition)
            {
                AcceptPrev(prevEdit);
                prevEdit = GetPrev(++prevIndex);
                continue;
            }

            if (currEdit.OldPosition < prevEdit.NewPosition)
            {
                var (e1, e2) = SplitCurr(currEdit, prevEdit.NewPosition - currEdit.OldPosition);
                AcceptCurr(e1);
                currEdit = e2;
                continue;
            }

            if (prevEdit.NewPosition < currEdit.OldPosition)
            {
                var (e1, e2) = SplitPrev(prevEdit, currEdit.OldPosition - prevEdit.NewPosition);
                AcceptPrev(e1);
                prevEdit = e2;
                continue;
            }

            // At this point, currEdit.OldPosition == prevEdit.NewPosition

            TextChange mergePrev;
            TextChange mergeCurr;

            if (currEdit.OldEnd == prevEdit.NewEnd)
            {
                mergePrev = prevEdit;
                mergeCurr = currEdit;
                prevEdit = GetPrev(++prevIndex);
                currEdit = GetCurr(++currIndex);
            }
            else if (currEdit.OldEnd < prevEdit.NewEnd)
            {
                var (e1, e2) = SplitPrev(prevEdit, currEdit.OldLength);
                mergePrev = e1;
                mergeCurr = currEdit;
                prevEdit = e2;
                currEdit = GetCurr(++currIndex);
            }
            else
            {
                var (e1, e2) = SplitCurr(currEdit, prevEdit.NewLength);
                mergePrev = prevEdit;
                mergeCurr = e1;
                prevEdit = GetPrev(++prevIndex);
                currEdit = e2;
            }

            _result.Add(new TextChange(
                mergePrev.OldPosition,
                mergePrev.OldText,
                mergeCurr.NewPosition,
                mergeCurr.NewText));
            _prevDeltaOffset += mergePrev.NewLength - mergePrev.OldLength;
            _currDeltaOffset += mergeCurr.NewLength - mergeCurr.OldLength;
        }

        return RemoveNoOps(Merge(_result));
    }

    private void AcceptCurr(TextChange currEdit)
    {
        _result.Add(RebaseCurr(_prevDeltaOffset, currEdit));
        _currDeltaOffset += currEdit.NewLength - currEdit.OldLength;
    }

    private TextChange? GetCurr(int currIndex)
    {
        return currIndex < _currLen ? _currEdits[currIndex] : null;
    }

    private void AcceptPrev(TextChange prevEdit)
    {
        _result.Add(RebasePrev(_currDeltaOffset, prevEdit));
        _prevDeltaOffset += prevEdit.NewLength - prevEdit.OldLength;
    }

    private TextChange? GetPrev(int prevIndex)
    {
        return prevIndex < _prevLen ? _prevEdits[prevIndex] : null;
    }

    private static TextChange RebaseCurr(int prevDeltaOffset, TextChange currEdit)
    {
        return new TextChange(
            currEdit.OldPosition - prevDeltaOffset,
            currEdit.OldText,
            currEdit.NewPosition,
            currEdit.NewText);
    }

    private static TextChange RebasePrev(int currDeltaOffset, TextChange prevEdit)
    {
        return new TextChange(
            prevEdit.OldPosition,
            prevEdit.OldText,
            prevEdit.NewPosition + currDeltaOffset,
            prevEdit.NewText);
    }

    private static (TextChange, TextChange) SplitPrev(TextChange edit, int offset)
    {
        var preText = edit.NewText[..offset];
        var postText = edit.NewText[offset..];

        return (
            new TextChange(edit.OldPosition, edit.OldText, edit.NewPosition, preText),
            new TextChange(edit.OldEnd, string.Empty, edit.NewPosition + offset, postText));
    }

    private static (TextChange, TextChange) SplitCurr(TextChange edit, int offset)
    {
        var preText = edit.OldText[..offset];
        var postText = edit.OldText[offset..];

        return (
            new TextChange(edit.OldPosition, preText, edit.NewPosition, edit.NewText),
            new TextChange(edit.OldPosition + offset, postText, edit.NewEnd, string.Empty));
    }

    private static List<TextChange> Merge(List<TextChange> edits)
    {
        if (edits.Count == 0)
        {
            return edits;
        }

        var result = new List<TextChange>();

        var prev = edits[0];
        for (var i = 1; i < edits.Count; i++)
        {
            var curr = edits[i];

            if (prev.OldEnd == curr.OldPosition)
            {
                // Merge into `prev`
                prev = new TextChange(
                    prev.OldPosition,
                    prev.OldText + curr.OldText,
                    prev.NewPosition,
                    prev.NewText + curr.NewText);
            }
            else
            {
                result.Add(prev);
                prev = curr;
            }
        }

        result.Add(prev);
        return result;
    }

    private static List<TextChange> RemoveNoOps(List<TextChange> edits)
    {
        if (edits.Count == 0)
        {
            return edits;
        }

        var result = new List<TextChange>();
        foreach (var edit in edits)
        {
            if (string.Equals(edit.OldText, edit.NewText, StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(edit);
        }

        return result;
    }
}
