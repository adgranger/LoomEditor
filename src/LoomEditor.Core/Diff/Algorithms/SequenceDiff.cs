// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>A range of the first sequence that is replaced by a range of the second one (Monaco: <c>SequenceDiff</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class SequenceDiff(OffsetRange seq1Range, OffsetRange seq2Range)
{
    /// <summary>The range in the first sequence.</summary>
    public OffsetRange Seq1Range { get; } = seq1Range;

    /// <summary>The range in the second sequence.</summary>
    public OffsetRange Seq2Range { get; } = seq2Range;

    /// <summary><c>invert</c>: the unchanged ranges between the diffs.</summary>
    public static SequenceDiff[] Invert(SequenceDiff[] sequenceDiffs, int doc1Length)
    {
        // Monaco's forEachAdjacent visits (undefined, first), every adjacent pair and (last, undefined).
        SequenceDiff[] result = new SequenceDiff[sequenceDiffs.Length + 1];
        for (int i = 0; i <= sequenceDiffs.Length; i++)
        {
            SequenceDiff? a = i > 0 ? sequenceDiffs[i - 1] : null;
            SequenceDiff? b = i < sequenceDiffs.Length ? sequenceDiffs[i] : null;
            result[i] = FromOffsetPairs(
                a is not null ? a.GetEndExclusives() : OffsetPair.Zero,
                b is not null ? b.GetStarts() : new OffsetPair(doc1Length, (a is not null ? a.Seq2Range.EndExclusive - a.Seq1Range.EndExclusive : 0) + doc1Length));
        }

        return result;
    }

    /// <summary><c>fromOffsetPairs</c>.</summary>
    public static SequenceDiff FromOffsetPairs(OffsetPair start, OffsetPair endExclusive) =>
        new(new OffsetRange(start.Offset1, endExclusive.Offset1), new OffsetRange(start.Offset2, endExclusive.Offset2));

    /// <summary><c>assertSorted</c>: Debug-only check that the diffs are sorted and do not overlap.</summary>
    public static void AssertSorted(SequenceDiff[] sequenceDiffs)
    {
        SequenceDiff? last = null;
        foreach (SequenceDiff cur in sequenceDiffs)
        {
            if (last is not null)
            {
                Guard.Assert(
                    last.Seq1Range.EndExclusive <= cur.Seq1Range.Start && last.Seq2Range.EndExclusive <= cur.Seq2Range.Start,
                    "Sequence diffs must be sorted");
            }

            last = cur;
        }
    }

    /// <summary><c>swap</c>.</summary>
    public SequenceDiff Swap() => new(Seq2Range, Seq1Range);

    /// <summary><c>join</c>: the smallest diff that contains both.</summary>
    public SequenceDiff Join(SequenceDiff other) => new(Seq1Range.Join(other.Seq1Range), Seq2Range.Join(other.Seq2Range));

    /// <summary><c>delta</c>.</summary>
    public SequenceDiff Delta(int offset) => offset == 0 ? this : new(Seq1Range.Delta(offset), Seq2Range.Delta(offset));

    /// <summary><c>deltaStart</c>.</summary>
    public SequenceDiff DeltaStart(int offset) => offset == 0 ? this : new(Seq1Range.DeltaStart(offset), Seq2Range.DeltaStart(offset));

    /// <summary><c>deltaEnd</c>.</summary>
    public SequenceDiff DeltaEnd(int offset) => offset == 0 ? this : new(Seq1Range.DeltaEnd(offset), Seq2Range.DeltaEnd(offset));

    /// <summary><c>intersectsOrTouches</c>.</summary>
    public bool IntersectsOrTouches(SequenceDiff other) =>
        Seq1Range.IntersectsOrTouches(other.Seq1Range) || Seq2Range.IntersectsOrTouches(other.Seq2Range);

    /// <summary><c>intersect</c>: <c>null</c> when either pair of ranges does not even touch.</summary>
    public SequenceDiff? Intersect(SequenceDiff other)
    {
        OffsetRange? i1 = Seq1Range.Intersect(other.Seq1Range);
        OffsetRange? i2 = Seq2Range.Intersect(other.Seq2Range);
        return i1 is null || i2 is null ? null : new SequenceDiff(i1.Value, i2.Value);
    }

    /// <summary><c>getStarts</c>.</summary>
    public OffsetPair GetStarts() => new(Seq1Range.Start, Seq2Range.Start);

    /// <summary><c>getEndExclusives</c>.</summary>
    public OffsetPair GetEndExclusives() => new(Seq1Range.EndExclusive, Seq2Range.EndExclusive);

    /// <inheritdoc />
    public override string ToString() => $"{Seq1Range} <-> {Seq2Range}";
}
