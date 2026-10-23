// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollActions.ts (ToggleStickyScroll)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>The ids of the sticky scroll commands (Monaco: the <c>id</c> of each sticky scroll action).</summary>
/// <remarks>
/// Threading: constants only. Not ported: the focus and navigation commands (<c>focusStickyScroll</c>,
/// <c>selectNextStickyScrollLine</c>, ...), a follow-up.
/// </remarks>
internal static class StickyScrollCommands
{
    /// <summary>Monaco: <c>ToggleStickyScroll</c> (no default keybinding).</summary>
    public const string ToggleStickyScroll = "editor.action.toggleStickyScroll";
}
