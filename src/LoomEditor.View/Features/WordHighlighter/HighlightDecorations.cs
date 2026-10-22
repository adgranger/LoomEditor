// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>The decoration options of the word highlights, by kind.</summary>
/// <remarks>
/// Not ported: the overview ruler and minimap options (phase 6) and the selection highlight options (find-based, step F).
/// Threading: immutable, safe from any thread.
/// </remarks>
internal static class HighlightDecorations
{
    private static readonly ModelDecorationOptions WriteOptions = Create("word-highlight-strong", "wordHighlightStrong");
    private static readonly ModelDecorationOptions TextOptions = Create("word-highlight-text", "wordHighlightText");
    private static readonly ModelDecorationOptions RegularOptions = Create("word-highlight", "wordHighlight");

    /// <summary>Monaco: <c>getHighlightDecorationOptions</c>.</summary>
    public static ModelDecorationOptions GetHighlightDecorationOptions(DocumentHighlightKind? kind) => kind switch
    {
        DocumentHighlightKind.Write => WriteOptions,
        DocumentHighlightKind.Text => TextOptions,
        _ => RegularOptions,
    };

    private static ModelDecorationOptions Create(string description, string className) =>
        ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = description,
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = className,
        });
}
