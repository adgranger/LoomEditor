// Ported from microsoft/vscode: src/vs/editor/common/inputMode.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>Whether typing inserts or overwrites (Monaco: <c>InputMode</c>'s <c>'insert' | 'overtype'</c>).</summary>
/// <remarks>Per editor (<c>EditorContext.InputMode</c>); Monaco keeps one global value.</remarks>
public enum EditorInputMode
{
    /// <summary>A typed character is inserted at the caret (default).</summary>
    Insert,

    /// <summary>A typed character replaces the character after the caret.</summary>
    Overtype,
}
