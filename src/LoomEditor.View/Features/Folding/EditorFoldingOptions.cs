// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (folding, foldingHighlight, foldingMaximumRegions, unfoldOnClickAfterEndOfLine, showFoldingControls)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

/// <summary>The folding options of the editor.</summary>
/// <param name="Folding">Enable folding.</param>
/// <param name="ShowFoldingControls">When the folding controls of the gutter are shown.</param>
/// <param name="FoldingHighlight">Highlight the folded ranges.</param>
/// <param name="FoldingMaximumRegions">The maximum number of foldable regions (Monaco clamps it to 10…65000).</param>
/// <param name="UnfoldOnClickAfterEndOfLine">Clicking the empty content after a folded line unfolds it.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record EditorFoldingOptions(
    bool Folding = true,
    ShowFoldingControls ShowFoldingControls = ShowFoldingControls.Mouseover,
    bool FoldingHighlight = true,
    int FoldingMaximumRegions = 5000,
    bool UnfoldOnClickAfterEndOfLine = false)
{
    /// <summary>VS Code's defaults.</summary>
    public static EditorFoldingOptions Default { get; } = new();
}
