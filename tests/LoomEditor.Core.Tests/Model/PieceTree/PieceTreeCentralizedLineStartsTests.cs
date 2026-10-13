// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'centralized lineStarts with CRLF')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeCentralizedLineStartsTests
{
    // Monaco: 'delete CR in CRLF 1'
    [Fact]
    public void DeleteCrInCrlf1()
    {
        var m = new MirroredPieceTree("a\r\nb", normalizeEol: false);
        m.Delete(2, 2);
        Assert.Equal(2, m.Tree.GetLineCount());
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'delete CR in CRLF 2'
    [Fact]
    public void DeleteCrInCrlf2()
    {
        var m = new MirroredPieceTree("a\r\nb");
        m.Delete(0, 2);
        Assert.Equal(2, m.Tree.GetLineCount());
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 1'
    [Fact]
    public void RandomBug1()
    {
        var m = new MirroredPieceTree("\n\n\r\r", normalizeEol: false);
        m.Insert(1, "\r\n\r\n");
        m.Delete(5, 3);
        m.Delete(2, 3);
        Assert.Equal(PieceTreeAssert.SplitLines(m.Text).Count, m.Tree.GetLineCount());
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 2'
    [Fact]
    public void RandomBug2()
    {
        var m = new MirroredPieceTree("\n\r\n\r", normalizeEol: false);
        m.Insert(2, "\n\r\r\r");
        m.Delete(4, 1);
        Assert.Equal(PieceTreeAssert.SplitLines(m.Text).Count, m.Tree.GetLineCount());
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 3'
    [Fact]
    public void RandomBug3()
    {
        var m = new MirroredPieceTree("\n\n\n\r", normalizeEol: false);
        m.Delete(2, 2);
        m.Delete(0, 2);
        m.Insert(0, "\r\r\r\r");
        m.Insert(2, "\r\n\r\r");
        m.Insert(3, "\r\r\r\n");
        Assert.Equal(PieceTreeAssert.SplitLines(m.Text).Count, m.Tree.GetLineCount());
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 4'
    [Fact]
    public void RandomBug4()
    {
        var m = new MirroredPieceTree("\n\n\n\n", normalizeEol: false);
        m.Delete(3, 1);
        m.Insert(1, "\r\r\r\r");
        m.Insert(6, "\r\n\n\r");
        m.Delete(5, 3);
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 5'
    [Fact]
    public void RandomBug5()
    {
        var m = new MirroredPieceTree("\n\n\n\n", normalizeEol: false);
        m.Delete(3, 1);
        m.Insert(0, "\n\r\r\n");
        m.Insert(4, "\n\r\r\n");
        m.Delete(4, 3);
        m.Insert(5, "\r\r\n\r");
        m.Insert(12, "\n\n\n\r");
        m.Insert(5, "\r\r\r\n");
        m.Insert(20, "\n\n\r\n");
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 6'
    [Fact]
    public void RandomBug6()
    {
        var m = new MirroredPieceTree("\n\r\r\n", normalizeEol: false);
        m.Insert(4, "\r\n\n\r");
        m.Insert(3, "\r\n\n\n");
        m.Delete(4, 8);
        m.Insert(4, "\r\n\n\r");
        m.Insert(0, "\r\n\n\r");
        m.Delete(4, 0);
        m.Delete(8, 4);
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 7'
    [Fact]
    public void RandomBug7()
    {
        var m = new MirroredPieceTree("\r\n\n\r", normalizeEol: false);
        m.Delete(1, 0);
        m.Insert(3, "\n\n\n\r");
        m.Insert(7, "\n\n\r\n");
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 8'
    [Fact]
    public void RandomBug8()
    {
        var m = new MirroredPieceTree("\r\r\n\n", normalizeEol: false);
        m.Insert(4, "\r\n\n\r");
        m.Insert(7, "\n\r\r\r");
        m.Insert(11, "\n\n\r\n");
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 9'
    [Fact]
    public void RandomBug9()
    {
        var m = new MirroredPieceTree("qneW", normalizeEol: false);
        m.Insert(0, "YhIl");
        m.Insert(0, "qdsm");
        m.Delete(7, 0);
        m.Insert(12, "iiPv");
        m.Insert(9, "V\rSA");
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random bug 10'
    [Fact]
    public void RandomBug10()
    {
        var m = new MirroredPieceTree("\n\n\n\n", normalizeEol: false);
        m.Insert(3, "\n\r\n\r");
        m.Insert(2, "\n\r\n\n");
        m.Insert(0, "\n\n\r\r");
        m.Insert(3, "\r\r\r\r");
        m.Insert(3, "\n\n\r\r");
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random chunk bug 1'
    [Fact]
    public void RandomChunkBug1()
    {
        var m = new MirroredPieceTree("\n\r\r\n\n\n\r\n\r", normalizeEol: false);
        m.Delete(0, 2);
        m.Insert(1, "\r\r\n\n");
        m.Insert(7, "\r\r\r\r");
        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random chunk bug 2'
    [Fact]
    public void RandomChunkBug2()
    {
        var m = new MirroredPieceTree("\n\r\n\n\n\r\n\r\n\r\r\n\n\n\r\r\n\r\n", normalizeEol: false);
        m.Insert(16, "\r\n\r\r");
        m.Insert(13, "\n\n\r\r");
        m.Insert(19, "\n\n\r\n");
        m.Delete(5, 0);
        m.Delete(11, 2);
        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random chunk bug 3'
    [Fact]
    public void RandomChunkBug3()
    {
        var m = new MirroredPieceTree("\r\n\n\n\n\n\n\r\n", normalizeEol: false);
        m.Insert(4, "\n\n\r\n\r\r\n\n\r");
        m.Delete(4, 4);
        m.Insert(11, "\r\n\r\n\n\r\r\n\n");
        m.Delete(1, 2);
        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random chunk bug 4'
    [Fact]
    public void RandomChunkBug4()
    {
        var m = new MirroredPieceTree("\n\r\n\r", normalizeEol: false);
        m.Insert(4, "\n\n\r\n");
        m.Insert(3, "\r\n\n\n");
        m.AssertContent();
        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
