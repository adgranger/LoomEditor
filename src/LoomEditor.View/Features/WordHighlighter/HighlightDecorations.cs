// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>The decoration options of the word highlights, by kind.</summary>
/// <remarks>
/// The selection highlight options are in <c>SelectionHighlightDecorations</c>.
/// Threading: immutable, safe from any thread.
/// </remarks>
internal static class HighlightDecorations
{
    private static readonly ModelDecorationOptions WriteOptions = Create("word-highlight-strong", "wordHighlightStrong", "editorOverviewRuler.wordHighlightStrongForeground");
    private static readonly ModelDecorationOptions TextOptions = Create("word-highlight-text", "wordHighlightText", "editorOverviewRuler.wordHighlightTextForeground");
    private static readonly ModelDecorationOptions RegularOptions = Create("word-highlight", "wordHighlight", "editorOverviewRuler.wordHighlightForeground");

    /// <summary>Monaco: <c>getHighlightDecorationOptions</c>.</summary>
    public static ModelDecorationOptions GetHighlightDecorationOptions(DocumentHighlightKind? kind) => kind switch
    {
        DocumentHighlightKind.Write => WriteOptions,
        DocumentHighlightKind.Text => TextOptions,
        _ => RegularOptions,
    };

    private static ModelDecorationOptions Create(string description, string className, string rulerColorId) =>
        ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = description,
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = className,
            OverviewRuler = new ModelDecorationOverviewRulerOptions(DecorationColor.FromThemeColor(rulerColorId), DecorationColor.FromThemeColor(rulerColorId), OverviewRulerLane.Center),
            Minimap = new ModelDecorationMinimapOptions(DecorationColor.FromThemeColor("minimap.selectionOccurrenceHighlight"), null, MinimapPosition.Inline),
        });
}
