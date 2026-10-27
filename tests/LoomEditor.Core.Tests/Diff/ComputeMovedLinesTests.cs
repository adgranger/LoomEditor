using LoomEditor.Core.Diff;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class ComputeMovedLinesTests
{
    private static string[] Filler(int count, int from = 1) =>
        Enumerable.Range(from, count).Select(i => "this is distinct filler line number " + i).ToArray();

    private static string[] Block(string name, int count) =>
        Enumerable.Range(1, count).Select(i => $"block {name} contains text {i}").ToArray();

    private static int[] Hashes(string[] lines, Dictionary<string, int> map) =>
        lines.Select(l =>
        {
            string t = l.Trim();
            if (!map.TryGetValue(t, out int h))
            {
                h = map.Count;
                map[t] = h;
            }

            return h;
        }).ToArray();

    private static IReadOnlyList<LineRangeMapping> Compute(string[] original, string[] modified, IReadOnlyList<DetailedLineRangeMapping>? changes = null)
    {
        changes ??= LinesDiffComputers.GetDefault()
            .ComputeDiff(original, modified, new LinesDiffComputerOptions(false, 0, false)).Changes;
        var map = new Dictionary<string, int>();
        return ComputeMovedLines.Compute(changes, original, modified, Hashes(original, map), Hashes(modified, map), InfiniteTimeout.Instance);
    }

    private static string[] Moved(string[] block, string[] rest) => [.. rest, .. block];

    private static DetailedLineRangeMapping Change(int os, int oe, int ms, int me) => new(new LineRange(os, oe), new LineRange(ms, me), []);

    [Fact]
    public void ComputeMovedLines_NoChanges_NoMoves()
    {
        string[] lines = Filler(10);
        Assert.Empty(Compute(lines, lines));
    }

    [Fact]
    public void ComputeMovedLines_BlockMovedDown_OneMove()
    {
        string[] rest = Filler(25);
        string[] block = Block("a", 5);
        IReadOnlyList<LineRangeMapping> moves = Compute([.. block, .. rest], Moved(block, rest));
        Assert.Equal(["{[1,6)->[26,31)}"], moves.Select(m => m.ToString()));
    }

    [Fact]
    public void ComputeMovedLines_ShortBlock_Ignored()
    {
        string[] rest = Filler(25);
        string[] block = ["ab", "cd", "ef"];
        Assert.Empty(Compute([.. block, .. rest], Moved(block, rest)));
    }

    [Fact]
    public void ComputeMovedLines_TwoLinesOnlyOneLong_Ignored()
    {
        string[] rest = Filler(25);
        string[] block = ["x", "a rather long line of text", "y"];
        Assert.Empty(Compute([.. block, .. rest], Moved(block, rest)));
    }

    [Fact]
    public void ComputeMovedLines_TwoMovesCloseTogether_Joined()
    {
        string[] original = ["p1 text of line", "p2 text of line", "p3 text of line", "separator line one", "q1 text of line", "q2 text of line", "q3 text of line", .. Filler(5)];
        string[] modified = [.. Filler(5), "separator line zzz", "p1 text of line", "p2 text of line", "p3 text of line", "separator line two", "q1 text of line", "q2 text of line", "q3 text of line"];
        IReadOnlyList<DetailedLineRangeMapping> changes = [Change(1, 8, 1, 7), Change(13, 13, 7, 14)];
        Assert.Equal(["{[1,8)->[7,14)}"], Compute(original, modified, changes).Select(m => m.ToString()));
    }

    [Fact]
    public void ComputeMovedLines_MoveInsideSameDiff_Removed()
    {
        string[] original = ["common first line", "p1 text of line", "p2 text of line", "p3 text of line", "separator line one", "q1 text of line", "q2 text of line", "q3 text of line"];
        string[] modified = ["common first line", "q1 text of line", "q2 text of line", "q3 text of line", "separator line two", "p1 text of line", "p2 text of line", "p3 text of line"];
        Assert.Empty(Compute(original, modified, [Change(2, 9, 2, 9)]));
    }

    [Fact]
    public void ComputeMovedLines_DoesNotModifyTheChangesList()
    {
        string[] rest = Filler(25);
        string[] block = Block("a", 5);
        string[] original = [.. block, .. rest];
        string[] modified = Moved(block, rest);
        List<DetailedLineRangeMapping> changes = [.. LinesDiffComputers.GetDefault().ComputeDiff(original, modified, new LinesDiffComputerOptions(false, 0, false)).Changes];
        string[] before = changes.Select(c => c.ToString()).ToArray();
        Compute(original, modified, changes);
        Assert.Equal(before, changes.Select(c => c.ToString()));
    }
}
