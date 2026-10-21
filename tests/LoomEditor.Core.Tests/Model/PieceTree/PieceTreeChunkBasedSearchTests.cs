// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'chunk based search')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeChunkBasedSearchTests
{
    private static readonly string Lf = ((char)10).ToString();

    private static WordCharacterClassifier Separators => WordCharacterClassifierCache.Get(",./", []);

    // Monaco: '#45892. For some cases, the buffer is empty but we still try to search'
    [Fact]
    public void FindMatchesLineByLine_EmptyBuffer_ReturnsNothing()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create([""]);
        buffer.GetPieceTree().Delete(0, 1);

        var ret = buffer.FindMatchesLineByLine(new Range(1, 1, 1, 1), new SearchData(new Regex("abc"), Separators, "abc"), true, 1000);

        Assert.Empty(ret);
    }

    // Monaco: '#45770. FindInNode should not cross node boundary.'
    [Fact]
    public void FindMatchesLineByLine_FindInNode_DoesNotCrossNodeBoundary()
    {
        PieceTreeBase pieceTable = TestTextBuffers.Create(
        [
            string.Join(Lf, "balabalababalabalababalabalaba", "balabalababalabalababalabalaba", "", "* [ ] task1", "* [x] task2 balabalaba", "* [ ] task 3"),
        ]).GetPieceTree();

        pieceTable.Delete(0, 62);
        pieceTable.Delete(16, 1);
        pieceTable.Insert(16, " ");
        var regex = new Regex(@"\[", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var ret = pieceTable.FindMatchesLineByLine(new Range(1, 1, 4, 13), new SearchData(regex, Separators, "["), true, 1000);

        Assert.Equal(3, ret.Count);
        Assert.Equal(new Range(2, 3, 2, 4), ret[0].Range);
        Assert.Equal(new Range(3, 3, 3, 4), ret[1].Range);
        Assert.Equal(new Range(4, 3, 4, 4), ret[2].Range);
    }

    // Monaco: 'search searching from the middle'
    [Fact]
    public void FindMatchesLineByLine_SearchingFromTheMiddle_FindsMatch()
    {
        PieceTreeBase pieceTable = TestTextBuffers.Create([string.Join(Lf, "def", "dbcabc")]).GetPieceTree();
        var regex = new Regex("a", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        pieceTable.Delete(4, 1);
        var ret = pieceTable.FindMatchesLineByLine(new Range(2, 3, 2, 6), new SearchData(regex, null, "a"), true, 1000);
        Assert.Single(ret);
        Assert.Equal(new Range(2, 3, 2, 4), ret[0].Range);

        pieceTable.Delete(4, 1);
        ret = pieceTable.FindMatchesLineByLine(new Range(2, 2, 2, 5), new SearchData(regex, null, "a"), true, 1000);
        Assert.Single(ret);
        Assert.Equal(new Range(2, 2, 2, 3), ret[0].Range);
    }

    [Fact]
    public void TryNodeAt2_EmptyBuffer_ReturnsNull()
    {
        PieceTreeBase pieceTable = TestTextBuffers.Create([""]).GetPieceTree();

        Assert.Null(pieceTable.TryNodeAt2(1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => pieceTable.NodeAt2(1, 5));
    }
}
