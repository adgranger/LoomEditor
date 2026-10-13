// Not ported from Monaco: Monaco tests EOL changes only through TextModel.setEOL.

using System.Text;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeEolTests
{
    [Fact]
    public void SetEol_LfTextToCrlf_RewritesEveryLineBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "a\nbb\n\nccc");

        tree.SetEol("\r\n");

        Assert.Equal("\r\n", tree.GetEol());
        Assert.Equal(4, tree.GetLineCount());
        Assert.Equal("a\r\nbb\r\n\r\nccc", tree.GetLinesRawContent());
        Assert.Equal(["a", "bb", "", "ccc"], tree.GetLinesContent());
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Fact]
    public void SetEol_MixedLineBreaks_ToLfLeavesOnlyLf()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", false, "a\rb\nc\r\nd", "\r\ne\r");

        tree.SetEol("\n");

        Assert.Equal("a\nb\nc\nd\ne\n", tree.GetLinesRawContent());
        Assert.Equal(6, tree.GetLineCount());
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Fact]
    public void NormalizeEol_InvalidEol_Throws()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "a\nb");

        Assert.Throws<ArgumentException>(() => tree.NormalizeEol("\r"));
        Assert.Throws<ArgumentException>(() => tree.SetEol("\n\n"));
    }

    [Fact]
    public void SetEol_LargeText_ContentIsCorrect()
    {
        var chunks = new string[4];
        var expected = new StringBuilder();
        var expectedCrlf = new StringBuilder();
        var random = new Random(1234);
        for (int c = 0; c < chunks.Length; c++)
        {
            var sb = new StringBuilder();
            while (sb.Length < 70_000)
            {
                sb.Append('x', random.Next(0, 60));
                sb.Append(random.Next(3) switch { 0 => "\n", 1 => "\r\n", _ => "\r" });
            }

            chunks[c] = sb.ToString();
            expected.Append(chunks[c]);
        }

        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", false, chunks);
        string lf = expected.Replace("\r\n", "\n").Replace('\r', '\n').ToString();
        expectedCrlf.Append(lf.Replace("\n", "\r\n"));

        tree.SetEol("\r\n");
        TreeInvariants.AssertValid(tree.Tree);
        Assert.True(tree.GetLength() > 3 * 65_535);
        Assert.Equal(expectedCrlf.ToString(), tree.GetLinesRawContent(), ignoreLineEndingDifferences: false);
        PieceTreeAssert.LinesContent(expectedCrlf.ToString(), tree);

        tree.SetEol("\n");
        TreeInvariants.AssertValid(tree.Tree);
        PieceTreeAssert.LinesContent(lf, tree);
    }

    [Fact]
    public void SetEol_ThenInsertAndDelete_StillWorks()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "one\ntwo\nthree");

        tree.SetEol("\r\n");
        tree.Insert(3, "\r\nXX");
        PieceTreeAssert.LinesContent("one\r\nXX\r\ntwo\r\nthree", tree);

        tree.Delete(0, 5);
        PieceTreeAssert.LinesContent("XX\r\ntwo\r\nthree", tree);

        tree.Insert(tree.GetLength(), "\r\nend");
        PieceTreeAssert.LinesContent("XX\r\ntwo\r\nthree\r\nend", tree);
        TreeInvariants.AssertValid(tree.Tree);
    }
}
