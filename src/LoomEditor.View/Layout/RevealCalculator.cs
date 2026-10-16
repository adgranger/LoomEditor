// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewLines/viewLines.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

/// <summary>
/// Turns <see cref="ViewRevealRangeRequestEvent"/>s into scroll positions: the scrolling half of Monaco's <c>ViewLines</c>.
/// </summary>
/// <remarks>
/// <para>
/// The vertical scroll is applied at once. The horizontal one needs the X of a character, so it is kept as a
/// <see cref="HorizontalRevealRequest"/> until the control has drawn the lines it needs
/// (<see cref="TryApplyPendingHorizontalReveal"/>, Monaco: step 2 of <c>renderText</c>).
/// </para>
/// <para>
/// Phase 4–5 cut point: Monaco's <c>_stickyScrollEnabled</c> is always <c>false</c> (no sticky scroll), and
/// <c>hasRTL</c> is always <c>false</c> (no right-to-left lines, no word wrap).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class RevealCalculator(ViewModel viewModel)
{
    /// <summary>Monaco: <c>ViewLines.HORIZONTAL_EXTRA_PX</c>.</summary>
    private const int HorizontalExtraPx = 30;

    /// <summary>Monaco: <c>Constants.MAX_SAFE_SMALL_INTEGER</c>.</summary>
    private const int MaxSafeSmallInteger = 1 << 30;

    /// <summary>The horizontal scroll waiting for its lines to be drawn, if any.</summary>
    public HorizontalRevealRequest? PendingHorizontalRequest { get; private set; }

    /// <summary>
    /// Scrolls to reveal the range or the selections of the event (Monaco: <c>onRevealRangeRequest</c>).
    /// </summary>
    /// <returns><c>false</c> if the request was aborted (for example too many cursors to fit the viewport).</returns>
    public bool OnRevealRangeRequest(ViewRevealRangeRequestEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        ViewLayout viewLayout = viewModel.ViewLayout;

        // Using the future viewport here in order to handle multiple incoming reveal range requests that might all
        // desire to be animated.
        int desiredScrollTop = ComputeScrollTopToRevealRange(
            viewLayout.GetFutureViewport(), e.Source, e.MinimalReveal, e.Range, e.Selections, e.VerticalType);
        if (desiredScrollTop == -1)
        {
            // Marker to abort the reveal range request.
            return false;
        }

        IScrollPosition validated = viewLayout.ValidateScrollPosition(new NewScrollPosition(null, desiredScrollTop));
        var newScrollPosition = new NewScrollPosition(null, validated.ScrollTop);

        if (e.RevealHorizontal)
        {
            if (e.Range is { } range && range.StartLineNumber != range.EndLineNumber)
            {
                // Two or more lines? => scroll to base (that is how you see most of the two lines).
                newScrollPosition = new NewScrollPosition(0, validated.ScrollTop);
            }
            else if (e.Range is { } singleLine)
            {
                // We don't necessarily know the horizontal offset of this range since the line might not be in the view...
                PendingHorizontalRequest = new HorizontalRevealRangeRequest(
                    e.MinimalReveal, singleLine.StartLineNumber, singleLine.StartColumn, singleLine.EndColumn,
                    viewLayout.GetCurrentScrollTop(), validated.ScrollTop, e.ScrollType);
            }
            else if (e.Selections is { Count: > 0 } selections)
            {
                PendingHorizontalRequest = new HorizontalRevealSelectionsRequest(
                    e.MinimalReveal, selections, viewLayout.GetCurrentScrollTop(), validated.ScrollTop, e.ScrollType);
            }
        }
        else
        {
            PendingHorizontalRequest = null;
        }

        double scrollTopDelta = Math.Abs(viewLayout.GetCurrentScrollTop() - validated.ScrollTop);
        ScrollType scrollType = scrollTopDelta <= viewModel.FontInfo.LineHeight ? ScrollType.Immediate : e.ScrollType;
        viewLayout.SetScrollPosition(newScrollPosition, scrollType);
        return true;
    }

    /// <summary>Cancels the pending horizontal request when someone else scrolls (Monaco: <c>onScrollChanged</c>).</summary>
    public void OnScrollChanged(in ScrollEvent e)
    {
        if (PendingHorizontalRequest is not null && e.ScrollLeftChanged)
        {
            // Cancel any outstanding horizontal reveal request if someone else scrolls horizontally.
            PendingHorizontalRequest = null;
        }

        if (PendingHorizontalRequest is { } request && e.ScrollTopChanged)
        {
            double min = Math.Min(request.StartScrollTop, request.StopScrollTop);
            double max = Math.Max(request.StartScrollTop, request.StopScrollTop);
            if (e.ScrollTop < min || e.ScrollTop > max)
            {
                // Cancel any outstanding horizontal reveal request if someone else scrolls vertically.
                PendingHorizontalRequest = null;
            }
        }
    }

    /// <summary>
    /// Step 2 of Monaco's <c>renderText</c>: once the lines of the pending request are drawn, scrolls horizontally.
    /// </summary>
    /// <param name="startLineNumber">First line that was drawn.</param>
    /// <param name="endLineNumber">Last line that was drawn.</param>
    /// <param name="visibleRangeForLineRange">Monaco's <c>_visibleRangesForLineRange</c>: (line, start column, end column) → left and width in DIPs.</param>
    /// <param name="ensureMaxLineWidth">Monaco's <c>_ensureMaxLineWidth</c>: makes the scroll width large enough for the given offset.</param>
    /// <returns><c>true</c> if a request was pending and its lines were drawn (the request is cleared).</returns>
    public bool TryApplyPendingHorizontalReveal(
        int startLineNumber,
        int endLineNumber,
        Func<int, int, int, (double Left, double Width)?> visibleRangeForLineRange,
        Action<int> ensureMaxLineWidth)
    {
        ArgumentNullException.ThrowIfNull(visibleRangeForLineRange);
        ArgumentNullException.ThrowIfNull(ensureMaxLineWidth);

        if (PendingHorizontalRequest is not { } request
            || startLineNumber > request.MinLineNumber
            || request.MaxLineNumber > endLineNumber)
        {
            return false;
        }

        PendingHorizontalRequest = null;
        (int ScrollLeft, int MaxHorizontalOffset)? newScrollLeft = ComputeScrollLeftToReveal(request, visibleRangeForLineRange);
        if (newScrollLeft is { } value)
        {
            ensureMaxLineWidth(value.MaxHorizontalOffset);
            viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(value.ScrollLeft, null), request.ScrollType);
        }

        return true;
    }

    /// <summary>Monaco: <c>_computeScrollTopToRevealRange</c>. Returns <c>-1</c> to abort.</summary>
    public int ComputeScrollTopToRevealRange(
        Viewport viewport,
        string? source,
        bool minimalReveal,
        Range? range,
        IReadOnlyList<Selection>? selections,
        VerticalRevealType verticalType)
    {
        int lineHeight = viewModel.FontInfo.LineHeight;
        EditorCursorOptions options = viewModel.CursorOptions;
        double viewportStartY = viewport.Top;
        double viewportHeight = viewport.Height;
        double viewportEndY = viewportStartY + viewportHeight;
        bool boxIsSingleRange;
        double boxStartY;
        double boxEndY;

        if (selections is { Count: > 0 })
        {
            int minLineNumber = selections[0].Range.StartLineNumber;
            int maxLineNumber = selections[0].Range.EndLineNumber;
            for (int i = 1; i < selections.Count; i++)
            {
                minLineNumber = Math.Min(minLineNumber, selections[i].Range.StartLineNumber);
                maxLineNumber = Math.Max(maxLineNumber, selections[i].Range.EndLineNumber);
            }

            boxIsSingleRange = false;
            boxStartY = viewModel.ViewLayout.GetVerticalOffsetForLineNumber(minLineNumber);
            boxEndY = viewModel.ViewLayout.GetVerticalOffsetForLineNumber(maxLineNumber) + lineHeight;
        }
        else if (range is { } singleRange)
        {
            boxIsSingleRange = true;
            boxStartY = viewModel.ViewLayout.GetVerticalOffsetForLineNumber(singleRange.StartLineNumber);
            boxEndY = viewModel.ViewLayout.GetVerticalOffsetForLineNumber(singleRange.EndLineNumber) + lineHeight;
        }
        else
        {
            return -1;
        }

        bool shouldIgnoreScrollOff =
            (source == "mouse" || minimalReveal) && options.CursorSurroundingLinesStyle == CursorSurroundingLinesStyle.Default;

        double paddingTop = 0;
        double paddingBottom = 0;
        if (!shouldIgnoreScrollOff)
        {
            double maxLinesInViewport = viewportHeight / lineHeight;
            // Phase 4–5 cut point: Monaco also takes the number of sticky scroll lines here (sticky scroll is off).
            int surroundingLines = options.CursorSurroundingLines;
            double context = Math.Min(maxLinesInViewport / 2, surroundingLines);
            paddingTop = context * lineHeight;
            paddingBottom = Math.Max(0, context - 1) * lineHeight;
        }
        else if (!minimalReveal)
        {
            // Reveal one more line above (this case is hit when dragging).
            paddingTop = lineHeight;
        }

        if (!minimalReveal && verticalType is VerticalRevealType.Simple or VerticalRevealType.Bottom)
        {
            // Reveal one line more when the last line would be covered by the scrollbar - arrow down case or
            // revealing a line explicitly at bottom.
            paddingBottom += lineHeight;
        }

        boxStartY -= paddingTop;
        boxEndY += paddingBottom;
        double newScrollTop;

        if (boxEndY - boxStartY > viewportHeight)
        {
            // The box is larger than the viewport ... scroll to its top.
            if (!boxIsSingleRange)
            {
                // Do not reveal multiple cursors if there are more than fit the viewport.
                return -1;
            }

            newScrollTop = boxStartY;
        }
        else if (verticalType is VerticalRevealType.NearTop or VerticalRevealType.NearTopIfOutsideViewport)
        {
            if (verticalType == VerticalRevealType.NearTopIfOutsideViewport && viewportStartY <= boxStartY && boxEndY <= viewportEndY)
            {
                // Box is already in the viewport... do nothing.
                newScrollTop = viewportStartY;
            }
            else
            {
                // We want a gap that is 20% of the viewport, but with a minimum of 5 lines.
                double desiredGapAbove = Math.Max(5 * lineHeight, viewportHeight * 0.2);
                // Try to scroll just above the box with the desired gap.
                double desiredScrollTop = boxStartY - desiredGapAbove;
                // But ensure that the box is not pushed out of viewport.
                double minScrollTop = boxEndY - viewportHeight;
                newScrollTop = Math.Max(minScrollTop, desiredScrollTop);
            }
        }
        else if (verticalType is VerticalRevealType.Center or VerticalRevealType.CenterIfOutsideViewport)
        {
            if (verticalType == VerticalRevealType.CenterIfOutsideViewport && viewportStartY <= boxStartY && boxEndY <= viewportEndY)
            {
                // Box is already in the viewport... do nothing.
                newScrollTop = viewportStartY;
            }
            else
            {
                // Box is outside the viewport... center it.
                double boxMiddleY = (boxStartY + boxEndY) / 2;
                newScrollTop = Math.Max(0, boxMiddleY - (viewportHeight / 2));
            }
        }
        else
        {
            newScrollTop = ComputeMinimumScrolling(
                (int)viewportStartY, (int)viewportEndY, (int)boxStartY, (int)boxEndY,
                verticalType == VerticalRevealType.Top, verticalType == VerticalRevealType.Bottom);
        }

        // Monaco keeps the fraction; scroll positions are whole DIPs here.
        return (int)Math.Floor(newScrollTop);
    }

    /// <summary>Monaco: <c>_computeScrollLeftToReveal</c>. Returns <c>null</c> when the horizontal reveal does not apply.</summary>
    /// <param name="request">What to reveal.</param>
    /// <param name="visibleRangeForLineRange">Monaco's <c>_visibleRangesForLineRange</c>; one range per line in phase 3 (no RTL, no wrap).</param>
    public (int ScrollLeft, int MaxHorizontalOffset)? ComputeScrollLeftToReveal(
        HorizontalRevealRequest request,
        Func<int, int, int, (double Left, double Width)?> visibleRangeForLineRange)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(visibleRangeForLineRange);

        Viewport viewport = viewModel.ViewLayout.GetCurrentViewport();
        int viewportStartX = viewport.Left;
        int viewportEndX = viewportStartX + viewport.Width - viewModel.LayoutInfo.VerticalScrollbarWidth;

        int boxStartX = MaxSafeSmallInteger;
        int boxEndX = 0;
        if (request is HorizontalRevealRangeRequest rangeRequest)
        {
            if (visibleRangeForLineRange(rangeRequest.LineNumber, rangeRequest.StartColumn, rangeRequest.EndColumn) is not { } visible)
            {
                return null;
            }

            boxStartX = Math.Min(boxStartX, RoundHalfUp(visible.Left));
            boxEndX = Math.Max(boxEndX, RoundHalfUp(visible.Left + visible.Width));
        }
        else if (request is HorizontalRevealSelectionsRequest selectionsRequest)
        {
            foreach (Selection selection in selectionsRequest.Selections)
            {
                Range selectionRange = selection.Range;
                if (selectionRange.StartLineNumber != selectionRange.EndLineNumber)
                {
                    return null;
                }

                if (visibleRangeForLineRange(selectionRange.StartLineNumber, selectionRange.StartColumn, selectionRange.EndColumn) is not { } visible)
                {
                    return null;
                }

                boxStartX = Math.Min(boxStartX, RoundHalfUp(visible.Left));
                boxEndX = Math.Max(boxEndX, RoundHalfUp(visible.Left + visible.Width));
            }
        }

        if (!request.MinimalReveal)
        {
            boxStartX = Math.Max(0, boxStartX - HorizontalExtraPx);
            boxEndX += viewModel.CursorOptions.RevealHorizontalRightPadding;
        }

        if (request is HorizontalRevealSelectionsRequest && boxEndX - boxStartX > viewport.Width)
        {
            return null;
        }

        int newScrollLeft = ComputeMinimumScrolling(viewportStartX, viewportEndX, boxStartX, boxEndX);
        return (newScrollLeft, boxEndX);
    }

    /// <summary>Monaco: <c>_computeMinimumScrolling</c>: the smallest scroll that shows the box.</summary>
    public static int ComputeMinimumScrolling(
        int viewportStart, int viewportEnd, int boxStart, int boxEnd, bool revealAtStart = false, bool revealAtEnd = false)
    {
        int viewportLength = viewportEnd - viewportStart;
        int boxLength = boxEnd - boxStart;

        if (boxLength < viewportLength)
        {
            // The box would fit in the viewport.
            if (revealAtStart)
            {
                return boxStart;
            }

            if (revealAtEnd)
            {
                return Math.Max(0, boxEnd - viewportLength);
            }

            if (boxStart < viewportStart)
            {
                // The box is above the viewport.
                return boxStart;
            }

            if (boxEnd > viewportEnd)
            {
                // The box is below the viewport.
                return Math.Max(0, boxEnd - viewportLength);
            }
        }
        else
        {
            // The box would not fit in the viewport: reveal the beginning of the box.
            return boxStart;
        }

        return viewportStart;
    }

    // JavaScript's Math.round: .5 rounds up (phase 3 conventions, trap 1).
    private static int RoundHalfUp(double value) => (int)Math.Floor(value + 0.5);
}
