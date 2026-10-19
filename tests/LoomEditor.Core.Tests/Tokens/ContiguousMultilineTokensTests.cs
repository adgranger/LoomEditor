using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class ContiguousMultilineTokensTests
{
    private const uint M1 = 1u << MetadataConsts.ForegroundOffset;
    private const uint M2 = 2u << MetadataConsts.ForegroundOffset;
    private const uint M3 = 3u << MetadataConsts.ForegroundOffset;

    [Fact]
    public void Builder_ConsecutiveLines_GoInOneBlock()
    {
        var builder = new ContiguousMultilineTokensBuilder();
        builder.Add(3, [2, M1]);
        builder.Add(4, [2, M2]);

        List<ContiguousMultilineTokens> blocks = builder.Finalize();

        ContiguousMultilineTokens block = Assert.Single(blocks);
        Assert.Equal(3, block.StartLineNumber);
        Assert.Equal(4, block.EndLineNumber);
        Assert.Equal([2, M2], block.GetLineTokens(4));
    }

    [Fact]
    public void Builder_Gap_StartsANewBlock()
    {
        var builder = new ContiguousMultilineTokensBuilder();
        builder.Add(3, [2, M1]);
        builder.Add(5, [2, M2]);

        List<ContiguousMultilineTokens> blocks = builder.Finalize();

        Assert.Equal(2, blocks.Count);
        Assert.Equal(5, blocks[1].StartLineNumber);
        Assert.Equal(5, blocks[1].EndLineNumber);
    }

    [Fact]
    public void ApplyEdit_InsertLineBeforeBlock_ShiftsBlock()
    {
        var block = new ContiguousMultilineTokens(5, [[2, M1]]);

        block.ApplyEdit(new Range(1, 1, 1, 1), "x\ny");

        Assert.Equal(6, block.StartLineNumber);
        Assert.Equal(6, block.EndLineNumber);
    }

    [Fact]
    public void ApplyEdit_DeleteInsideBlock_RemovesLines()
    {
        var block = new ContiguousMultilineTokens(2, [[2, M1], [2, M2], [2, M3]]);

        block.ApplyEdit(new Range(3, 1, 4, 1), string.Empty);

        Assert.Equal(2, block.StartLineNumber);
        Assert.Equal(3, block.EndLineNumber);
        Assert.Equal([2, M1], block.GetLineTokens(2));
        Assert.Equal([2, M3], block.GetLineTokens(3));
    }

    [Fact]
    public void ApplyEdit_EditInsideALine_EditsItsTokens()
    {
        var block = new ContiguousMultilineTokens(1, [[3, M1, 6, M2]]);

        block.ApplyEdit(new Range(1, 3, 1, 5), string.Empty);
        Assert.Equal([2, M1, 4, M2], block.GetLineTokens(1));

        block.ApplyEdit(new Range(1, 2, 1, 2), "XYZ");
        Assert.Equal([5, M1, 7, M2], block.GetLineTokens(1));
    }
}
