// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/combineTextEditInfos.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>Combines two consecutive lists of edits into one (Monaco: <c>combineTextEditInfos</c>).</summary>
/// <remarks>
/// Difference from Monaco: Monaco's <c>ArrayQueue</c> is a <see cref="Queue{T}"/>.
/// Threading: pure and thread-safe.
/// </remarks>
internal static class TextEditInfoCombiner
{
    /// <summary>
    /// Returns the edits that do the same as <paramref name="first"/> followed by <paramref name="second"/>.
    /// Returns one of its arguments unchanged when the other one is empty: callers must not mutate the result.
    /// </summary>
    public static List<TextEditInfo> Combine(List<TextEditInfo> first, List<TextEditInfo> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Count == 0)
        {
            return second;
        }

        if (second.Count == 0)
        {
            return first;
        }

        // s0: State before any edits
        var s0ToS1Map = new Queue<LengthMapping>(ToLengthMapping(first));
        // s1: State after first edit, but before second edit
        var s1ToS2Map = ToLengthMapping(second);
        // s2: State after both edits

        LengthMapping? curItem = s0ToS1Map.Count > 0 ? s0ToS1Map.Dequeue() : null;

        // Use null for length "infinity".
        List<LengthMapping> NextS0ToS1MapWithS1LengthOf(Length? s1LengthOrNull)
        {
            if (s1LengthOrNull is null)
            {
                var arr = new List<LengthMapping>();
                if (curItem is not null)
                {
                    arr.Add(curItem);
                }

                while (s0ToS1Map.Count > 0)
                {
                    arr.Add(s0ToS1Map.Dequeue());
                }

                return arr;
            }

            var s1Length = s1LengthOrNull.Value;
            var result = new List<LengthMapping>();
            while (curItem is not null && !s1Length.IsZero)
            {
                var (item, remainingItem) = curItem.SplitAt(s1Length);
                result.Add(item);
                s1Length = Length.DiffNonNegative(item.LengthAfter, s1Length);
                curItem = remainingItem ?? (s0ToS1Map.Count > 0 ? s0ToS1Map.Dequeue() : null);
            }

            if (!s1Length.IsZero)
            {
                result.Add(new LengthMapping(false, s1Length, s1Length));
            }

            return result;
        }

        var combined = new List<TextEditInfo>();

        void PushEdit(Length startOffset, Length endOffset, Length newLength)
        {
            if (combined.Count > 0 && combined[^1].EndOffset == startOffset)
            {
                var lastResult = combined[^1];
                combined[^1] = new TextEditInfo(lastResult.StartOffset, endOffset, Length.Add(lastResult.NewLength, newLength));
            }
            else
            {
                combined.Add(new TextEditInfo(startOffset, endOffset, newLength));
            }
        }

        var s0Offset = Length.Zero;

        // The last iteration is the sentinel (s1Length == null): copy everything from old to new.
        for (var i = 0; i <= s1ToS2Map.Count; i++)
        {
            var s1ToS2 = i < s1ToS2Map.Count ? s1ToS2Map[i] : null;
            var s0ToS1 = NextS0ToS1MapWithS1LengthOf(s1ToS2?.LengthBefore);

            if (s1ToS2 is not null && s1ToS2.Modified)
            {
                var s0Length = Length.Sum(s0ToS1, s => s.LengthBefore);
                var s0EndOffset = Length.Add(s0Offset, s0Length);
                PushEdit(s0Offset, s0EndOffset, s1ToS2.LengthAfter);
                s0Offset = s0EndOffset;
            }
            else
            {
                foreach (var s1 in s0ToS1)
                {
                    var s0StartOffset = s0Offset;
                    s0Offset = Length.Add(s0Offset, s1.LengthBefore);

                    if (s1.Modified)
                    {
                        PushEdit(s0StartOffset, s0Offset, s1.LengthAfter);
                    }
                }
            }
        }

        return combined;
    }

    private static List<LengthMapping> ToLengthMapping(List<TextEditInfo> textEditInfos)
    {
        var result = new List<LengthMapping>();
        var lastOffset = Length.Zero;
        foreach (var textEditInfo in textEditInfos)
        {
            var spaceLength = Length.DiffNonNegative(lastOffset, textEditInfo.StartOffset);
            if (!spaceLength.IsZero)
            {
                result.Add(new LengthMapping(false, spaceLength, spaceLength));
            }

            var lengthBefore = Length.DiffNonNegative(textEditInfo.StartOffset, textEditInfo.EndOffset);
            result.Add(new LengthMapping(true, lengthBefore, textEditInfo.NewLength));

            lastOffset = textEditInfo.EndOffset;
        }

        return result;
    }

    /// <param name="Modified">If false, length before and length after are equal.</param>
    /// <param name="LengthBefore">The length in the old text.</param>
    /// <param name="LengthAfter">The length in the new text.</param>
    private sealed record LengthMapping(bool Modified, Length LengthBefore, Length LengthAfter)
    {
        public (LengthMapping Item, LengthMapping? Remaining) SplitAt(Length lengthAfter)
        {
            var remainingLengthAfter = Length.DiffNonNegative(lengthAfter, LengthAfter);
            if (remainingLengthAfter == Length.Zero)
            {
                return (this, null);
            }

            if (Modified)
            {
                return (
                    new LengthMapping(Modified, LengthBefore, lengthAfter),
                    new LengthMapping(Modified, Length.Zero, remainingLengthAfter));
            }

            return (
                new LengthMapping(Modified, lengthAfter, lengthAfter),
                new LengthMapping(Modified, remainingLengthAfter, remainingLengthAfter));
        }

        public override string ToString()
        {
            return $"{(Modified ? 'M' : 'U')}:{LengthBefore.ToObj()} -> {LengthAfter.ToObj()}";
        }
    }
}
