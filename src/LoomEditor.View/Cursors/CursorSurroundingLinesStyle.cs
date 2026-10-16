// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (cursorSurroundingLinesStyle)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>When <see cref="EditorCursorOptions.CursorSurroundingLines"/> is enforced.</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum CursorSurroundingLinesStyle
{
    /// <summary>Only when the cursor moves through the keyboard or the API (not the mouse), and not for a minimal reveal.</summary>
    Default = 0,

    /// <summary>Always.</summary>
    All = 1,
}
