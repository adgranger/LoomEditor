// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'buffer api')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeBufferApiTests
{
    // Monaco: 'equal'
    [Fact]
    public void Equal()
    {
        PieceTreeBase a = TestTextBuffers.Create(["abc"]).GetPieceTree();
        PieceTreeBase b = TestTextBuffers.Create(["ab", "c"]).GetPieceTree();
        PieceTreeBase c = TestTextBuffers.Create(["abd"]).GetPieceTree();
        PieceTreeBase d = TestTextBuffers.Create(["abcd"]).GetPieceTree();

        Assert.True(a.Equal(b));
        Assert.False(a.Equal(c));
        Assert.False(a.Equal(d));
    }

    // Monaco: 'equal with more chunks'
    [Fact]
    public void EqualWithMoreChunks()
    {
        PieceTreeBase a = TestTextBuffers.Create(["ab", "cd", "e"]).GetPieceTree();
        PieceTreeBase b = TestTextBuffers.Create(["ab", "c", "de"]).GetPieceTree();

        Assert.True(a.Equal(b));
    }

    // Monaco: 'equal 2, empty buffer'
    [Fact]
    public void Equal2EmptyBuffer()
    {
        PieceTreeBase a = TestTextBuffers.Create([""]).GetPieceTree();
        PieceTreeBase b = TestTextBuffers.Create([""]).GetPieceTree();

        Assert.True(a.Equal(b));
    }

    // Monaco: 'equal 3, empty buffer'
    [Fact]
    public void Equal3EmptyBuffer()
    {
        PieceTreeBase a = TestTextBuffers.Create(["a"]).GetPieceTree();
        PieceTreeBase b = TestTextBuffers.Create([""]).GetPieceTree();

        Assert.False(a.Equal(b));
    }

    // Monaco: 'getLineCharCode - issue #45735'
    [Fact]
    public void GetLineCharCode_Issue45735()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["LINE1\nline2"]).GetPieceTree();

        AssertLineChars(pt, 1, "LINE1\n");
        AssertLineChars(pt, 2, "line2");
    }

    // Monaco: 'getLineCharCode - issue #47733'
    [Fact]
    public void GetLineCharCode_Issue47733()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["", "LINE1\n", "line2"]).GetPieceTree();

        AssertLineChars(pt, 1, "LINE1\n");
        AssertLineChars(pt, 2, "line2");
    }

    // Monaco: 'getNearestChunk'
    [Fact]
    public void GetNearestChunk()
    {
        PieceTreeBase pt = TestTextBuffers.Create(["012345678"]).GetPieceTree();

        pt.Insert(3, "ABC");
        Assert.Equal("012ABC345678", pt.GetLineContent(1));
        Assert.Equal("ABC", pt.GetNearestChunk(3));
        Assert.Equal("345678", pt.GetNearestChunk(6));

        pt.Delete(9, 1);
        Assert.Equal("012ABC34578", pt.GetLineContent(1));
        Assert.Equal("345", pt.GetNearestChunk(6));
        Assert.Equal("78", pt.GetNearestChunk(9));
    }

    private static void AssertLineChars(PieceTreeBase pt, int lineNumber, string expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], pt.GetLineCharCode(lineNumber, i));
        }
    }
}
