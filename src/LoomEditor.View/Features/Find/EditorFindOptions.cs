// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (IEditorFindOptions, EditorFind)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>When the find box is seeded from the editor selection (Monaco: <c>seedSearchStringFromSelection</c>).</summary>
internal enum SeedSearchStringMode
{
    /// <summary>Never.</summary>
    Never,

    /// <summary>Always (the word at the cursor, or the selection).</summary>
    Always,

    /// <summary>Only from a non-empty selection.</summary>
    Selection,
}

/// <summary>When "find in selection" is turned on automatically (Monaco: <c>autoFindInSelection</c>).</summary>
internal enum AutoFindInSelectionMode
{
    /// <summary>Never.</summary>
    Never,

    /// <summary>Always.</summary>
    Always,

    /// <summary>When the selection spans several lines.</summary>
    Multiline,
}

/// <summary>The <c>editor.find</c> options.</summary>
/// <remarks>
/// Not ported: <c>globalFindClipboard</c> (macOS only), <c>addExtraSpaceOnTop</c> (needs view zones), <c>history</c> and
/// <c>replaceHistory</c> (no search history). Threading: immutable; thread-safe.
/// </remarks>
internal sealed record EditorFindOptions(
    bool CursorMoveOnType = true,
    bool FindOnType = true,
    SeedSearchStringMode SeedSearchStringFromSelection = SeedSearchStringMode.Always,
    AutoFindInSelectionMode AutoFindInSelection = AutoFindInSelectionMode.Never,
    bool Loop = true,
    bool CloseOnResult = false)
{
    /// <summary>VS Code's defaults.</summary>
    public static EditorFindOptions Default { get; } = new();
}
