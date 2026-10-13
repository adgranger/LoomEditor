// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (suite 'offset 2 position')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeOffsetToPositionTests
{
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

        PieceTreeAssert.LineStarts(m.Text, m.Tree);
        TreeInvariants.AssertValid(m.Tree.Tree);
    }
}
