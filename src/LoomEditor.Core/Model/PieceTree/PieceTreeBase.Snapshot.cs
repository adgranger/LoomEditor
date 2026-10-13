// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

// Snapshots and equality.
internal sealed partial class PieceTreeBase
{
    /// <summary>Creates a read-only snapshot of the current content (Monaco: <c>createSnapshot</c>).</summary>
    /// <param name="bom">The BOM returned in front of the first chunk.</param>
    public ITextSnapshot CreateSnapshot(string bom) => new PieceTreeSnapshot(this, bom);

    /// <summary>True when both trees hold the same text, whatever their pieces (Monaco: <c>equal</c>).</summary>
    /// <param name="other">The tree to compare with.</param>
    public bool Equal(PieceTreeBase other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (GetLength() != other.GetLength())
        {
            return false;
        }

        if (GetLineCount() != other.GetLineCount())
        {
            return false;
        }

        int offset = 0;
        return Iterate(_tree.Root, node =>
        {
            if (ReferenceEquals(node, _tree.Sentinel))
            {
                return true;
            }

            string str = GetNodeContent(node);
            int len = str.Length;
            NodePosition startPosition = other.NodeAt(offset);
            NodePosition endPosition = other.NodeAt(offset + len);
            string val = other.GetValueInRange2(startPosition, endPosition);

            offset += len;
            return str == val;
        });
    }
}
