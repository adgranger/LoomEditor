// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (selectedTextMatchMode, selectionHighlight*)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>How Ctrl+D and its siblings match the selected text (Monaco: <c>selectedTextMatchMode</c>).</summary>
internal enum SelectedTextMatchMode
{
    /// <summary>The whole word and match case toggles of the find widget decide (the default).</summary>
    FindOptions,

    /// <summary>Case sensitive, not whole word.</summary>
    CaseSensitive,

    /// <summary>Case insensitive, not whole word.</summary>
    CaseInsensitive,
}

/// <summary>The editor options the multi-cursor commands and the selection highlighter read (<c>selectionHighlightMaxLength</c>: 0 means no limit).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record MultiCursorOptions(
    SelectedTextMatchMode SelectedTextMatchMode = SelectedTextMatchMode.FindOptions,
    bool SelectionHighlight = true,
    int SelectionHighlightMaxLength = 200,
    bool SelectionHighlightMultiline = false)
{
    /// <summary>VS Code's defaults.</summary>
    public static MultiCursorOptions Default { get; } = new();
}
