// Not ported from Monaco: LoomEditor helper replacing Monaco's repeated `str = str.substring(...) + ... + str.substring(...)` lines.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

/// <summary>A piece tree plus a plain string receiving the same edits, as Monaco's recorded tests keep in <c>str</c>.</summary>
internal sealed class MirroredPieceTree
{
    public MirroredPieceTree(string initial)
    {
        Text = initial;
        Tree = TestTextBuffers.Create([initial]).GetPieceTree();
    }

    public PieceTreeBase Tree { get; }

    /// <summary>The expected content (Monaco: <c>str</c>).</summary>
    public string Text { get; private set; }

    public void Insert(int offset, string value)
    {
        Tree.Insert(offset, value);
        Text = Text.Insert(offset, value);
    }

    public void Delete(int offset, int count)
    {
        Tree.Delete(offset, count);
        Text = Text.Remove(offset, count);
    }

    public void AssertContent() => Assert.Equal(Text, Tree.GetLinesRawContent());
}
