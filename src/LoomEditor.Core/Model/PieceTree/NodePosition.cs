// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>The location of an offset inside the tree.</summary>
/// <param name="Node">Node that contains the offset.</param>
/// <param name="Remainder">Offset inside that node's piece.</param>
/// <param name="NodeStartOffset">Document offset of the first character of the node.</param>
/// <remarks>Monaco: <c>NodePosition</c>. Threading: immutable; the referenced node is only safe to read while the tree is not mutated.</remarks>
internal readonly record struct NodePosition(TreeNode Node, int Remainder, int NodeStartOffset);
