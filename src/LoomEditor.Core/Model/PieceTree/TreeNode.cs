// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>A node of the piece tree's red-black tree: one <see cref="Piece"/> plus augmented metadata.</summary>
/// <remarks>
/// Monaco: <c>TreeNode</c>. <c>next()</c>/<c>prev()</c> live on <see cref="RedBlackTree"/> because they need the
/// tree's sentinel. Threading: not thread-safe; owned by one <see cref="RedBlackTree"/>.
/// </remarks>
internal sealed class TreeNode
{
    public TreeNode(Piece piece, NodeColor color)
    {
        Piece = piece;
        Color = color;
        Parent = this;
        Left = this;
        Right = this;
    }

    public TreeNode Parent { get; set; }

    public TreeNode Left { get; set; }

    public TreeNode Right { get; set; }

    public NodeColor Color { get; set; }

    public Piece Piece { get; set; }

    /// <summary>Size of the left subtree (Monaco: <c>size_left</c>; not in-order).</summary>
    public int SizeLeft { get; set; }

    /// <summary>Line feed count of the left subtree (Monaco: <c>lf_left</c>; not in-order).</summary>
    public int LineFeedCountLeft { get; set; }

    /// <summary>True once the node was removed from its tree.</summary>
    /// <remarks>Difference from Monaco, which tests <c>parent === null</c>.</remarks>
    public bool IsDetached { get; private set; }

    /// <summary>Marks the node as removed and points its links to <paramref name="sentinel"/>.</summary>
    public void Detach(TreeNode sentinel)
    {
        IsDetached = true;
        Parent = sentinel;
        Left = sentinel;
        Right = sentinel;
    }
}
