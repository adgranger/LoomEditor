// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>A contiguous slice of one of the piece tree's buffers.</summary>
/// <param name="BufferIndex">Index of the buffer the piece reads from.</param>
/// <param name="Start">Start of the slice in that buffer.</param>
/// <param name="End">End of the slice in that buffer.</param>
/// <param name="LineFeedCount">Number of line breaks in the slice (Monaco: <c>lineFeedCnt</c>).</param>
/// <param name="Length">Length of the slice in characters.</param>
/// <remarks>
/// Monaco: <c>Piece</c>. Threading: immutable and a reference type on purpose: tree nodes swap whole pieces and
/// snapshots share them, so they can be read from any thread.
/// </remarks>
internal sealed record Piece(int BufferIndex, BufferCursor Start, BufferCursor End, int LineFeedCount, int Length);
