// Ported from microsoft/vscode: src/vs/base/common/scrollable.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Immutable scroll state: viewport size, scrollable size and (clamped) scroll position.
/// Monaco: <c>ScrollState</c>.
/// </summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not a record on purpose: <see cref="Equals(ScrollState)"/> compares the
/// raw positions but not the integer-forcing flag.
/// </remarks>
internal sealed class ScrollState : IScrollDimensions, IScrollPosition, ISmoothScrollPosition
{
    private readonly bool _forceIntegerValues;

    public ScrollState(
        bool forceIntegerValues,
        double width,
        double scrollWidth,
        double scrollLeft,
        double height,
        double scrollHeight,
        double scrollTop)
    {
        _forceIntegerValues = forceIntegerValues;

        if (forceIntegerValues)
        {
            // `(double)(int)x` is the C# form of JavaScript's `x | 0` (truncation toward zero).
            width = (double)(int)width;
            scrollWidth = (double)(int)scrollWidth;
            scrollLeft = (double)(int)scrollLeft;
            height = (double)(int)height;
            scrollHeight = (double)(int)scrollHeight;
            scrollTop = (double)(int)scrollTop;
        }

        RawScrollLeft = scrollLeft; // before validation
        RawScrollTop = scrollTop; // before validation

        if (width < 0)
        {
            width = 0;
        }

        if (scrollLeft + width > scrollWidth)
        {
            scrollLeft = scrollWidth - width;
        }

        if (scrollLeft < 0)
        {
            scrollLeft = 0;
        }

        if (height < 0)
        {
            height = 0;
        }

        if (scrollTop + height > scrollHeight)
        {
            scrollTop = scrollHeight - height;
        }

        if (scrollTop < 0)
        {
            scrollTop = 0;
        }

        Width = width;
        ScrollWidth = scrollWidth;
        ScrollLeft = scrollLeft;
        Height = height;
        ScrollHeight = scrollHeight;
        ScrollTop = scrollTop;
    }

    public double RawScrollLeft { get; }

    public double RawScrollTop { get; }

    public double Width { get; }

    public double ScrollWidth { get; }

    public double ScrollLeft { get; }

    public double Height { get; }

    public double ScrollHeight { get; }

    public double ScrollTop { get; }

    public bool Equals(ScrollState other)
    {
        return RawScrollLeft == other.RawScrollLeft
            && RawScrollTop == other.RawScrollTop
            && Width == other.Width
            && ScrollWidth == other.ScrollWidth
            && ScrollLeft == other.ScrollLeft
            && Height == other.Height
            && ScrollHeight == other.ScrollHeight
            && ScrollTop == other.ScrollTop;
    }

    public ScrollState WithScrollDimensions(NewScrollDimensions update, bool useRawScrollPositions)
    {
        return new ScrollState(
            _forceIntegerValues,
            update.Width ?? Width,
            update.ScrollWidth ?? ScrollWidth,
            useRawScrollPositions ? RawScrollLeft : ScrollLeft,
            update.Height ?? Height,
            update.ScrollHeight ?? ScrollHeight,
            useRawScrollPositions ? RawScrollTop : ScrollTop);
    }

    public ScrollState WithScrollPosition(NewScrollPosition update)
    {
        return new ScrollState(
            _forceIntegerValues,
            Width,
            ScrollWidth,
            update.ScrollLeft ?? RawScrollLeft,
            Height,
            ScrollHeight,
            update.ScrollTop ?? RawScrollTop);
    }

    public ScrollEvent CreateScrollEvent(ScrollState previous, bool inSmoothScrolling)
    {
        return new ScrollEvent(
            inSmoothScrolling,
            previous.Width,
            previous.ScrollWidth,
            previous.ScrollLeft,
            Width,
            ScrollWidth,
            ScrollLeft,
            previous.Height,
            previous.ScrollHeight,
            previous.ScrollTop,
            Height,
            ScrollHeight,
            ScrollTop,
            Width != previous.Width,
            ScrollWidth != previous.ScrollWidth,
            ScrollLeft != previous.ScrollLeft,
            Height != previous.Height,
            ScrollHeight != previous.ScrollHeight,
            ScrollTop != previous.ScrollTop);
    }
}
