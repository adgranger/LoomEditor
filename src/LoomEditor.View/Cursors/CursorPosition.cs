// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorMoveOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>A position and the visible columns the cursor still wants to move right (Monaco: <c>CursorPosition</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct CursorPosition(int LineNumber, int Column, int LeftoverVisibleColumns);
