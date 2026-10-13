// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>A position inside a <see cref="StringBuffer"/>.</summary>
/// <param name="Line">0-based index in the buffer's line starts.</param>
/// <param name="Column">Offset from the start of that line.</param>
/// <remarks>Monaco: <c>BufferCursor</c>. Threading: immutable value type; thread-safe.</remarks>
internal readonly record struct BufferCursor(int Line, int Column);
