// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Position of the overview ruler. Monaco: <c>OverviewRulerPosition</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record OverviewRulerPosition(int Width, int Height, int Top, int Right);

/// <summary>
/// The horizontal layout of the editor: glyph margin, line numbers, line decorations, content, scrollbars.
/// Monaco: <c>EditorLayoutInfo</c>.
/// </summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not ported: <c>isWordWrapMinified</c> (accessibility, phase 7); <c>minimap</c> arrives with P6-41.
/// </remarks>
internal sealed record EditorLayoutInfo(
    int Width,
    int Height,
    int GlyphMarginLeft,
    int GlyphMarginWidth,
    int GlyphMarginDecorationLaneCount,
    int LineNumbersLeft,
    int LineNumbersWidth,
    int DecorationsLeft,
    int DecorationsWidth,
    int ContentLeft,
    int ContentWidth,
    int ViewportColumn,
    bool IsViewportWrapping,
    int WrappingColumn,
    int VerticalScrollbarWidth,
    int HorizontalScrollbarHeight,
    OverviewRulerPosition OverviewRuler);
