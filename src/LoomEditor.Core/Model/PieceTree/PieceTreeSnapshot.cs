// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// Read-only snapshot of a piece tree: the list of pieces is copied at creation, and pieces are immutable, so later
/// edits of the tree do not change what the snapshot returns.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeSnapshot</c>.</para>
/// <para>
/// Threading: the snapshot itself is a cursor (not thread-safe). Reading the content goes through the owning tree,
/// whose buffers are append-only, but the tree must not be edited concurrently with a read.
/// </para>
/// </remarks>
internal sealed class PieceTreeSnapshot : ITextSnapshot
{
    private readonly List<Piece> _pieces = [];
    private readonly PieceTreeBase _tree;
    private readonly string _bom;
    private int _index;

    /// <summary>Captures the pieces of <paramref name="tree"/>.</summary>
    /// <param name="tree">The tree to snapshot.</param>
    /// <param name="bom">The BOM returned in front of the first chunk.</param>
    public PieceTreeSnapshot(PieceTreeBase tree, string bom)
    {
        _tree = tree;
        _bom = bom;
        if (!ReferenceEquals(tree.Tree.Root, tree.Tree.Sentinel))
        {
            tree.Iterate(tree.Tree.Root, node =>
            {
                if (!ReferenceEquals(node, tree.Tree.Sentinel))
                {
                    _pieces.Add(node.Piece);
                }

                return true;
            });
        }
    }

    /// <inheritdoc />
    public string? Read()
    {
        if (_pieces.Count == 0)
        {
            if (_index == 0)
            {
                _index++;
                return _bom;
            }

            return null;
        }

        if (_index > _pieces.Count - 1)
        {
            return null;
        }

        if (_index == 0)
        {
            return _bom + _tree.GetPieceContent(_pieces[_index++]);
        }

        return _tree.GetPieceContent(_pieces[_index++]);
    }
}
