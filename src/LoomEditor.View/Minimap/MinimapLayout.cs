// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (MinimapLayout)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>Which lines the minimap renders and where its slider is. Monaco: <c>MinimapLayout</c>.</summary>
/// <remarks>
/// Threading: immutable; UI thread only.
/// Behavior differences: <c>Math.round</c> is <see cref="MidpointRounding.AwayFromZero"/> (differs from JavaScript only
/// for negative halves); <c>intersectWithViewport</c> returns a nullable tuple.
/// </remarks>
internal sealed class MinimapLayout
{
    private readonly double _computedSliderRatio;

    /// <summary>Builds a layout; use <see cref="Create"/>.</summary>
    public MinimapLayout(
        double scrollTop, double scrollHeight, bool sliderNeeded, double computedSliderRatio, double sliderTop,
        double sliderHeight, int topPaddingLineCount, int startLineNumber, int endLineNumber)
    {
        ScrollTop = scrollTop;
        ScrollHeight = scrollHeight;
        SliderNeeded = sliderNeeded;
        _computedSliderRatio = computedSliderRatio;
        SliderTop = sliderTop;
        SliderHeight = sliderHeight;
        TopPaddingLineCount = topPaddingLineCount;
        StartLineNumber = startLineNumber;
        EndLineNumber = endLineNumber;
    }

    /// <summary>The given editor scrollTop (input). Monaco: <c>scrollTop</c>.</summary>
    public double ScrollTop { get; }

    /// <summary>The given editor scrollHeight (input). Monaco: <c>scrollHeight</c>.</summary>
    public double ScrollHeight { get; }

    /// <summary>Monaco: <c>sliderNeeded</c>.</summary>
    public bool SliderNeeded { get; }

    /// <summary>Slider top (CSS px). Monaco: <c>sliderTop</c>.</summary>
    public double SliderTop { get; }

    /// <summary>Slider height (CSS px). Monaco: <c>sliderHeight</c>.</summary>
    public double SliderHeight { get; }

    /// <summary>Empty lines to reserve at the top of the minimap. Monaco: <c>topPaddingLineCount</c>.</summary>
    public int TopPaddingLineCount { get; }

    /// <summary>Minimap render start line number. Monaco: <c>startLineNumber</c>.</summary>
    public int StartLineNumber { get; }

    /// <summary>Minimap render end line number. Monaco: <c>endLineNumber</c>.</summary>
    public int EndLineNumber { get; }

    /// <summary>Computes a desired scroll position such that the slider moves by <paramref name="delta"/>. Monaco: <c>getDesiredScrollTopFromDelta</c>.</summary>
    public double GetDesiredScrollTopFromDelta(double delta) =>
        Math.Round(ScrollTop + (delta / _computedSliderRatio), MidpointRounding.AwayFromZero);

    /// <summary>Monaco: <c>getDesiredScrollTopFromTouchLocation</c>.</summary>
    public double GetDesiredScrollTopFromTouchLocation(double pageY) =>
        Math.Round((pageY - (SliderHeight / 2)) / _computedSliderRatio, MidpointRounding.AwayFromZero);

    /// <summary>Intersects a line range with <see cref="StartLineNumber"/> and <see cref="EndLineNumber"/>. Monaco: <c>intersectWithViewport</c>.</summary>
    public (int Start, int End)? IntersectWithViewport(Range range)
    {
        int startLineNumber = Math.Max(StartLineNumber, range.StartLineNumber);
        int endLineNumber = Math.Min(EndLineNumber, range.EndLineNumber);
        if (startLineNumber > endLineNumber)
        {
            // entirely outside minimap's viewport
            return null;
        }

        return (startLineNumber, endLineNumber);
    }

    /// <summary>Gets the inner minimap y coordinate for a line number. Monaco: <c>getYForLineNumber</c>.</summary>
    public double GetYForLineNumber(int lineNumber, double minimapLineHeight) =>
        (lineNumber - StartLineNumber + TopPaddingLineCount) * minimapLineHeight;

