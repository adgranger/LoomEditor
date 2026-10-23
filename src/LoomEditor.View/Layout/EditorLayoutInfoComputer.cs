// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Measurements the layout depends on. Monaco: <c>EditorLayoutInfoComputerEnv</c>.</summary>
/// <remarks>
/// Threading: immutable, safe from any thread, except that <c>Memory</c> is mutated by the layout computation.
/// <c>scrollBeyondLastLine</c> and the padding are read from the
/// environment because <see cref="EditorLayoutOptions"/> has neither.
/// </remarks>
internal sealed record EditorLayoutInfoComputerEnv(
    int OuterWidth,
    int OuterHeight,
    int LineHeight,
    int LineNumbersDigitCount,
    double TypicalHalfwidthCharacterWidth,
    double MaxDigitWidth,
    double PixelRatio,
    int GlyphMarginDecorationLaneCount,
    int ViewLineCount = 1,
    ComputeOptionsMemory? Memory = null,
    bool ScrollBeyondLastLine = false,
    int PaddingTop = 0,
    int PaddingBottom = 0,
    bool IsDominatedByLongLines = false);

/// <summary>Monaco: <c>EditorLayoutInfoComputer.computeLayout</c>.</summary>
/// <remarks>
/// Threading: stateless, safe from any thread. Behavior differences: accessibility is ported except
/// <c>isWordWrapMinified</c>, which nothing reads.
/// </remarks>
internal static partial class EditorLayoutInfoComputer
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

        int remainingWidth = outerWidth - glyphMarginWidth - lineNumbersWidth - lineDecorationsWidth;

        bool isViewportWrapping = false;
        int wrappingColumn = -1;
        if (options.Accessibility.IsEnabled && env.IsDominatedByLongLines)
        {
            // screen reader mode: wrap long-line models at the viewport whatever the setting is
            isViewportWrapping = true;
        }
        else if (options.Wrapping.WordWrap is WordWrap.On or WordWrap.Bounded)
        {
            isViewportWrapping = true;
        }
        else if (options.Wrapping.WordWrap == WordWrap.WordWrapColumn)
        {
            wrappingColumn = options.Wrapping.WordWrapColumn;
        }

        EditorMinimapLayoutInfo minimapLayout = ComputeMinimapLayout(
            new MinimapLayoutInput(
                outerWidth,
                outerHeight,
                lineHeight,
                typicalHalfwidthCharacterWidth,
                env.PixelRatio,
                env.ScrollBeyondLastLine,
                env.PaddingTop,
                env.PaddingBottom,
                options.Minimap,
                verticalScrollbarWidth,
                env.ViewLineCount,
                remainingWidth,
                isViewportWrapping),
            env.Memory ?? new ComputeOptionsMemory());

        if (minimapLayout.RenderMinimap != RenderMinimap.None && minimapLayout.MinimapLeft == 0)
        {
            // the minimap is rendered to the left, so move everything to the right
            glyphMarginLeft += minimapLayout.MinimapWidth;
            lineNumbersLeft += minimapLayout.MinimapWidth;
            decorationsLeft += minimapLayout.MinimapWidth;
            contentLeft += minimapLayout.MinimapWidth;
        }

        int contentWidth = remainingWidth - minimapLayout.MinimapWidth;

        // (leaving 2px for the cursor to have space after the last character)
        int viewportColumn = Math.Max(
            1, (int)Math.Floor((contentWidth - verticalScrollbarWidth - 2) / typicalHalfwidthCharacterWidth));

        if (isViewportWrapping)
        {
            wrappingColumn = Math.Max(1, viewportColumn - (options.Wrapping.WordWrapIndicator ? 1 : 0));
            if (options.Wrapping.WordWrap == WordWrap.Bounded)
            {
                wrappingColumn = Math.Min(wrappingColumn, options.Wrapping.WordWrapColumn);
            }
        }

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
            Minimap: minimapLayout,
            ViewportColumn: viewportColumn,
            IsViewportWrapping: isViewportWrapping,
            WrappingColumn: wrappingColumn,
            VerticalScrollbarWidth: verticalScrollbarWidth,
            HorizontalScrollbarHeight: horizontalScrollbarHeight,
            OverviewRuler: new OverviewRulerPosition(
                Width: verticalScrollbarWidth,
                Height: outerHeight - 2 * verticalArrowSize,
                Top: verticalArrowSize,
                Right: 0));
    }
}
