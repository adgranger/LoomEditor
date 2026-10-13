// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'prefix sum for line feed')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreePrefixSumTests
{
    private static void AssertPositions(PieceTreeBase pt, params (int Offset, int Line, int Column)[] expected)
    {
        foreach ((int offset, int line, int column) in expected)
        {
            Assert.Equal(new Position(line, column), pt.GetPositionAt(offset));
            Assert.Equal(offset, pt.GetOffsetAt(line, column));
        }
    }

    // Monaco: 'basic'
    [Fact]
    public void Basic()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["1\n2\n3\n4"]).GetPieceTree();

        Assert.Equal(4, pt.GetLineCount());
        AssertPositions(pt, (0, 1, 1), (1, 1, 2), (2, 2, 1), (3, 2, 2), (4, 3, 1), (5, 3, 2), (6, 4, 1));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'append'
    [Fact]
    public void Append()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\nde"]).GetPieceTree();
        pt.Insert(8, "fh\ni\njk");

        Assert.Equal(6, pt.GetLineCount());
        Assert.Equal(new Position(4, 4), pt.GetPositionAt(9));
        Assert.Equal(0, pt.GetOffsetAt(1, 1));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'insert'
    [Fact]
    public void Insert()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\nde"]).GetPieceTree();
        pt.Insert(7, "fh\ni\njk");

        Assert.Equal(6, pt.GetLineCount());
        AssertPositions(pt, (6, 4, 1), (7, 4, 2), (8, 4, 3), (9, 4, 4), (12, 6, 1), (13, 6, 2), (14, 6, 3));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'delete'
    [Fact]
    public void Delete()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\ndefh\ni\njk"]).GetPieceTree();

        pt.Delete(7, 2);

        Assert.Equal("a\nb\nc\ndh\ni\njk", pt.GetLinesRawContent());
        Assert.Equal(6, pt.GetLineCount());
        AssertPositions(pt, (6, 4, 1), (7, 4, 2), (8, 4, 3), (9, 5, 1), (11, 6, 1), (12, 6, 2), (13, 6, 3));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'add+delete 1'
    [Fact]
    public void AddDelete1()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\nde"]).GetPieceTree();
        pt.Insert(8, "fh\ni\njk");
        pt.Delete(7, 2);

        Assert.Equal("a\nb\nc\ndh\ni\njk", pt.GetLinesRawContent());
        Assert.Equal(6, pt.GetLineCount());
        AssertPositions(pt, (6, 4, 1), (7, 4, 2), (8, 4, 3), (9, 5, 1), (11, 6, 1), (12, 6, 2), (13, 6, 3));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'insert random bug 1: prefixSumComputer.removeValues(start, cnt) cnt is 1 based.'
    [Fact]
    public void InsertRandomBug1_RemoveValuesCountIsOneBased()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, " ZX \n Z\nZ\n YZ\nY\nZXX ");
        m.Insert(14, "X ZZ\nYZZYZXXY Y XY\n ");

        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'insert random bug 2: prefixSumComputer initialize does not do deep copy of UInt32Array.'
    [Fact]
    public void InsertRandomBug2_InitializeDoesNotDeepCopy()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "ZYZ\nYY XY\nX \nZ Y \nZ ");
        m.Insert(3, "XXY \n\nY Y YYY  ZYXY ");

        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'delete random bug 1: I forgot to update the lineFeedCnt when deletion is on one single piece.'
    [Fact]
    public void DeleteRandomBug1_LineFeedCountOnSinglePieceDeletion()
    {
        PieceTreeBase pt = TestTextBuffers.Create([""]).GetPieceTree();
        pt.Insert(0, "ba\na\nca\nba\ncbab\ncaa ");
        pt.Insert(13, "cca\naabb\ncac\nccc\nab ");
        pt.Delete(5, 8);
        pt.Delete(30, 2);
        pt.Insert(24, "cbbacccbac\nbaaab\n\nc ");
        pt.Delete(29, 3);
        pt.Delete(23, 9);
        pt.Delete(21, 5);
        pt.Delete(30, 3);
        pt.Insert(3, "cb\nac\nc\n\nacc\nbb\nb\nc ");
        pt.Delete(19, 5);
        pt.Insert(18, "\nbb\n\nacbc\ncbb\nc\nbb\n ");
        pt.Insert(65, "cbccbac\nbc\n\nccabba\n ");
        pt.Insert(77, "a\ncacb\n\nac\n\n\n\n\nabab ");
        pt.Delete(30, 9);
        pt.Insert(45, "b\n\nc\nba\n\nbbbba\n\naa\n ");
        pt.Insert(82, "ab\nbb\ncabacab\ncbc\na ");
        pt.Delete(123, 9);
        pt.Delete(71, 2);
        pt.Insert(33, "acaa\nacb\n\naa\n\nc\n\n\n\n ");

        string str = pt.GetLinesRawContent();
        PieceTreeAssert.LineStarts(str, pt);
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'delete random bug rb tree 1'
    [Fact]
    public void DeleteRandomBugRbTree1()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "YXXZ\n\nYY\n");
        m.Delete(0, 5);
        m.Insert(0, "ZXYY\nX\nZ\n");
        m.Insert(10, "\nXY\nYXYXY");
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'delete random bug rb tree 2'
    [Fact]
    public void DeleteRandomBugRbTree2()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "YXXZ\n\nYY\n");
        m.Insert(0, "ZXYY\nX\nZ\n");
        m.Insert(10, "\nXY\nYXYXY");
        m.Insert(8, "YZXY\nZ\nYX");
        m.Insert(12, "XX\nXXYXYZ");
        m.Delete(0, 4);

        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'delete random bug rb tree 3'
    [Fact]
    public void DeleteRandomBugRbTree3()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "YXXZ\n\nYY\n");
        m.Delete(7, 2);
        m.Delete(6, 1);
        m.Delete(0, 5);
        m.Insert(0, "ZXYY\nX\nZ\n");
        m.Insert(10, "\nXY\nYXYXY");
        m.Insert(8, "YZXY\nZ\nYX");
        m.Insert(12, "XX\nXXYXYZ");
        m.Delete(0, 4);
        m.Delete(30, 3);

        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
