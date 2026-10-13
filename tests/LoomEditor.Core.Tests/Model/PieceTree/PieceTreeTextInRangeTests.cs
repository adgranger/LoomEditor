// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'get text in range')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// Despite their names, the "random" tests are recorded sequences without any randomness.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeTextInRangeTests
{
    // Monaco: 'getContentInRange'
    [Fact]
    public void GetContentInRange()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\nde"]).GetPieceTree();
        pt.Insert(8, "fh\ni\njk");
        pt.Delete(7, 2);
        // 'a\nb\nc\ndh\ni\njk'

        Assert.Equal("a\n", pt.GetValueInRange(new Range(1, 1, 1, 3)));
        Assert.Equal("b\n", pt.GetValueInRange(new Range(2, 1, 2, 3)));
        Assert.Equal("c\n", pt.GetValueInRange(new Range(3, 1, 3, 3)));
        Assert.Equal("dh\n", pt.GetValueInRange(new Range(4, 1, 4, 4)));
        Assert.Equal("i\n", pt.GetValueInRange(new Range(5, 1, 5, 3)));
        Assert.Equal("jk", pt.GetValueInRange(new Range(6, 1, 6, 3)));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'random test value in range'
    [Fact]
    public void RandomTestValueInRange()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "ZXXY");
        m.Insert(1, "XZZY");
        m.Insert(5, "\nX\n\n");
        m.Insert(3, "\nXX\n");
        m.Insert(12, "YYYX");

        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random test value in range exception'
    [Fact]
    public void RandomTestValueInRangeException()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "XZ\nZ");
        m.Delete(0, 3);
        m.Delete(0, 1);
        m.Insert(0, "ZYX\n");
        m.Delete(0, 4);

        m.Tree.GetValueInRange(new Range(1, 1, 1, 1));
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random tests bug 1'
    [Fact]
    public void RandomTestsBug1()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "huuyYzUfKOENwGgZLqn ");
        m.Delete(18, 2);
        m.Delete(3, 1);
        m.Delete(12, 4);
        m.Insert(3, "hMbnVEdTSdhLlPevXKF ");
        m.Delete(22, 8);
        m.Insert(4, "S umSnYrqOmOAV\nEbZJ ");

        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random tests bug 2'
    [Fact]
    public void RandomTestsBug2()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "xfouRDZwdAHjVXJAMV\n ");
        m.Insert(16, "dBGndxpFZBEAIKykYYx ");
        m.Delete(7, 6);
        m.Delete(9, 7);
        m.Delete(17, 6);
        m.Delete(0, 4);
        m.Insert(9, "qvEFXCNvVkWgvykahYt ");
        m.Delete(4, 6);
        m.Insert(11, "OcSChUYT\nzPEBOpsGmR ");
        m.Insert(15, "KJCozaXTvkE\nxnqAeTz ");

        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'get line content'
    [Fact]
    public void GetLineContent()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["1"]).GetPieceTree();

        Assert.Equal("1", pt.GetLineRawContent(1));
        pt.Insert(1, "2");
        Assert.Equal("12", pt.GetLineRawContent(1));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'get line content basic'
    [Fact]
    public void GetLineContentBasic()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["1\n2\n3\n4"]).GetPieceTree();

        Assert.Equal("1\n", pt.GetLineRawContent(1));
        Assert.Equal("2\n", pt.GetLineRawContent(2));
        Assert.Equal("3\n", pt.GetLineRawContent(3));
        Assert.Equal("4", pt.GetLineRawContent(4));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'get line content after inserts/deletes'
    [Fact]
    public void GetLineContentAfterInsertsDeletes()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["a\nb\nc\nde"]).GetPieceTree();
        pt.Insert(8, "fh\ni\njk");
        pt.Delete(7, 2);
        // 'a\nb\nc\ndh\ni\njk'

        Assert.Equal("a\n", pt.GetLineRawContent(1));
        Assert.Equal("b\n", pt.GetLineRawContent(2));
        Assert.Equal("c\n", pt.GetLineRawContent(3));
        Assert.Equal("dh\n", pt.GetLineRawContent(4));
        Assert.Equal("i\n", pt.GetLineRawContent(5));
        Assert.Equal("jk", pt.GetLineRawContent(6));
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'random 1'
    [Fact]
    public void Random1()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "J eNnDzQpnlWyjmUu\ny ");
        m.Insert(0, "QPEeRAQmRwlJqtZSWhQ ");
        m.Delete(5, 1);

        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random 2'
    [Fact]
    public void Random2()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "DZoQ tglPCRHMltejRI ");
        m.Insert(10, "JRXiyYqJ qqdcmbfkKX ");
        m.Delete(16, 3);
        m.Delete(25, 1);
        m.Insert(18, "vH\nNlvfqQJPm\nSFkhMc ");

        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
