// Not ported from Monaco: Monaco tests these members only indirectly (through its helpers). All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeBasePositionsTests
{
    public static TheoryData<string, string[]> Texts => new()
    {
        { "\n", [""] },
        { "\n", ["a\nbc\n\ndef"] },
        { "\r\n", ["a\r\nbc\r\n\r\ndef"] },
        { "\n", ["abc\nde", "f\n\nghi", "\n", "jk\nl"] },
        { "\r\n", ["abc\r\nde", "f\r\n", "g", "\r\nh\r\n"] },
        { "\n", ["a\nb\n"] },
        { "\r\n", ["a\r\nb\r\n"] },
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void Helpers_TextShapes_Pass(string eol, string[] chunks)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks(eol, chunks);
        string text = string.Concat(chunks);

        PieceTreeAssert.LinesContent(text, tree);
        PieceTreeAssert.LineStarts(text, tree);
        TreeInvariants.AssertValid(tree.Tree);
    }

    [Theory]
    [MemberData(nameof(Texts))]
    public void PositionAndOffset_EveryOffset_RoundTrip(string eol, string[] chunks)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks(eol, chunks);
        string text = string.Concat(chunks);

        for (int offset = 0; offset <= text.Length; offset++)
        {
            Position pos = tree.GetPositionAt(offset);
            Assert.Equal(offset, tree.GetOffsetAt(pos.LineNumber, pos.Column));
        }
    }

    [Fact]
    public void GetPositionAt_NegativeOffset_ReturnsStart()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "a\nb");

        Assert.Equal(new Position(1, 1), tree.GetPositionAt(-5));
    }

    [Fact]
    public void GetPositionAt_EmptyTree_ReturnsStart()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n");

        Assert.Equal(new Position(1, 1), tree.GetPositionAt(0));
    }

    [Fact]
    public void GetPositionAt_BetweenCrAndLf_StaysOnTheFirstLine()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "a\r\nb");

        Assert.Equal(new Position(1, 2), tree.GetPositionAt(1));
        Assert.Equal(new Position(1, 3), tree.GetPositionAt(2));
        Assert.Equal(new Position(2, 1), tree.GetPositionAt(3));
    }

    [Fact]
    public void GetValueInRange_AcrossChunks_ConcatenatesPieces()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "abc\nde", "f\nghi", "\njkl");

        Assert.Equal("c\ndef\ngh", tree.GetValueInRange(new Range(1, 3, 3, 3)));
        Assert.Equal("abc\ndef\nghi\njkl", tree.GetValueInRange(new Range(1, 1, 4, 4)));
        Assert.Equal("ef", tree.GetValueInRange(new Range(2, 2, 2, 4)));
        Assert.Equal("", tree.GetValueInRange(new Range(2, 2, 2, 2)));
    }

    [Fact]
    public void GetValueInRange_EolArgument_ReplacesLineBreaks()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "a\r\nb", "\r\nc");
        var all = new Range(1, 1, 3, 2);

        Assert.Equal("a\r\nb\r\nc", tree.GetValueInRange(all));
        Assert.Equal("a\r\nb\r\nc", tree.GetValueInRange(all, "\r\n"));
        Assert.Equal("a\nb\nc", tree.GetValueInRange(all, "\n"));
        Assert.Equal("a\r\nb\r\nc", tree.GetValueInRange(all, ""));
    }

    [Fact]
    public void GetValueInRange_NotNormalizedEol_ReplacesLoneCr()
    {
        var buffers = new List<StringBuffer> { new("a\rb\nc\r\nd", LineStarts.CreateFast("a\rb\nc\r\nd")) };
        var tree = new PieceTreeBase(buffers, "\n", eolNormalized: false);

        Assert.Equal("a\nb\nc\nd", tree.GetValueInRange(new Range(1, 1, 4, 2), "\n"));
    }

    [Fact]
    public void GetValueInRange_OutsideDocument_Throws()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\ncd");

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.GetValueInRange(new Range(1, 1, 9, 1)));
    }

    [Fact]
    public void GetLineCharCode_Lines_ReturnsCharacters()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\nc", "de\nf");

        Assert.Equal('a', tree.GetLineCharCode(1, 0));
        Assert.Equal('b', tree.GetLineCharCode(1, 1));
        Assert.Equal('\n', tree.GetLineCharCode(1, 2));
        Assert.Equal('c', tree.GetLineCharCode(2, 0));
        Assert.Equal('d', tree.GetLineCharCode(2, 1));
        Assert.Equal('f', tree.GetLineCharCode(3, 0));
    }

    [Fact]
    public void GetCharCode_EveryOffset_MatchesText()
    {
        string[] chunks = ["ab\nc", "de\nf", "\ng"];
        string text = string.Concat(chunks);
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", chunks);

        for (int offset = 0; offset < text.Length; offset++)
        {
            Assert.Equal(text[offset], tree.GetCharCode(offset));
        }

        Assert.Equal('\0', tree.GetCharCode(text.Length));
    }

    [Fact]
    public void GetCharCode_OutsideDocument_Throws()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab");

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.GetCharCode(3));
    }

    [Fact]
    public void GetLineLength_Lines_ExcludesLineBreak()
    {
        PieceTreeBase lf = TestPieceTrees.FromChunks("\n", "ab\n\ncde", "f\n");
        PieceTreeBase crlf = TestPieceTrees.FromChunks("\r\n", "ab\r\n\r\ncde", "f\r\n");

        foreach (PieceTreeBase tree in new[] { lf, crlf })
        {
            Assert.Equal(2, tree.GetLineLength(1));
            Assert.Equal(0, tree.GetLineLength(2));
            Assert.Equal(4, tree.GetLineLength(3));
            Assert.Equal(0, tree.GetLineLength(4));
        }
    }

    [Fact]
    public void GetNearestChunk_Offsets_ReturnsRestOfThePiece()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "abc", "de\nf", "gh");

        Assert.Equal("abc", tree.GetNearestChunk(0));
        Assert.Equal("bc", tree.GetNearestChunk(1));
        Assert.Equal("de\nf", tree.GetNearestChunk(3));
        Assert.Equal("e\nf", tree.GetNearestChunk(4));
        Assert.Equal("gh", tree.GetNearestChunk(7));
        Assert.Equal("h", tree.GetNearestChunk(8));
        Assert.Equal("", tree.GetNearestChunk(9));
    }

    [Fact]
    public void OffsetOfNode_Nodes_MatchesNodeStartOffsets()
    {
        string[] chunks = ["abc", "de\nf", "gh", "i", "jk"];
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", chunks);

        int expected = 0;
        TreeNode node = tree.Tree.Root;
        while (!ReferenceEquals(node.Left, tree.Tree.Sentinel))
        {
            node = node.Left;
        }

        foreach (string chunk in chunks)
        {
            Assert.Equal(expected, tree.OffsetOfNode(node));
            Assert.Equal(expected, tree.NodeAt(expected + 1).NodeStartOffset);
            expected += chunk.Length;
            node = tree.Tree.Next(node);
        }
    }

    [Fact]
    public void NodeCharCodeAt_PieceWithoutLineFeed_ReturnsMinusOne()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "abc", "d\ne");

        Assert.Equal(-1, tree.NodeCharCodeAt(tree.NodeAt(1).Node, 0));
        Assert.Equal('\n', tree.NodeCharCodeAt(tree.NodeAt(5).Node, 1));
    }
}
