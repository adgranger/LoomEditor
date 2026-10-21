// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts (selection highlight options)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>The decoration options of the selection highlight (the rest of <c>highlightDecorations.ts</c> comes with the word highlighter).</summary>
/// <remarks>
/// Behavior difference: the <c>overviewRuler</c> and <c>minimap</c> members are dropped (phase 6), so both Monaco options
/// are the same here apart from their description. Threading: immutable; thread-safe.
/// </remarks>
internal static class SelectionHighlightDecorations
{
    private static readonly ModelDecorationOptions SelectionHighlightOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "selection-highlight-overview",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "selectionHighlight",
    });

    private static readonly ModelDecorationOptions SelectionHighlightNoOverviewOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "selection-highlight",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "selectionHighlight",
    });

    /// <summary>Monaco: <c>getSelectionHighlightDecorationOptions</c>.</summary>
    public static ModelDecorationOptions GetSelectionHighlightDecorationOptions(bool hasSemanticHighlights) =>
        // Show in overviewRuler only if model has no semantic highlighting
        hasSemanticHighlights ? SelectionHighlightNoOverviewOptions : SelectionHighlightOptions;
}
