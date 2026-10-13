// Not ported from Monaco: Monaco tests delete only through its larger suites (ported in P1-10 to P1-12). All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeBaseDeleteTests
{
    private static string Check(PieceTreeBase tree, string expected)
    {
        Assert.Equal(expected, tree.GetLinesRawContent());
        Assert.Equal(expected.Length, tree.GetLength());
        PieceTreeAssert.LinesContent(expected, tree);
        PieceTreeAssert.LineStarts(expected, tree);
        TreeInvariants.AssertValid(tree.Tree);
        return expected;
    }

    private static string DeleteAndCheck(PieceTreeBase tree, string text, int offset, int count)
    {
        tree.Delete(offset, count);
        return Check(tree, count <= 0 ? text : text.Remove(offset, count));
    }

    private static string InsertAndCheck(PieceTreeBase tree, string text, int offset, string value)
    {
        tree.Insert(offset, value);
        return Check(tree, text.Insert(offset, value));
    }

    [Fact]
    public void Delete_ZeroOrNegativeCount_DoesNothing()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\ncd");
        DeleteAndCheck(tree, "ab\ncd", 1, 0);
        DeleteAndCheck(tree, "ab\ncd", 1, -3);
    }

    [Fact]
    public void Delete_EmptyTree_DoesNothing()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "");
        tree.Delete(0, 5);
        Check(tree, "");
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(3, 2)]
    [InlineData(2, 1)]
    [InlineData(0, 5)]
    public void Delete_InsideOnePiece_HeadMiddleTailAndAll(int offset, int count)
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab\ncd");
        DeleteAndCheck(tree, "ab\ncd", offset, count);
    }

    [Fact]
    public void Delete_WholePiece_RemovesIt()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab", "c\nd", "ef");
        DeleteAndCheck(tree, "abc\ndef", 2, 3);
    }

    [Fact]
    public void Delete_AcrossSeveralPieces_RemovesInnerPieces()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab", "c\nd", "ef", "g\nh");
        DeleteAndCheck(tree, "abc\ndefg\nh", 1, 7);
    }

    [Fact]
    public void Delete_Everything_LeavesEmptyDocument()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\n", "ab", "c\nd", "ef");
        DeleteAndCheck(tree, "abc\ndef", 0, 7);
        Assert.Equal(1, tree.GetLineCount());
        InsertAndCheck(tree, "", 0, "x\ny");
    }

    [Fact]
    public void Delete_BetweenCarriageReturnAndLineFeed_JoinsThem()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", false, "\rx\n");
        string text = DeleteAndCheck(tree, "\rx\n", 1, 1);
        Assert.Equal("\r\n", text);
        Assert.Equal(2, tree.GetLineCount());
    }

    [Fact]
    public void Delete_AroundCrlfPair_ThenReinsert_KeepsLineCountConsistent()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", "a\r\nb");
        string text = DeleteAndCheck(tree, "a\r\nb", 0, 1);
        text = DeleteAndCheck(tree, text, 2, 1);
        text = InsertAndCheck(tree, text, 0, "a");
        text = InsertAndCheck(tree, text, 3, "b");
        Assert.Equal("a\r\nb", text);
        Assert.Equal(2, tree.GetLineCount());
    }

    [Fact]
    public void Delete_CarriageReturnOfCrlfPair_KeepsLineCountConsistent()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", false, "a\r\nb");
        string text = DeleteAndCheck(tree, "a\r\nb", 1, 1);
        Assert.Equal("a\nb", text);
        Assert.Equal(2, tree.GetLineCount());
    }

    [Fact]
    public void Delete_AcrossPiecesJoiningCarriageReturnAndLineFeed_MakesOneBreak()
    {
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", false, "a\r", "xy", "\nb");
        string text = DeleteAndCheck(tree, "a\rxy\nb", 2, 2);
        Assert.Equal("a\r\nb", text);
        Assert.Equal(2, tree.GetLineCount());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void InsertDelete_RandomOperations_MatchStringModel(int seed)
    {
        var random = new Random(seed);
        string[] alphabet = ["a", "b", "\r", "\n", "\r\n", "\n\r", "xyz"];
        PieceTreeBase tree = TestPieceTrees.FromChunks("\r\n", false, "ab\r\ncd", "e\rf", "\ng");
        string text = "ab\r\ncde\rf\ng";
        for (int op = 0; op < 300; op++)
        {
            bool insert = text.Length == 0 || random.Next(2) == 0;
            int offset = random.Next(text.Length + 1);
            string description = string.Empty;
            try
            {
                if (insert)
                {
                    string value = alphabet[random.Next(alphabet.Length)] + alphabet[random.Next(alphabet.Length)];
                    description = $"insert {value.Replace("\r", "\\r").Replace("\n", "\\n")}";
                    text = InsertAndCheck(tree, text, offset, value);
                }
                else
                {
                    int count = random.Next(text.Length - offset + 1);
                    description = $"delete {count}";
                    text = DeleteAndCheck(tree, text, offset, count);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"seed={seed} op={op} offset={offset} {description}", ex);
            }
        }
    }
}
