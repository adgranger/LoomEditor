// Ported from microsoft/vscode: src/vs/editor/contrib/toggleTabFocusMode/browser/toggleTabFocusMode.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.TabFocus;

/// <summary>Ids of the commands exposed by <see cref="TabFocusFeature"/>. Same ids as in Monaco.</summary>
internal static class TabFocusCommands
{
    /// <summary>Toggles whether Tab and Shift+Tab move the keyboard focus out of the editor instead of indenting.</summary>
    public const string ToggleTabFocusMode = "editor.action.toggleTabFocusMode";
}
