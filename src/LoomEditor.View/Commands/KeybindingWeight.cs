// Ported from microsoft/vscode: src/vs/platform/keybinding/common/keybindingsRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>Monaco's <c>KeybindingWeight</c>: of two rules on the same key, the heavier one wins.</summary>
/// <remarks>Threading: constants; thread-safe.</remarks>
internal static class KeybindingWeight
{
    /// <summary>The core commands of the editor (<c>coreCommands.ts</c>, <c>undo</c>, <c>redo</c>, select all).</summary>
    public const int EditorCore = 0;

    /// <summary>The editor contributions (word operations, multi-cursor, clipboard).</summary>
    public const int EditorContrib = 100;
}