    /// <summary>Monaco: <c>MinimapLayout.create</c>.</summary>
    public static MinimapLayout Create(
        MinimapOptions options,
        int viewportStartLineNumber,
        int viewportEndLineNumber,
        double viewportStartLineNumberVerticalOffset,
        int viewportHeight,
        bool viewportContainsWhitespaceGaps,
        int lineCount,
        int realLineCount,
        double scrollTop,
        double scrollHeight,
        MinimapLayout? previousLayout)
    {
        double pixelRatio = options.PixelRatio;
        double minimapLineHeight = options.MinimapLineHeight;
        int minimapLinesFitting = (int)Math.Floor(options.CanvasInnerHeight / minimapLineHeight);
        int lineHeight = options.LineHeight;

        if (options.MinimapHeightIsEditorHeight)
        {
            double logicalScrollHeight = (realLineCount * options.LineHeight) + options.PaddingTop + options.PaddingBottom;
            if (options.ScrollBeyondLastLine)
            {
                logicalScrollHeight += Math.Max(0, viewportHeight - options.LineHeight - options.PaddingBottom);
            }

            double heightModeSliderHeight = Math.Max(1, Math.Floor(viewportHeight * (double)viewportHeight / logicalScrollHeight));
            double maxSliderTop = Math.Max(0, options.MinimapHeight - heightModeSliderHeight);
            // The slider can move from 0 to `maxSliderTop` in the same way `scrollTop` can move from 0 to
            // `scrollHeight` - `viewportHeight`.
            double sliderRatio = maxSliderTop / (scrollHeight - viewportHeight);
            double heightModeSliderTop = scrollTop * sliderRatio;
            int maxLinesFitting = (int)Math.Floor(options.CanvasInnerHeight / options.MinimapLineHeight);
            int paddingLineCount = (int)Math.Floor((double)options.PaddingTop / options.LineHeight);
            return new MinimapLayout(
                scrollTop, scrollHeight, maxSliderTop > 0, sliderRatio, heightModeSliderTop, heightModeSliderHeight,
                paddingLineCount, 1, Math.Min(lineCount, maxLinesFitting));
        }

        // We must first establish a desirable slider height.
        double sliderHeight;
        if (viewportContainsWhitespaceGaps && viewportEndLineNumber != lineCount)
        {
            // case b): there are whitespace gaps in the viewport; the slider height reflects the visible line count.
            int viewportLineCount = viewportEndLineNumber - viewportStartLineNumber + 1;
            sliderHeight = Math.Floor(viewportLineCount * minimapLineHeight / pixelRatio);
        }
        else
        {
            // The slider has a stable height
            double expectedViewportLineCount = (double)viewportHeight / lineHeight;
            sliderHeight = Math.Floor(expectedViewportLineCount * minimapLineHeight / pixelRatio);
        }

        int extraLinesAtTheTop = (int)Math.Floor((double)options.PaddingTop / lineHeight);
        double extraLinesAtTheBottom = Math.Floor((double)options.PaddingBottom / lineHeight);
        if (options.ScrollBeyondLastLine)
        {
            double expectedViewportLineCount = (double)viewportHeight / lineHeight;
            extraLinesAtTheBottom = Math.Max(extraLinesAtTheBottom, expectedViewportLineCount - 1);
        }

        double maxMinimapSliderTop;
        if (extraLinesAtTheBottom > 0)
        {
            double expectedViewportLineCount = (double)viewportHeight / lineHeight;
            // The minimap slider, when dragged all the way down, will contain the last line at its top
            maxMinimapSliderTop = (extraLinesAtTheTop + lineCount + extraLinesAtTheBottom - expectedViewportLineCount - 1) * minimapLineHeight / pixelRatio;
        }
        else
        {
            // The minimap slider, when dragged all the way down, will contain the last line at its bottom
            maxMinimapSliderTop = Math.Max(0, ((extraLinesAtTheTop + lineCount) * minimapLineHeight / pixelRatio) - sliderHeight);
        }

        maxMinimapSliderTop = Math.Min(options.MinimapHeight - sliderHeight, maxMinimapSliderTop);

        double computedSliderRatio = maxMinimapSliderTop / (scrollHeight - viewportHeight);
        double sliderTop = scrollTop * computedSliderRatio;

        if (minimapLinesFitting >= extraLinesAtTheTop + lineCount + extraLinesAtTheBottom)
        {
            // All lines fit in the minimap
            return new MinimapLayout(
                scrollTop, scrollHeight, maxMinimapSliderTop > 0, computedSliderRatio, sliderTop, sliderHeight,
                extraLinesAtTheTop, 1, lineCount);
        }

        double consideringStartLineNumber = viewportStartLineNumber > 1
            ? viewportStartLineNumber + extraLinesAtTheTop
            : Math.Max(1, scrollTop / lineHeight);

        int topPaddingLineCount;
        int startLineNumber = (int)Math.Max(1, Math.Floor(consideringStartLineNumber - (sliderTop * pixelRatio / minimapLineHeight)));
        if (startLineNumber < extraLinesAtTheTop)
        {
            topPaddingLineCount = extraLinesAtTheTop - startLineNumber + 1;
            startLineNumber = 1;
        }
        else
        {
            topPaddingLineCount = 0;
            startLineNumber = Math.Max(1, startLineNumber - extraLinesAtTheTop);
        }

        // Avoid flickering caused by a partial viewport start line by being consistent w.r.t. the previous layout decision
        if (previousLayout is not null && previousLayout.ScrollHeight == scrollHeight)
        {
            if (previousLayout.ScrollTop > scrollTop)
            {
                // Scrolling up => never increase `startLineNumber`
                startLineNumber = Math.Min(startLineNumber, previousLayout.StartLineNumber);
                topPaddingLineCount = Math.Max(topPaddingLineCount, previousLayout.TopPaddingLineCount);
            }

            if (previousLayout.ScrollTop < scrollTop)
            {
                // Scrolling down => never decrease `startLineNumber`
                startLineNumber = Math.Max(startLineNumber, previousLayout.StartLineNumber);
                topPaddingLineCount = Math.Min(topPaddingLineCount, previousLayout.TopPaddingLineCount);
            }
        }

        int endLineNumber = Math.Min(lineCount, startLineNumber - topPaddingLineCount + minimapLinesFitting - 1);
        double partialLine = (scrollTop - viewportStartLineNumberVerticalOffset) / lineHeight;

        double sliderTopAligned = scrollTop >= options.PaddingTop
            ? (viewportStartLineNumber - startLineNumber + topPaddingLineCount + partialLine) * minimapLineHeight / pixelRatio
            : scrollTop / options.PaddingTop * (topPaddingLineCount + partialLine) * minimapLineHeight / pixelRatio;

        return new MinimapLayout(
            scrollTop, scrollHeight, true, computedSliderRatio, sliderTopAligned, sliderHeight, topPaddingLineCount,
            startLineNumber, endLineNumber);
    }
}
