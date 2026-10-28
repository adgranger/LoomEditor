using LoomEditor.Core.Diff;
using LoomEditor.Gallery;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// New tests: the Gallery samples (compiled into this project) must show every feature of the diff editor.
public class DiffSamplesSanityTests
{
    private static LinesDiff Compute(bool ignoreTrimWhitespace) =>
        new DefaultLinesDiffComputer().ComputeDiff(
            DiffSamples.Original.Split('\n'),
            DiffSamples.Modified.Split('\n'),
            new LinesDiffComputerOptions(ignoreTrimWhitespace, 5000, ComputeMoves: false));

    [Fact]
    public void Samples_ProduceChanges_WithInnerChanges()
    {
        IReadOnlyList<DetailedLineRangeMapping> changes = Compute(ignoreTrimWhitespace: false).Changes;

        Assert.True(changes.Count >= 4, $"{changes.Count} change(s)");
        Assert.Contains(changes, c => c.InnerChanges is { Count: > 0 } && !c.Original.IsEmpty && !c.Modified.IsEmpty);
        Assert.Contains(changes, c => c.Original.IsEmpty && !c.Modified.IsEmpty);
        Assert.Contains(changes, c => !c.Original.IsEmpty && c.Modified.IsEmpty);
    }

    [Fact]
    public void Samples_HideUnchangedRegions_ProducesAtLeastTwoRegions()
    {
        IReadOnlyList<DetailedLineRangeMapping> changes = Compute(ignoreTrimWhitespace: true).Changes;
        int originalLines = DiffSamples.Original.Split('\n').Length;
        int modifiedLines = DiffSamples.Modified.Split('\n').Length;

        var regions = UnchangedRegion.FromDiffs(changes, originalLines, modifiedLines, 3, 3);

        Assert.True(regions.Count >= 2, $"{regions.Count} region(s)");
    }

    [Fact]
    public void Samples_WhitespaceOnlyLine_OnlyWithoutIgnoreTrim() =>
        Assert.Equal(Compute(ignoreTrimWhitespace: false).Changes.Count - 1, Compute(ignoreTrimWhitespace: true).Changes.Count);

    [Fact]
    public void Samples_UseLineFeedsAndHaveTheExpectedSize()
    {
        Assert.DoesNotContain('\r', DiffSamples.Original);
        Assert.DoesNotContain('\r', DiffSamples.Modified);
        Assert.InRange(DiffSamples.Original.Split('\n').Length, 55, 100);
        Assert.InRange(DiffSamples.Modified.Split('\n').Length, 60, 110);
    }

    [Fact]
    public void CreateLargePair_FiveThousandLines_AboutOnePercentChanged()
    {
        var (original, modified) = DiffSamples.CreateLargePair();
        string[] a = original.Split('\n');
        string[] b = modified.Split('\n');

        Assert.Equal(5000, a.Length);
        Assert.Equal(5000, b.Length);
        int changed = a.Where((line, i) => line != b[i]).Count();
        Assert.InRange(changed, 20, 90);
        Assert.Equal((original, modified), DiffSamples.CreateLargePair());
    }
}
