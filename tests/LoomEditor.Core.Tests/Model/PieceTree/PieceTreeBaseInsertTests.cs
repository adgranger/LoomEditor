// Not ported from Monaco: Monaco tests insert only through its larger suites (ported in P1-10 to P1-12). All tests here are LoomEditor additions.

using System.Text;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeBaseInsertTests
{
    private static string InsertAndCheck(PieceTreeBase tree, string text, int offset, string value)
    {
        tree.Insert(offset, value);
        string expected = text.Insert(offset, value);
        Assert.Equal(expected, tree.GetLinesRawContent());
        Assert.Equal(expected.Length, tree.GetLength());
        PieceTreeAssert.LinesContent(expected, tree);
        PieceTreeAssert.LineStarts(expected, tree);
        TreeInvariants.AssertValid(tree.Tree);
        return expected;
    }

    private static int CountNodes(PieceTreeBase tree)
    {
        int count = 0;
        TreeNode sentinel = tree.Tree.Sentinel;
        if (ReferenceEquals(tree.Tree.Root, sentinel))
        {
            return 0;
        }

        for (TreeNode n = tree.Tree.Leftest(tree.Tree.Root); !ReferenceEquals(n, sentinel); n = tree.Tree.Next(n))
        {
            count++;
        }

        return count;
    }

    [Fact]
    public void Insert_EmptyTree_AddsText()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "");
        InsertAndCheck(tree, "", 0, "a\nb");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void Insert_OnePieceTree_StartMiddleEnd(int offset)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\ncd");
        InsertAndCheck(tree, "ab\ncd", offset, "X\nY");
    }

    [Fact]
    public void Insert_ManyAppendsAtEnd_KeepOneChangeBufferPiece()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "abc");
        string text = "abc";
        for (int i = 0; i < 100; i++)
        {
            text = InsertAndCheck(tree, text, text.Length, i % 10 == 9 ? "\n" : "x");
        }

        Assert.Equal(2, CountNodes(tree));
    }

    [Fact]
    public void Insert_TextLongerThanAverageBufferSize_SplitsIntoSeveralBuffers()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 150_000; i++)
        {
            sb.Append(i % 50 == 49 ? '\n' : (char)('a' + (i % 26)));
        }

        string big = sb.ToString();
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "start-end");
        string text = InsertAndCheck(tree, "start-end", 6, big);

        Assert.True(CountNodes(tree) >= 4);
        Assert.StartsWith("start-", text);
    }

    [Fact]
    public void Insert_LongTextEndingWithCarriageReturnAtSplit_KeepsCrlfTogether()
    {
        string big = new string('a', PieceTreeConstants.AverageBufferSize - 1) + "\r\nb";
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "x");
        tree.Insert(0, big);
        Assert.Equal(big + "x", tree.GetLinesRawContent());
        Assert.Equal(2, tree.GetLineCount());
    }

    [Fact]
    public void Insert_LineFeedRightAfterCarriageReturn_IsOneLineBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "ab");
        string text = InsertAndCheck(tree, "ab", 2, "\r");
        Assert.Equal(2, tree.GetLineCount());
        text = InsertAndCheck(tree, text, 3, "\n");
        Assert.Equal(2, tree.GetLineCount());
        Assert.Equal("ab\r\n", text);
    }

    [Fact]
    public void Insert_LineFeedAfterCarriageReturnAtEndOfPiece_IsOneLineBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "ab\r", "cd");
        string text = InsertAndCheck(tree, "ab\rcd", 3, "\n");
        Assert.Equal(2, tree.GetLineCount());
        Assert.Equal("ab\r\ncd", text);
    }

    [Fact]
    public void Insert_CarriageReturnBeforeLineFeed_IsOneLineBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "ab", "\ncd");
        string text = InsertAndCheck(tree, "ab\ncd", 2, "\r");
        Assert.Equal(2, tree.GetLineCount());
        Assert.Equal("ab\r\ncd", text);
    }

    [Fact]
    public void Insert_CarriageReturnInMiddleOfPieceBeforeLineFeed_IsOneLineBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "ab\ncd");
        string text = InsertAndCheck(tree, "ab\ncd", 2, "x\r");
        Assert.Equal("abx\r\ncd", text);
        Assert.Equal(2, tree.GetLineCount());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Insert_RandomShortStrings_MatchStringModel(int seed)
    {
        var random = new Random(seed);
        string[] alphabet = ["a", "b", "\r", "\n", "\r\n", "\n\r", "xyz", ""];
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "ab\r\ncd");
        string text = "ab\r\ncd";
        for (int op = 0; op < 200; op++)
        {
            int offset = random.Next(text.Length + 1);
            string value = alphabet[random.Next(alphabet.Length)] + alphabet[random.Next(alphabet.Length)];
            if (value.Length == 0)
            {
                continue;
            }

            try
            {
                text = InsertAndCheck(tree, text, offset, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"seed={seed} op={op} offset={offset} value={value.Replace("\r", "\\r").Replace("\n", "\\n")}", ex);
            }
        }
    }
}
