// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>Where the cursor may stop around injected text (Monaco: <c>InjectedTextCursorStops</c>).</summary>
internal enum InjectedTextCursorStops
{
    /// <summary>Both sides.</summary>
    Both,

    /// <summary>Only on the right of the injected text.</summary>
    Right,

    /// <summary>Only on the left of the injected text.</summary>
    Left,

    /// <summary>Neither side.</summary>
    None,
}
