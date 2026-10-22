// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

internal static partial class EditorLayoutInfoComputer
{
    private const int MinimapGutterWidth = 8;

    /// <summary>The line counts the <c>fill</c> and <c>fit</c> minimap sizes derive from. Monaco: the result of <c>computeContainedMinimapLineCount</c>.</summary>
    public readonly record struct ContainedMinimapLineCount(
        double TypicalViewportLineCount,
        int ExtraLinesBeforeFirstLine,
        double ExtraLinesBeyondLastLine,
        double DesiredRatio,
        double MinimapLineCount);

    /// <summary>Monaco: <c>computeContainedMinimapLineCount</c>.</summary>
    public static ContainedMinimapLineCount ComputeContainedMinimapLineCount(
        int viewLineCount,
        bool scrollBeyondLastLine,
        int paddingTop,
        int paddingBottom,
        int height,
        int lineHeight,
        double pixelRatio)
    {
        double typicalViewportLineCount = (double)height / lineHeight;
        int extraLinesBeforeFirstLine = (int)Math.Floor((double)paddingTop / lineHeight);
        double extraLinesBeyondLastLine = Math.Floor((double)paddingBottom / lineHeight);
        if (scrollBeyondLastLine)
        {
            extraLinesBeyondLastLine = Math.Max(extraLinesBeyondLastLine, typicalViewportLineCount - 1);
        }

        double desiredRatio = (extraLinesBeforeFirstLine + viewLineCount + extraLinesBeyondLastLine) / (pixelRatio * height);
        double minimapLineCount = Math.Floor(viewLineCount / desiredRatio);
        return new ContainedMinimapLineCount(
            typicalViewportLineCount, extraLinesBeforeFirstLine, extraLinesBeyondLastLine, desiredRatio, minimapLineCount);
    }

