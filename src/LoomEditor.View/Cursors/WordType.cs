// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>The kind of a word found on a line (Monaco: <c>WordType</c>).</summary>
internal enum WordType
{
    None = 0,
    Regular = 1,
    Separator = 2,
}
