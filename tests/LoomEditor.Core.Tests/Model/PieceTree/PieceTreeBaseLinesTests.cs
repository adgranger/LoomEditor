// Not ported from Monaco: Monaco tests these members only indirectly. All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeBaseLinesTests
{
    [Fact]
    public void Create_EmptyChunk_HasOneEmptyLine()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "");

        Assert.Equal(0, tree.GetLength());
        Assert.Equal(1, tree.GetLineCount());
        Assert.Equal("", tree.GetLineContent(1));
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Fact]
    public void Create_NoChunks_HasOneEmptyLine()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n");

        Assert.Equal(0, tree.GetLength());
        Assert.Equal(1, tree.GetLineCount());
        Assert.Equal("", tree.GetLinesRawContent());
    }

    [Fact]
    public void Create_InvalidEol_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PieceTreeBase([], "\r", eolNormalized: true));
    }

    [Fact]
    public void GetLineContent_OneChunk_ReturnsEachLine()
    {
        const string text = "a\nbc\n\ndef";
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", text);

        Assert.Equal(text.Length, tree.GetLength());
        Assert.Equal(4, tree.GetLineCount());
        Assert.Equal("a", tree.GetLineContent(1));
        Assert.Equal("bc", tree.GetLineContent(2));
        Assert.Equal("", tree.GetLineContent(3));
        Assert.Equal("def", tree.GetLineContent(4));
        Assert.Equal(text, tree.GetLinesRawContent());
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Fact]
    public void GetLineContent_LineSpansChunks_JoinsPieces()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab", "c\nd", "ef\n", "g");

        Assert.Equal(3, tree.GetLineCount());
        Assert.Equal("abc", tree.GetLineContent(1));
        Assert.Equal("def", tree.GetLineContent(2));
        Assert.Equal("g", tree.GetLineContent(3));
        Assert.Equal("abc\ndef\ng", tree.GetLinesRawContent());
        Assert.Equal(["abc", "def", "g"], tree.GetLinesContent());
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Fact]
    public void GetLineContent_Crlf_StripsEolButRawKeepsIt()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "a\r\nbc\r\nd");

        Assert.Equal("\r\n", tree.GetEol());
        Assert.Equal("a", tree.GetLineContent(1));
        Assert.Equal("bc", tree.GetLineContent(2));
        Assert.Equal("d", tree.GetLineContent(3));
        Assert.Equal("a\r\n", tree.GetLineRawContent(1));
        Assert.Equal("bc\r\n", tree.GetLineRawContent(2));
        Assert.Equal("d", tree.GetLineRawContent(3));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("a")]
    [InlineData("a\nb\nc")]
    [InlineData("a\nb\nc\n")]
    [InlineData("\n\n\n")]
    [InlineData("one\ntwo\n\nfour")]
    public void GetLinesContent_NormalizedLf_EqualsSplit(string text)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", text);

        Assert.Equal(text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None), tree.GetLinesContent());
    }

    [Theory]
    [InlineData("a\r\nb\r\n\r\nc")]
    [InlineData("a\r\n")]
    [InlineData("\r\n")]
    public void GetLinesContent_NormalizedCrlf_EqualsSplit(string text)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", text);

        Assert.Equal(text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None), tree.GetLinesContent());
    }

    [Theory]
    [InlineData("a\rb\r\nc\nd")]
    [InlineData("a\r", "b")]
    [InlineData("a\r", "\r\nb", "\rc")]
    [InlineData("\r", "\r")]
    [InlineData("x\r", "")]
    public void GetLinesContent_MixedEolNotNormalized_EqualsSplit(params string[] chunks)
    {
        var buffers = chunks.Select(c => new StringBuffer(c, LineStarts.CreateFast(c))).ToList();
        var tree = new PieceTreeBase(buffers, "\n", eolNormalized: false);
        string text = string.Concat(chunks);

        Assert.Equal(text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None), tree.GetLinesContent());
        Assert.Equal(text, tree.GetLinesRawContent());
    }

    [Fact]
    public void GetLineContent_SameLineTwice_ReturnsSameValue()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\ncd", "ef\ngh");

        string first = tree.GetLineContent(2);
        string second = tree.GetLineContent(2);

        Assert.Equal("cdef", first);
        Assert.Same(first, second);
        Assert.Equal("gh", tree.GetLineContent(3));
        Assert.Equal("cdef", tree.GetLineContent(2));
    }

    [Fact]
    public void GetLineContent_ManyChunks_EveryLineMatchesSplit()
    {
        string[] chunks = Enumerable.Range(0, 40).Select(i => $"l{i}\nx{i}").ToArray();
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", chunks);
        string[] expected = string.Concat(chunks).Split('\n');

        Assert.Equal(expected.Length, tree.GetLineCount());
        for (int line = expected.Length; line >= 1; line--)
        {
            Assert.Equal(expected[line - 1], tree.GetLineContent(line));
        }

        TreeInvariants.AssertValid(tree.Tree);
    }
}
