// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'inserts and deletes')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// Despite their names, the "random" tests are recorded sequences without any randomness.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeInsertsAndDeletesTests
{
    // Monaco: 'basic insert/delete'
    [Fact]
    public void BasicInsertDelete()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["This is a document with some text."]).GetPieceTree();

        pt.Insert(34, "This is some more text to insert at offset 34.");
        Assert.Equal(
            "This is a document with some text.This is some more text to insert at offset 34.",
            pt.GetLinesRawContent());
        pt.Delete(42, 5);
        Assert.Equal(
            "This is a document with some text.This is more text to insert at offset 34.",
            pt.GetLinesRawContent());
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'more inserts'
    [Fact]
    public void MoreInserts()
    {
        PieceTreeBase pt = TestTextBuffers.Create([""]).GetPieceTree();
        pt.Insert(0, "AAA");
        Assert.Equal("AAA", pt.GetLinesRawContent());
        pt.Insert(0, "BBB");
        Assert.Equal("BBBAAA", pt.GetLinesRawContent());
        pt.Insert(6, "CCC");
        Assert.Equal("BBBAAACCC", pt.GetLinesRawContent());
        pt.Insert(5, "DDD");
        Assert.Equal("BBBAADDDACCC", pt.GetLinesRawContent());
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'more deletes'
    [Fact]
    public void MoreDeletes()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["012345678"]).GetPieceTree();

        pt.Delete(8, 1);
        Assert.Equal("01234567", pt.GetLinesRawContent());
        pt.Delete(0, 1);
        Assert.Equal("1234567", pt.GetLinesRawContent());
        pt.Delete(5, 1);
        Assert.Equal("123457", pt.GetLinesRawContent());
        pt.Delete(5, 1);
        Assert.Equal("12345", pt.GetLinesRawContent());
        pt.Delete(0, 5);
        Assert.Equal("", pt.GetLinesRawContent());
        TreeInvariants.AssertValid(pt.Tree);
    }

    // Monaco: 'random test 1'
    [Fact]
    public void RandomTest1()
    {
        var m = new MirroredPieceTree("");

        m.Insert(0, "ceLPHmFzvCtFeHkCBej ");
        m.AssertContent();
        m.Insert(8, "gDCEfNYiBUNkSwtvB K ");
        m.AssertContent();
        m.Insert(38, "cyNcHxjNPPoehBJldLS ");
        m.AssertContent();
        m.Insert(59, "ejMx\nOTgWlbpeDExjOk ");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random test 2'
    [Fact]
    public void RandomTest2()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "VgPG ");
        m.Insert(2, "DdWF ");
        m.Insert(0, "hUJc ");
        m.Insert(8, "lQEq ");
        m.Insert(10, "Gbtp ");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random test 3'
    [Fact]
    public void RandomTest3()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "gYSz");
        m.Insert(1, "mDQe");
        m.Insert(1, "DTMQ");
        m.Insert(2, "GGZB");
        m.Insert(12, "wXpq");
        m.AssertContent();
    }

    // Monaco: 'random delete 1'
    [Fact]
    public void RandomDelete1()
    {
        var m = new MirroredPieceTree("");

        m.Insert(0, "vfb");
        m.AssertContent();
        m.Insert(0, "zRq");
        m.AssertContent();

        m.Delete(5, 1);
        m.AssertContent();

        m.Insert(1, "UNw");
        m.AssertContent();

        m.Delete(4, 3);
        m.AssertContent();

        m.Delete(1, 4);
        m.AssertContent();

        m.Delete(0, 1);
        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random delete 2'
    [Fact]
    public void RandomDelete2()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "IDT");
        m.Insert(3, "wwA");
        m.Insert(3, "Gnr");
        m.Delete(6, 3);
        m.Insert(4, "eHp");
        m.Insert(1, "UAi");
        m.Insert(2, "FrR");
        m.Delete(6, 7);
        m.Delete(3, 5);
        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random delete 3'
    [Fact]
    public void RandomDelete3()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "PqM");
        m.Delete(1, 2);
        m.Insert(1, "zLc");
        m.Insert(0, "MEX");
        m.Insert(0, "jZh");
        m.Insert(8, "GwQ");
        m.Delete(5, 6);
        m.Insert(4, "ktw");
        m.Insert(5, "GVu");
        m.Insert(9, "jdm");
        m.Insert(15, "na\n");
        m.Delete(5, 8);
        m.Delete(3, 4);
        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random insert/delete \r bug 1'
    [Fact]
    public void RandomInsertDeleteCrBug1()
    {
        var m = new MirroredPieceTree("a");
        m.Delete(0, 1);
        m.Insert(0, "\r\r\n\n");
        m.Delete(3, 1);
        m.Insert(2, "\n\n\ra");
        m.Delete(4, 3);
        m.Insert(2, "\na\r\r");
        m.Insert(6, "\ra\n\n");
        m.Insert(0, "aa\n\n");
        m.Insert(5, "\n\na\r");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random insert/delete \r bug 2'
    [Fact]
    public void RandomInsertDeleteCrBug2()
    {
        var m = new MirroredPieceTree("a");
        m.Insert(1, "\naa\r");
        m.Delete(0, 4);
        m.Insert(1, "\r\r\na");
        m.Insert(2, "\n\r\ra");
        m.Delete(4, 1);
        m.Insert(8, "\r\n\r\r");
        m.Insert(7, "\n\n\na");
        m.Insert(13, "a\n\na");
        m.Delete(17, 3);
        m.Insert(2, "a\ra\n");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random insert/delete \r bug 3'
    [Fact]
    public void RandomInsertDeleteCrBug3()
    {
        var m = new MirroredPieceTree("a");
        m.Insert(0, "\r\na\r");
        m.Delete(2, 3);
        m.Insert(2, "a\r\n\r");
        m.Delete(4, 2);
        m.Insert(4, "a\n\r\n");
        m.Insert(1, "aa\n\r");
        m.Insert(7, "\na\r\n");
        m.Insert(5, "\n\na\r");
        m.Insert(10, "\r\r\n\r");
        m.AssertContent();
        m.Delete(21, 3);

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random insert/delete \r bug 4s'
    [Fact]
    public void RandomInsertDeleteCrBug4s()
    {
        var m = new MirroredPieceTree("a");
        m.Delete(0, 1);
        m.Insert(0, "\naaa");
        m.Insert(2, "\n\naa");
        m.Delete(1, 4);
        m.Delete(3, 1);
        m.Delete(1, 2);
        m.Delete(0, 1);
        m.Insert(0, "a\n\n\r");
        m.Insert(2, "aa\r\n");
        m.Insert(3, "a\naa");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'random insert/delete \r bug 5'
    [Fact]
    public void RandomInsertDeleteCrBug5()
    {
        var m = new MirroredPieceTree("");
        m.Insert(0, "\n\n\n\r");
        m.Insert(1, "\n\n\n\r");
        m.Insert(2, "\n\r\r\r");
        m.Insert(8, "\n\r\n\r");
        m.Delete(5, 2);
        m.Insert(4, "\n\r\r\r");
        m.Insert(8, "\n\n\n\r");
        m.Delete(0, 7);
        m.Insert(1, "\r\n\r\r");
        m.Insert(15, "\n\r\r\r");

        m.AssertContent();
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
