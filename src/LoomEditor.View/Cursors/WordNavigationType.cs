// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>How a word move stops (Monaco: <c>WordNavigationType</c>).</summary>
internal enum WordNavigationType
{
    WordStart = 0,
    WordStartFast = 1,
    WordEnd = 2,

    /// <summary>Respect Chrome's definition of a word (tuned to what NVDA expects).</summary>
    WordAccessibility = 3,
}
