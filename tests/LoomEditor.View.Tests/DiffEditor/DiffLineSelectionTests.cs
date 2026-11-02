using System.Diagnostics;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffLineSelectionTests
{
    private const bool Original = false;
    private const bool Modified = true;

    private static DiffChangeBlock Block(int originalStart, int originalCount, int modifiedStart, int modifiedCount) =>
        new(originalStart, originalCount, modifiedStart, modifiedCount, []);

    // A modified pair, a 2 → 4 block, a pure insertion, a pure deletion.
    private static readonly DiffChangeBlock[] Blocks =
    [
        Block(3, 1, 3, 1),
        Block(6, 2, 6, 4),
        Block(12, 0, 14, 2),
        Block(15, 1, 19, 0),
    ];

    private static DiffLineSelection Create()
    {
        var selection = new DiffLineSelection();
        selection.SetBlocks(Blocks, keepSelection: false);
        return selection;
    }

    private static string Format(DiffLineSelection selection) =>
        string.Join(" ", selection.Lines.Select(l => (l.Modified ? "M" : "O") + l.Line));

    [Fact]
    public void Toggle_AddedLine_SelectsOnlyIt()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.Toggle(Modified, 3));

        Assert.Equal("M3", Format(selection));
    }

    [Fact]
    public void Toggle_DeletedLine_SelectsOnlyIt()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.Toggle(Original, 15));

        Assert.Equal("O15", Format(selection));
    }

    [Fact]
    public void Toggle_Twice_Unselects()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Modified, 14);

        Assert.True(selection.Toggle(Modified, 14));

        Assert.Empty(selection.Lines);
    }

    [Fact]
    public void Toggle_UnchangedLine_ReturnsFalse()
    {
        DiffLineSelection selection = Create();

        Assert.False(selection.Toggle(Original, 4));
        Assert.False(selection.Toggle(Modified, 19));
        Assert.Empty(selection.Lines);
    }

    [Fact]
    public void TogglePair_ModifiedPair_SelectsBoth()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.TogglePair(Modified, 3));

        Assert.Equal("O3 M3", Format(selection));
    }

    [Fact]
    public void TogglePair_OneAlreadySelected_SelectsBoth()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Original, 7);

        Assert.True(selection.TogglePair(Original, 7));

        Assert.Equal("O7 M7", Format(selection));
    }

    [Fact]
    public void TogglePair_BothSelected_UnselectsBoth()
    {
        DiffLineSelection selection = Create();
        selection.TogglePair(Original, 6);

        Assert.True(selection.TogglePair(Modified, 6));

        Assert.Empty(selection.Lines);
    }

    [Fact]
    public void TogglePair_LongerSide_NoPartner_TogglesOne()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.TogglePair(Modified, 8));
        Assert.True(selection.TogglePair(Modified, 9));

        Assert.Equal("M8 M9", Format(selection));
    }

    [Fact]
    public void ExtendTo_SameSide_AddsChangedLinesOnly()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Modified, 3);

        Assert.True(selection.ExtendTo(Modified, 15));

        Assert.Equal("M3 M6 M7 M8 M9 M14 M15", Format(selection));
    }

    [Fact]
    public void ExtendTo_Backwards_AddsChangedLinesOnly()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Original, 15);

        Assert.True(selection.ExtendTo(Original, 7));

        Assert.Equal("O7 O15", Format(selection));
    }

    [Fact]
    public void ExtendTo_OtherSide_ActsAsToggle()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Original, 3);

        Assert.True(selection.ExtendTo(Modified, 9));

        Assert.Equal("O3 M9", Format(selection));
    }

    [Fact]
    public void ExtendTo_NoAnchor_ActsAsToggle()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.ExtendTo(Modified, 9));

        Assert.Equal("M9", Format(selection));
    }

    [Fact]
    public void Lines_OriginalFirstThenModified_Ascending()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Modified, 15);
        selection.Toggle(Original, 15);
        selection.Toggle(Modified, 3);
        selection.Toggle(Original, 6);

        Assert.Equal("O6 O15 M3 M15", Format(selection));
    }

    [Fact]
    public void SetBlocks_Keep_DropsLinesNoLongerChanged()
    {
        DiffLineSelection selection = Create();
        selection.Set([(Original, 3), (Modified, 3), (Modified, 14)]);

        Assert.True(selection.SetBlocks([Block(3, 1, 3, 1)], keepSelection: true));

        Assert.Equal("O3 M3", Format(selection));
    }

    [Fact]
    public void SetBlocks_Keep_SameLines_ReturnsFalse()
    {
        DiffLineSelection selection = Create();
        selection.Set([(Original, 3)]);

        Assert.False(selection.SetBlocks(Blocks, keepSelection: true));

        Assert.Equal("O3", Format(selection));
    }

    [Fact]
    public void SetBlocks_NotKeep_Clears()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Modified, 3);

        Assert.True(selection.SetBlocks(Blocks, keepSelection: false));

        Assert.Empty(selection.Lines);
    }

    [Fact]
    public void SetBlocks_NotKeep_ClearsTheAnchor()
    {
        DiffLineSelection selection = Create();
        selection.Toggle(Modified, 3);
        selection.SetBlocks(Blocks, keepSelection: false);

        selection.ExtendTo(Modified, 7);

        Assert.Equal("M7", Format(selection));
    }

    [Fact]
    public void Set_IgnoresUnchangedLines()
    {
        DiffLineSelection selection = Create();

        Assert.True(selection.Set([(Original, 1), (Original, 6), (Modified, 19)]));

        Assert.Equal("O6", Format(selection));
    }

    [Fact]
    public void Mutators_NoChange_ReturnFalse()
    {
        DiffLineSelection selection = Create();

        Assert.False(selection.Clear());
        Assert.False(selection.Set([(Original, 1)]));
        selection.Set([(Modified, 6), (Modified, 7)]);
        Assert.False(selection.Set([(Modified, 7), (Modified, 6)]));
        selection.Toggle(Modified, 8);
        Assert.False(selection.ExtendTo(Modified, 6));
    }

    [Fact]
    public void IsChangedLine_EmptySidesAndBounds()
    {
        DiffLineSelection selection = Create();

        Assert.False(selection.IsChangedLine(Original, 12));
        Assert.True(selection.IsChangedLine(Original, 7));
        Assert.False(selection.IsChangedLine(Original, 8));
        Assert.True(selection.IsChangedLine(Modified, 9));
        Assert.False(selection.IsChangedLine(Modified, 10));
        Assert.False(selection.IsChangedLine(Modified, 1));
    }

    [Fact]
    public void IsChangedLine_FiftyThousandLines_IsFast()
    {
        // 10 000 two-line blocks every 5 lines: 50 000 lines.
        var blocks = new DiffChangeBlock[10_000];
        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i] = Block((i * 5) + 1, 2, (i * 5) + 1, 2);
        }

        var selection = new DiffLineSelection();
        selection.SetBlocks(blocks, keepSelection: false);
        const int Seed = 14;
        var random = new Random(Seed);
        int hits = 0;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 100_000; i++)
        {
            int line = random.Next(1, 50_001);
            if (selection.IsChangedLine(i % 2 == 0, line))
            {
                hits++;
            }
        }

        watch.Stop();
        Assert.True(hits > 0, $"seed {Seed}");
        Assert.True(watch.ElapsedMilliseconds < 200, $"seed {Seed}: {watch.ElapsedMilliseconds} ms");
    }
}
