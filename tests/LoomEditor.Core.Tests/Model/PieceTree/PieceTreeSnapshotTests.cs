// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'snapshot')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// Monaco runs these through TextModel.applyEdits; TextModel does not exist yet, so the edits are applied to the piece
// tree directly, as offset-based insert/delete. The original line/column ranges are given in comments.

using System.Text;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeSnapshotTests
{
    // Monaco: helper 'getValueInSnapshot'
    private static string GetValueInSnapshot(ITextSnapshot snapshot)
    {
        var ret = new StringBuilder();
        string? tmp = snapshot.Read();
        while (tmp is not null)
        {
            ret.Append(tmp);
            tmp = snapshot.Read();
        }

        return ret.ToString();
    }

    private static PieceTreeBase Create(string text) => TestTextBuffers.Create([text]).GetPieceTree();

    // Monaco: 'bug #45564, piece tree pieces should be immutable'
    [Fact]
    public void Snapshot_Bug45564PiecesAreImmutable()
    {
        PieceTreeBase pt = Create("\n");
        pt.Insert(1, "!"); // Range(2, 1, 2, 1) -> "!"
        ITextSnapshot snapshot = pt.CreateSnapshot("");
        ITextSnapshot snapshot1 = pt.CreateSnapshot("");
        Assert.Equal(pt.GetLinesRawContent(), GetValueInSnapshot(snapshot));

        pt.Delete(1, 1); // Range(2, 1, 2, 2) -> ""
        pt.Insert(1, "!"); // Range(2, 1, 2, 1) -> "!"

        Assert.Equal(pt.GetLinesRawContent(), GetValueInSnapshot(snapshot1));
    }

    // Monaco: 'immutable snapshot 1'
    [Fact]
    public void Snapshot_Immutable1()
    {
        PieceTreeBase pt = Create("abc\ndef");
        ITextSnapshot snapshot = pt.CreateSnapshot("");
        pt.Delete(4, 3); // Range(2, 1, 2, 4) -> ""
        pt.Delete(0, 4); // Range(1, 1, 2, 1) -> "abc\ndef", done as delete + insert
        pt.Insert(0, "abc\ndef");

        Assert.Equal(pt.GetLinesRawContent(), GetValueInSnapshot(snapshot));
    }

    // Monaco: 'immutable snapshot 2'
    [Fact]
    public void Snapshot_Immutable2()
    {
        PieceTreeBase pt = Create("abc\ndef");
        ITextSnapshot snapshot = pt.CreateSnapshot("");
        pt.Insert(4, "!"); // Range(2, 1, 2, 1) -> "!"
        pt.Delete(4, 1); // Range(2, 1, 2, 2) -> ""

        Assert.Equal(pt.GetLinesRawContent(), GetValueInSnapshot(snapshot));
    }

    // Monaco: 'immutable snapshot 3'
    [Fact]
    public void Snapshot_Immutable3()
    {
        PieceTreeBase pt = Create("abc\ndef");
        pt.Insert(7, "!"); // Range(2, 4, 2, 4) -> "!"
        ITextSnapshot snapshot = pt.CreateSnapshot("");
        pt.Insert(8, "!"); // Range(2, 5, 2, 5) -> "!"

        Assert.NotEqual(pt.GetLinesRawContent(), GetValueInSnapshot(snapshot));
    }

    [Fact]
    public void Read_EmptyTree_ReturnsBomOnceThenNull()
    {
        ITextSnapshot snapshot = Create("").CreateSnapshot("﻿");

        Assert.Equal("﻿", snapshot.Read());
        Assert.Null(snapshot.Read());
    }

    [Fact]
    public void Read_SeveralPieces_PrependsBomToFirstChunkOnly()
    {
        ITextSnapshot snapshot = TestTextBuffers.Create(["ab", "cd"]).GetPieceTree().CreateSnapshot("B");

        Assert.Equal("Bab", snapshot.Read());
        Assert.Equal("cd", snapshot.Read());
        Assert.Null(snapshot.Read());
    }
}
