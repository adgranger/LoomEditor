// Ported from microsoft/vscode: src/vs/editor/common/cursorEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>The reason the cursor changed its position (Monaco: <c>CursorChangeReason</c>).</summary>
internal enum CursorChangeReason
{
    /// <summary>Unknown or not set.</summary>
    NotSet = 0,

    /// <summary>A <c>TextModel.SetValue</c> was called.</summary>
    ContentFlush = 1,

    /// <summary>The model has been changed outside of this cursor and the cursor recovers its position from its tracked ranges.</summary>
    RecoverFromMarkers = 2,

    /// <summary>There was an explicit user gesture.</summary>
    Explicit = 3,

    /// <summary>There was a paste.</summary>
    Paste = 4,

    /// <summary>There was an undo.</summary>
    Undo = 5,

    /// <summary>There was a redo.</summary>
    Redo = 6,
}
