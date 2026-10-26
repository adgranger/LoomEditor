// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (snippetSuggestions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>Where items of kind <c>Snippet</c> go in the list (Monaco: <c>snippetSuggestions</c>; <c>none</c> is not ported: there are no snippets in phase 8).</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
internal enum SnippetSuggestions
{
    /// <summary>Snippets first.</summary>
    Top,

    /// <summary>Snippets last.</summary>
    Bottom,

    /// <summary>Snippets sorted with the other items.</summary>
    Inline,
}
