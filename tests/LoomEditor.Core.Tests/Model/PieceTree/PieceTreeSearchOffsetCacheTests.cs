// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'search offset cache')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeSearchOffsetCacheTests
{
    private static void AssertLineBreakReplacementNotNeeded(string initial, int offset)
    {
        var m = new MirroredPieceTree(initial);
        m.Insert(offset, "def\nabc");

        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        PieceTreeAssert.LinesContent(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }

    // Monaco: 'render white space exception'
    [Fact]
    public void RenderWhiteSpaceException()
    {
        var m = new MirroredPieceTree("class Name{\n\t\n\t\t\tget() {\n\n\t\t\t}\n\t\t}");

        m.Insert(12, "s");
        m.Insert(13, "e");
        m.Insert(14, "t");
        m.Insert(15, "()");
        m.Delete(16, 1);
        m.Insert(17, "()");
        m.Delete(18, 1);
        m.Insert(18, "}");
        m.Insert(12, "\n");
        m.Delete(12, 1);
        m.Delete(18, 1);
        m.Insert(18, "}");
        m.Delete(17, 2);
        m.Delete(16, 1);
        m.Insert(16, ")");
        m.Delete(15, 2);

        m.AssertContent();
    }

    // Monaco: 'Line breaks replacement is not necessary when EOL is normalized'
    [Fact]
    public void LineBreaksReplacementNotNecessaryWhenEolNormalized()
    {
        AssertLineBreakReplacementNotNeeded("abc", 3);
    }

    // Monaco: 'Line breaks replacement is not necessary when EOL is normalized 2'
    [Fact]
    public void LineBreaksReplacementNotNecessaryWhenEolNormalized2()
    {
        AssertLineBreakReplacementNotNeeded("abc\n", 4);
    }

    // Monaco: 'Line breaks replacement is not necessary when EOL is normalized 3'
    [Fact]
    public void LineBreaksReplacementNotNecessaryWhenEolNormalized3()
    {
        AssertLineBreakReplacementNotNeeded("abc\n", 2);
    }

    // Monaco: 'Line breaks replacement is not necessary when EOL is normalized 4'
    [Fact]
    public void LineBreaksReplacementNotNecessaryWhenEolNormalized4()
    {
        AssertLineBreakReplacementNotNeeded("abc\n", 3);
    }
}
