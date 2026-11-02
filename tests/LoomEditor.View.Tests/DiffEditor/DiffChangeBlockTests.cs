using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffChangeBlockTests
{
    // The options the view model uses by default (IgnoreTrimWhitespace on).
    private static async Task<IReadOnlyList<DiffChangeBlock>> Blocks(TextModel original, TextModel modified)
    {
        var options = new DiffEditorOptions();
        DocumentDiff diff = await new DocumentDiffProvider().ComputeDiffAsync(
            original, modified, new LinesDiffComputerOptions(options.IgnoreTrimWhitespace, options.MaxComputationTimeMs, false), TestContext.Current.CancellationToken);
        return DiffChangeBlock.FromMappings(DiffState.FromDiffResult(diff).Mappings);
    }

    private static Task<IReadOnlyList<DiffChangeBlock>> Blocks(string original, string modified) => Blocks(new TextModel(original), new TextModel(modified));

    private static string Lines(int from, int to) => string.Join("\n", Enumerable.Range(from, to - from + 1).Select(i => $"line {i}"));

    [Fact]
    public async Task FromMappings_SeveralBlocks_InOrderWithExactRanges()
    {
        string original = Lines(1, 20);
        string modified = string.Join("\n", Lines(1, 2), "line 3 changed", Lines(4, 8), Lines(10, 15), "inserted", Lines(16, 20));

        IReadOnlyList<DiffChangeBlock> blocks = await Blocks(original, modified);

        Assert.Equal(["3,1 -> 3,1", "9,1 -> 9,0", "16,0 -> 15,1"], blocks.Select(b => b.ToString()));
    }

    [Fact]
    public async Task FromMappings_PureInsertion_OriginalCountIsZero()
    {
        DiffChangeBlock block = Assert.Single(await Blocks(Lines(1, 8), string.Join("\n", Lines(1, 4), "new a", "new b", Lines(5, 8))));

        Assert.Equal(5, block.OriginalStartLineNumber);
        Assert.Equal(0, block.OriginalLineCount);
        Assert.Equal(5, block.ModifiedStartLineNumber);
        Assert.Equal(2, block.ModifiedLineCount);
    }

    [Fact]
    public async Task FromMappings_PureDeletion_ModifiedCountIsZero()
    {
        DiffChangeBlock block = Assert.Single(await Blocks(Lines(1, 8), string.Join("\n", Lines(1, 2), Lines(6, 8))));

        Assert.Equal("3,3 -> 3,0", block.ToString());
    }

    [Fact]
    public async Task FromMappings_ModifiedLine_PairWithInnerChanges()
    {
        var original = new TextModel("first\nhello cat\nlast");
        var modified = new TextModel("first\nhello dog\nlast");

        DiffChangeBlock block = Assert.Single(await Blocks(original, modified));

        Assert.Equal("2,1 -> 2,1", block.ToString());
        DiffInnerChange inner = Assert.Single(block.InnerChanges);
        Assert.Equal("cat", original.GetValueInRange(inner.OriginalRange));
        Assert.Equal("dog", modified.GetValueInRange(inner.ModifiedRange));
    }

    [Fact]
    public async Task FromMappings_CrlfAndLf_SameBlocks()
    {
        string original = Lines(1, 10);
        string modified = string.Join("\n", Lines(1, 3), "changed", Lines(5, 10));

        IReadOnlyList<DiffChangeBlock> lf = await Blocks(original, modified);
        IReadOnlyList<DiffChangeBlock> crlf = await Blocks(original.Replace("\n", "\r\n"), modified.Replace("\n", "\r\n"));
        IReadOnlyList<DiffChangeBlock> mixed = await Blocks(original.Replace("\n", "\r\n"), modified);

        Assert.Equal(["4,1 -> 4,1"], lf.Select(b => b.ToString()));
        Assert.Equal(lf.Select(b => b.ToString()), crlf.Select(b => b.ToString()));
        Assert.Equal(lf.Select(b => b.ToString()), mixed.Select(b => b.ToString()));
    }

    [Fact]
    public async Task FromMappings_EmptyOriginal_OneBlock()
    {
        // The diff provider's "original file is empty" shortcut: the single empty line of the original is replaced by the whole modified text.
        DiffChangeBlock block = Assert.Single(await Blocks("", "a\nb"));

        Assert.Equal("1,1 -> 1,2", block.ToString());
    }

    [Fact]
    public async Task FromMappings_IdenticalTexts_NoBlock()
    {
        Assert.Empty(await Blocks(Lines(1, 5), Lines(1, 5)));
        Assert.Empty(await Blocks("", ""));
    }

    [Fact]
    public void FromMappings_NoInnerChanges_EmptyList()
    {
        var mapping = new DiffMapping(new DetailedLineRangeMapping(new LineRange(2, 3), new LineRange(2, 4), null));

        DiffChangeBlock block = Assert.Single(DiffChangeBlock.FromMappings([mapping]));

        Assert.Empty(block.InnerChanges);
        Assert.Equal("2,1 -> 2,2", block.ToString());
    }
}
