using LoomEditor.Core.Diff;

namespace LoomEditor.Core.Tests.Diff;

public class DefaultLinesDiffComputerTests
{
    private static readonly LinesDiffComputerOptions Default = new(IgnoreTrimWhitespace: false, MaxComputationTimeMs: 0, ComputeMoves: false);

    private static LinesDiff Diff(string[] a, string[] b, LinesDiffComputerOptions? options = null) =>
        LinesDiffComputers.GetDefault().ComputeDiff(a, b, options ?? Default);

    private static string[] Numbered(int count) => Enumerable.Range(1, count).Select(i => "line number " + i).ToArray();

    [Fact]
    public void ComputeDiff_BothEmptySingleLine_NoChanges()
    {
        LinesDiff result = Diff([""], [""]);
        Assert.Empty(result.Changes);
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void ComputeDiff_IdenticalLines_NoChanges()
    {
        Assert.Empty(Diff(["a", "b", "c"], ["a", "b", "c"]).Changes);
    }

    [Fact]
    public void ComputeDiff_OriginalEmptyFile_WholeFileAdded()
    {
        LinesDiff result = Diff([""], ["ab", "cde"]);
        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        Assert.Equal("[1,2)", change.Original.ToString());
        Assert.Equal("[1,3)", change.Modified.ToString());
        RangeMapping inner = Assert.Single(change.InnerChanges!);
        Assert.Equal(new Range(1, 1, 1, 1), inner.OriginalRange);
        Assert.Equal(new Range(1, 1, 2, 4), inner.ModifiedRange);
    }

    [Fact]
    public void ComputeDiff_InsertedLine_OneLineMapping()
    {
        LinesDiff result = Diff(["a", "c"], ["a", "b", "c"]);
        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        Assert.Equal("{[2,2)->[2,3)}", change.ToString());
    }

    [Fact]
    public void ComputeDiff_DeletedLine()
    {
        LinesDiff result = Diff(["a", "b", "c"], ["a", "c"]);
        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        Assert.Equal("{[2,3)->[2,2)}", change.ToString());
    }

    [Fact]
    public void ComputeDiff_ChangedWord_InnerChangeCoversWord()
    {
        LinesDiff result = Diff(["let hello = 1;"], ["let world = 1;"]);
        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        Assert.Equal("{[1,2)->[1,2)}", change.ToString());
        RangeMapping inner = Assert.Single(change.InnerChanges!);
        Assert.Equal(new Range(1, 5, 1, 10), inner.OriginalRange);
        Assert.Equal(new Range(1, 5, 1, 10), inner.ModifiedRange);
    }

    [Fact]
    public void ComputeDiff_IgnoreTrimWhitespace_TrimmedEqualLinesNotReported()
    {
        var options = Default with { IgnoreTrimWhitespace = true };
        Assert.Empty(Diff(["a", "  b  ", "c"], ["a", "b", "c"], options).Changes);
    }

    [Fact]
    public void ComputeDiff_WhitespaceOnlyChange_Reported_WhenNotIgnoring()
    {
        LinesDiff result = Diff(["a", "b", "c"], ["a", "  b", "c"]);
        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        Assert.Equal("{[2,3)->[2,3)}", change.ToString());
        RangeMapping inner = Assert.Single(change.InnerChanges!);
        Assert.Equal(new Range(2, 1, 2, 1), inner.OriginalRange);
        Assert.Equal(new Range(2, 1, 2, 3), inner.ModifiedRange);
    }

    [Fact]
    public void ComputeDiff_LargeInput_UsesMyersPath()
    {
        string[] a = Numbered(1000);
        string[] b = (string[])a.Clone();
        b[100] = "changed one";
        b[500] = "changed two";
        b[900] = "changed three";
        LinesDiff result = Diff(a, b);
        Assert.Equal(["{[101,102)->[101,102)}", "{[501,502)->[501,502)}", "{[901,902)->[901,902)}"], result.Changes.Select(c => c.ToString()));
    }

    [Fact]
    public void ComputeDiff_TimeoutZero_NoTimeout()
    {
        Assert.False(Diff(Numbered(50), Numbered(50).Reverse().ToArray()).HitTimeout);
    }

    [Fact]
    public void ComputeDiff_ChangesAreSortedAndMappingsConsistent()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var random = new Random(seed);
            string[] a = Enumerable.Range(0, random.Next(2, 40)).Select(_ => "l" + random.Next(0, 8)).ToArray();
            var bList = a.ToList();
            for (int e = random.Next(1, 6); e > 0; e--)
            {
                int idx = random.Next(0, bList.Count);
                switch (random.Next(3))
                {
                    case 0: bList.RemoveAt(idx); break;
                    case 1: bList.Insert(idx, "n" + random.Next(0, 5)); break;
                    default: bList[idx] = "m" + random.Next(0, 5); break;
                }
            }

            string[] b = bList.ToArray();
            LinesDiff result = Diff(a, b);
            int previousOriginalEnd = 1;
            int previousModifiedEnd = 1;
            foreach (DetailedLineRangeMapping c in result.Changes)
            {
                Assert.True(
                    c.Original.StartLineNumber - previousOriginalEnd == c.Modified.StartLineNumber - previousModifiedEnd,
                    $"seed {seed}: unchanged line counts differ before {c}");
                previousOriginalEnd = c.Original.EndLineNumberExclusive;
                previousModifiedEnd = c.Modified.EndLineNumberExclusive;
            }

            Assert.True(
                (a.Length + 1 - previousOriginalEnd) == (b.Length + 1 - previousModifiedEnd),
                $"seed {seed}: unchanged line counts differ after the last change");
        }
    }

    [Fact]
    public void DefaultLinesDiffComputer_ComputeMoves_ReportsMoveWithInnerChanges()
    {
        string[] rest = Numbered(25);
        string[] block = ["first moved line here", "second moved line here", "third moved line here", "fourth moved line here", "fifth moved line here"];
        string[] editedBlock = [.. block];
        editedBlock[2] = "third relocated line here";

        LinesDiff result = Diff([.. block, .. rest], [.. rest, .. editedBlock], Default with { ComputeMoves = true });

        MovedText move = Assert.Single(result.Moves);
        Assert.Equal("{[1,6)->[26,31)}", move.LineRangeMapping.ToString());
        DetailedLineRangeMapping change = Assert.Single(move.Changes);
        Assert.Single(change.InnerChanges!);
    }

    [Fact]
    public void DefaultLinesDiffComputer_ComputeMoves_False_NoMoves()
    {
        string[] rest = Numbered(25);
        string[] block = ["first moved line here", "second moved line here", "third moved line here"];
        LinesDiff result = Diff([.. block, .. rest], [.. rest, .. block]);
        Assert.Empty(result.Moves);
    }
}
