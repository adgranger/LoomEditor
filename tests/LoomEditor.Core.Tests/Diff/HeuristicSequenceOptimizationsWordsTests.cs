using LoomEditor.Core.Diff;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class HeuristicSequenceOptimizationsWordsTests
{
    private static SequenceDiff Diff(int s1, int e1, int s2, int e2) => new(new OffsetRange(s1, e1), new OffsetRange(s2, e2));

    private static LinesSliceCharSequence Seq(string text) =>
        new([text], new Range(1, 1, 1, text.Length + 1), true);

    private static LineSequence Lines(params string[] lines) => new([.. lines.Select((_, i) => i)], lines);

    private static string[] Show(IReadOnlyList<SequenceDiff> diffs) => [.. diffs.Select(d => d.ToString())];

    private static IReadOnlyList<SequenceDiff> Extend(string a, string b, SequenceDiff[] diffs, bool subWords = false) =>
        HeuristicSequenceOptimizations.ExtendDiffsToEntireWordIfAppropriate(
            Seq(a), Seq(b), diffs, subWords ? (s, i) => s.FindSubWordContaining(i) : (s, i) => s.FindWordContaining(i), subWords);

    [Fact]
    public void ExtendToWord_PartialWordChange_ExtendsToWholeWord()
    {
        // Half of the word changed; for "foobar" -> "foobaz" 5/6 equal chars would be kept as is (2/3 rule).
        Assert.Equal(["[0, 4) <-> [0, 4)"], Show(Extend("abcd", "abxy", [Diff(2, 4, 2, 4)])));
    }

    [Fact]
    public void ExtendToWord_SubWords_CamelCase()
    {
        SequenceDiff[] diffs = [Diff(5, 6, 5, 6)];
        Assert.Equal(["[5, 6) <-> [5, 6)"], Show(Extend("fooBar", "fooBaz", diffs, subWords: false)));
        Assert.Equal(["[3, 6) <-> [3, 6)"], Show(Extend("fooBar", "fooBaz", diffs, subWords: true)));
    }

    [Fact]
    public void ExtendToWord_NothingToExtend_Unchanged()
    {
        Assert.Equal(["[2, 3) <-> [2, 3)"], Show(Extend("a b", "a c", [Diff(2, 3, 2, 3)])));
    }

    [Fact]
    public void Merge_InterleavesAndJoinsOverlaps()
    {
        // The extended word [0,4) comes first and swallows the original diff [2,4); the far diff stays apart.
        IReadOnlyList<SequenceDiff> result = Extend("abcd e q", "abxy e r", [Diff(2, 4, 2, 4), Diff(7, 8, 7, 8)]);
        Assert.Equal(["[0, 4) <-> [0, 4)", "[7, 8) <-> [7, 8)"], Show(result));
    }

    [Fact]
    public void RemoveVeryShortMatchingLines_MergesAcrossOneBlankLine()
    {
        LineSequence seq = Lines("a", "b", "c", "", "d");
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.RemoveVeryShortMatchingLinesBetweenDiffs(
            seq, seq, [Diff(0, 3, 0, 3), Diff(4, 5, 4, 5)]);
        Assert.Equal(["[0, 5) <-> [0, 5)"], Show(result));
    }

    [Fact]
    public void RemoveVeryShortMatchingLines_KeepsLongMatchingBlock()
    {
        LineSequence seq = Lines("a", "b", "c", "a long matching line", "d");
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.RemoveVeryShortMatchingLinesBetweenDiffs(
            seq, seq, [Diff(0, 3, 0, 3), Diff(4, 5, 4, 5)]);
        Assert.Equal(["[0, 3) <-> [0, 3)", "[4, 5) <-> [4, 5)"], Show(result));
    }

    [Fact]
    public void RemoveVeryShortMatchingText_MergesAcrossShortEqualText()
    {
        LinesSliceCharSequence s1 = Seq(new string('x', 80) + "ab" + new string('x', 80));
        LinesSliceCharSequence s2 = Seq(new string('y', 80) + "ab" + new string('y', 80));
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.RemoveVeryShortMatchingTextBetweenLongDiffs(
            s1, s2, [Diff(0, 80, 0, 80), Diff(82, 162, 82, 162)]);
        Assert.Equal(["[0, 162) <-> [0, 162)"], Show(result));
    }

    [Fact]
    public void RemoveVeryShortMatchingText_KeepsEqualTextBetweenSmallDiffs()
    {
        LinesSliceCharSequence s1 = Seq("xx ab xx");
        LinesSliceCharSequence s2 = Seq("yy ab yy");
        IReadOnlyList<SequenceDiff> result = HeuristicSequenceOptimizations.RemoveVeryShortMatchingTextBetweenLongDiffs(
            s1, s2, [Diff(0, 2, 0, 2), Diff(6, 8, 6, 8)]);
        Assert.Equal(["[0, 2) <-> [0, 2)", "[6, 8) <-> [6, 8)"], Show(result));
    }
}
