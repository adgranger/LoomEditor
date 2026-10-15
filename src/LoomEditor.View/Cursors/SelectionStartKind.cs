// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>What the selection start of a cursor stands for: a position, a whole word or a whole line (Monaco: <c>SelectionStartKind</c>).</summary>
internal enum SelectionStartKind
{
    Simple,
    Word,
    Line,
}
