// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (multiCursorModifier)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>The key that, held during a click, adds a cursor (Monaco: <c>multiCursorModifier</c>: <c>altKey</c>, <c>ctrlKey</c>, <c>metaKey</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum MultiCursorModifier
{
    /// <summary>The Alt key (Monaco's default).</summary>
    Alt = 0,

    /// <summary>The Ctrl key (Monaco: <c>ctrlCmd</c>, which is <c>ctrlKey</c> on Windows).</summary>
    CtrlCmd = 1,

    /// <summary>The Windows key.</summary>
    Meta = 2,
}
