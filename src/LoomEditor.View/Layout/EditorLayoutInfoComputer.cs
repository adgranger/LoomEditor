// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Measurements the layout depends on. Monaco: <c>EditorLayoutInfoComputerEnv</c>.</summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not ported: <c>memory</c>, <c>isDominatedByLongLines</c>,
/// <c>viewLineCount</c> (minimap and accessibility only).
/// </remarks>
internal sealed record EditorLayoutInfoComputerEnv(
    int OuterWidth,
    int OuterHeight,
    int LineHeight,
    int LineNumbersDigitCount,
    double TypicalHalfwidthCharacterWidth,
    double MaxDigitWidth,
    double PixelRatio,
    int GlyphMarginDecorationLaneCount);

/// <summary>Monaco: <c>EditorLayoutInfoComputer.computeLayout</c>.</summary>
/// <remarks>
/// Threading: stateless, safe from any thread. Behavior differences: the minimap is treated as disabled, word wrap
/// as <c>off</c>, and accessibility "dominated by long lines" is not ported (phase 6).
/// </remarks>
internal static class EditorLayoutInfoComputer
{
    private const int FoldingDecorationWidth = 16;

    /// <summary>Computes where the gutter ends and the text starts.</summary>
    public static EditorLayoutInfo ComputeLayout(EditorLayoutOptions options, EditorLayoutInfoComputerEnv env)
    {
        int outerWidth = env.OuterWidth;
        int outerHeight = env.OuterHeight;
        int lineHeight = env.LineHeight;
        double typicalHalfwidthCharacterWidth = env.TypicalHalfwidthCharacterWidth;
        double maxDigitWidth = env.MaxDigitWidth;

        int verticalScrollbarWidth = options.VerticalScrollbarSize;
        int horizontalScrollbarHeight = options.HorizontalScrollbarSize;

        int lineDecorationsWidth = options.LineDecorationsWidth;
        if (options.Folding && options.ShowFoldingDecoration)
        {
            lineDecorationsWidth += FoldingDecorationWidth;
        }

        int lineNumbersWidth = 0;
        if (options.LineNumbers)
        {
            int digitCount = Math.Max(env.LineNumbersDigitCount, options.LineNumbersMinChars);
            lineNumbersWidth = (int)Math.Round(digitCount * maxDigitWidth, MidpointRounding.AwayFromZero);
        }

        int glyphMarginWidth = 0;
        if (options.GlyphMargin)
        {
            glyphMarginWidth = lineHeight * env.GlyphMarginDecorationLaneCount;
        }

        int glyphMarginLeft = 0;
        int lineNumbersLeft = glyphMarginLeft + glyphMarginWidth;
        int decorationsLeft = lineNumbersLeft + lineNumbersWidth;
        int contentLeft = decorationsLeft + lineDecorationsWidth;

        int contentWidth = outerWidth - glyphMarginWidth - lineNumbersWidth - lineDecorationsWidth;

        // (leaving 2px for the cursor to have space after the last character)
        int viewportColumn = Math.Max(
            1, (int)Math.Floor((contentWidth - verticalScrollbarWidth - 2) / typicalHalfwidthCharacterWidth));

        int verticalArrowSize = options.VerticalHasArrows ? options.ArrowSize : 0;

        return new EditorLayoutInfo(
            Width: outerWidth,
            Height: outerHeight,
            GlyphMarginLeft: glyphMarginLeft,
            GlyphMarginWidth: glyphMarginWidth,
            GlyphMarginDecorationLaneCount: env.GlyphMarginDecorationLaneCount,
            LineNumbersLeft: lineNumbersLeft,
            LineNumbersWidth: lineNumbersWidth,
            DecorationsLeft: decorationsLeft,
            DecorationsWidth: lineDecorationsWidth,
            ContentLeft: contentLeft,
            ContentWidth: contentWidth,
            ViewportColumn: viewportColumn,
            VerticalScrollbarWidth: verticalScrollbarWidth,
            HorizontalScrollbarHeight: horizontalScrollbarHeight,
            OverviewRuler: new OverviewRulerPosition(
                Width: verticalScrollbarWidth,
                Height: outerHeight - 2 * verticalArrowSize,
                Top: verticalArrowSize,
                Right: 0));
    }
}