    // Monaco: _computeMinimapLayout
    private static EditorMinimapLayoutInfo ComputeMinimapLayout(MinimapLayoutInput input, ComputeOptionsMemory memory)
    {
        int outerWidth = input.OuterWidth;
        int outerHeight = input.OuterHeight;
        double pixelRatio = input.PixelRatio;

        if (!input.Minimap.Enabled)
        {
            return new EditorMinimapLayoutInfo(
                RenderMinimap.None,
                MinimapLeft: 0,
                MinimapWidth: 0,
                MinimapHeightIsEditorHeight: false,
                MinimapIsSampling: false,
                MinimapScale: 1,
                MinimapLineHeight: 1,
                MinimapCanvasInnerWidth: 0,
                MinimapCanvasInnerHeight: (int)Math.Floor(pixelRatio * outerHeight),
                MinimapCanvasOuterWidth: 0,
                MinimapCanvasOuterHeight: outerHeight);
        }

        // Can use memory if only the `viewLineCount` and `remainingWidth` have changed
        MinimapLayoutInput? stable = memory.StableMinimapLayoutInput;
        bool couldUseMemory = stable is not null
            // outerWidth, viewLineCount and remainingWidth are intentionally omitted
            && input.OuterHeight == stable.OuterHeight
            && input.LineHeight == stable.LineHeight
            && input.TypicalHalfwidthCharacterWidth == stable.TypicalHalfwidthCharacterWidth
            && input.PixelRatio == stable.PixelRatio
            && input.ScrollBeyondLastLine == stable.ScrollBeyondLastLine
            && input.PaddingTop == stable.PaddingTop
            && input.PaddingBottom == stable.PaddingBottom
            && input.Minimap.Enabled == stable.Minimap.Enabled
            && input.Minimap.Side == stable.Minimap.Side
            && input.Minimap.Size == stable.Minimap.Size
            && input.Minimap.ShowSlider == stable.Minimap.ShowSlider
            && input.Minimap.RenderCharacters == stable.Minimap.RenderCharacters
            && input.Minimap.MaxColumn == stable.Minimap.MaxColumn
            && input.Minimap.Scale == stable.Minimap.Scale
            && input.VerticalScrollbarWidth == stable.VerticalScrollbarWidth
            && input.IsViewportWrapping == stable.IsViewportWrapping;

        int lineHeight = input.LineHeight;
        bool minimapRenderCharacters = input.Minimap.RenderCharacters;
        double minimapScale = pixelRatio >= 2
            ? Math.Round(input.Minimap.Scale * 2.0, MidpointRounding.AwayFromZero)
            : input.Minimap.Scale;
        int verticalScrollbarWidth = input.VerticalScrollbarWidth;
        int viewLineCount = input.ViewLineCount;
        int remainingWidth = input.RemainingWidth;
        bool isViewportWrapping = input.IsViewportWrapping;
        MinimapSize minimapSize = input.Minimap.Size;

        int baseCharHeight = minimapRenderCharacters ? 2 : 3;
        double minimapCanvasInnerHeight = Math.Floor(pixelRatio * outerHeight);
        double minimapCanvasOuterHeight = minimapCanvasInnerHeight / pixelRatio;
        bool minimapHeightIsEditorHeight = false;
        bool minimapIsSampling = false;
        double minimapLineHeight = baseCharHeight * minimapScale;
        double minimapCharWidth = minimapScale / pixelRatio;
        double minimapWidthMultiplier = 1;

        if (minimapSize is MinimapSize.Fill or MinimapSize.Fit)
        {
            ContainedMinimapLineCount contained = ComputeContainedMinimapLineCount(
                viewLineCount, input.ScrollBeyondLastLine, input.PaddingTop, input.PaddingBottom, outerHeight, lineHeight, pixelRatio);

            // ratio is intentionally not part of the layout to avoid the layout changing all the time when doing sampling
            double ratio = viewLineCount / contained.MinimapLineCount;

            if (ratio > 1)
            {
                minimapHeightIsEditorHeight = true;
                minimapIsSampling = true;
                minimapScale = 1;
                minimapLineHeight = 1;
                minimapCharWidth = minimapScale / pixelRatio;
            }
            else
            {
                bool fitBecomesFill = false;
                double maxMinimapScale = minimapScale + 1;
                double totalLines = contained.ExtraLinesBeforeFirstLine + viewLineCount + contained.ExtraLinesBeyondLastLine;

                if (minimapSize == MinimapSize.Fit)
                {
                    double effectiveMinimapHeight = Math.Ceiling(totalLines * minimapLineHeight);
                    if (isViewportWrapping && couldUseMemory && remainingWidth <= memory.StableFitRemainingWidth)
                    {
                        // There is a loop when using `fit` and viewport wrapping:
                        // - view line count impacts minimap layout
                        // - minimap layout impacts viewport width
                        // - viewport width impacts view line count
                        // To break the loop, once we go to a smaller minimap scale, we try to stick with it.
                        fitBecomesFill = true;
                        maxMinimapScale = memory.StableFitMaxMinimapScale;
                    }
                    else
                    {
                        fitBecomesFill = effectiveMinimapHeight > minimapCanvasInnerHeight;
                    }
                }

                if (minimapSize == MinimapSize.Fill || fitBecomesFill)
                {
                    minimapHeightIsEditorHeight = true;
                    double configuredMinimapScale = minimapScale;
                    minimapLineHeight = Math.Min(lineHeight * pixelRatio, Math.Max(1, Math.Floor(1 / contained.DesiredRatio)));
                    if (isViewportWrapping && couldUseMemory && remainingWidth <= memory.StableFitRemainingWidth)
                    {
                        // Same loop as above with `fill`.
                        maxMinimapScale = memory.StableFitMaxMinimapScale;
                    }

                    minimapScale = Math.Min(maxMinimapScale, Math.Max(1, Math.Floor(minimapLineHeight / baseCharHeight)));
                    if (minimapScale > configuredMinimapScale)
                    {
                        minimapWidthMultiplier = Math.Min(2, minimapScale / configuredMinimapScale);
                    }

                    minimapCharWidth = minimapScale / pixelRatio / minimapWidthMultiplier;
                    minimapCanvasInnerHeight = Math.Ceiling(Math.Max(contained.TypicalViewportLineCount, totalLines) * minimapLineHeight);
                    if (isViewportWrapping)
                    {
                        // remember for next time
                        memory.StableMinimapLayoutInput = input;
                        memory.StableFitRemainingWidth = remainingWidth;
                        memory.StableFitMaxMinimapScale = minimapScale;
                    }
                    else
                    {
                        memory.StableMinimapLayoutInput = null;
                        memory.StableFitRemainingWidth = 0;
                    }
                }
            }
        }

        // minimapWidth = ((remainingWidth - verticalScrollbarWidth - 2) * minimapCharWidth) / (typicalHalfwidthCharacterWidth + minimapCharWidth)
        // (derived in Monaco from contentWidth = remainingWidth - minimapWidth)
        int minimapMaxWidth = (int)Math.Floor(input.Minimap.MaxColumn * minimapCharWidth);
        int minimapWidth = (int)Math.Min(
            minimapMaxWidth,
            Math.Max(0, Math.Floor((remainingWidth - verticalScrollbarWidth - 2) * minimapCharWidth / (input.TypicalHalfwidthCharacterWidth + minimapCharWidth)))
                + MinimapGutterWidth);

        double minimapCanvasInnerWidth = Math.Floor(pixelRatio * minimapWidth);
        double minimapCanvasOuterWidth = minimapCanvasInnerWidth / pixelRatio;
        minimapCanvasInnerWidth = Math.Floor(minimapCanvasInnerWidth * minimapWidthMultiplier);

        RenderMinimap renderMinimap = minimapRenderCharacters ? RenderMinimap.Text : RenderMinimap.Blocks;
        int minimapLeft = input.Minimap.Side == MinimapSide.Left ? 0 : outerWidth - minimapWidth - verticalScrollbarWidth;

        return new EditorMinimapLayoutInfo(
            renderMinimap,
            minimapLeft,
            minimapWidth,
            minimapHeightIsEditorHeight,
            minimapIsSampling,
            minimapScale,
            minimapLineHeight,
            (int)minimapCanvasInnerWidth,
            (int)minimapCanvasInnerHeight,
            minimapCanvasOuterWidth,
            minimapCanvasOuterHeight);
    }
}
