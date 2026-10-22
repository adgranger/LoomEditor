// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Joins <see cref="LinesLayout"/> (vertical offsets) and <see cref="Scrollable"/> (scroll position) for an editor:
/// content size, "scroll beyond last line", the current viewport and the visible lines. Monaco: <c>ViewLayout</c>.
/// </summary>
/// <remarks>
/// Threading: UI thread only; events are raised synchronously on the calling thread.
/// Behavior differences: Monaco's <c>onConfigurationChanged</c> is split into <see cref="OnOptionsChanged"/>,
/// <see cref="OnLayoutInfoChanged"/> and <see cref="OnFontInfoChanged"/> (explicit inputs replace
/// <c>IEditorConfiguration</c>); word wrap is never on, so only the non-wrapping content width is computed.
/// </remarks>
internal sealed partial class ViewLayout : IDisposable
{
    private const int SmoothScrollingTime = 125;

    private readonly LinesLayout _linesLayout;
    private readonly EditorScrollable _scrollable;
    private ViewLayoutOptions _options;
    private EditorLayoutInfo _layoutInfo;
    private FontInfo _fontInfo;
    private int _maxLineWidth;
    private int _overlayWidgetsMinWidth;

    public ViewLayout(
        ViewLayoutOptions options,
        EditorLayoutInfo layoutInfo,
        FontInfo fontInfo,
        int lineCount,
        IReadOnlyList<CustomLineHeightData> customLineHeightData,
        Func<Action, IDisposable> scheduleAtNextAnimationFrame,
        TimeProvider timeProvider)
    {
        _options = options;
        _layoutInfo = layoutInfo;
        _fontInfo = fontInfo;

        _linesLayout = new LinesLayout(
            lineCount, options.LineHeight, options.PaddingTop, options.PaddingBottom, customLineHeightData);
        _maxLineWidth = 0;
        _overlayWidgetsMinWidth = 0;

        _scrollable = new EditorScrollable(0, scheduleAtNextAnimationFrame, timeProvider);
        ConfigureSmoothScrollDuration();

        _scrollable.SetScrollDimensions(new EditorScrollDimensions(layoutInfo.ContentWidth, 0, layoutInfo.Height, 0));

        UpdateHeight();
    }

    /// <summary>Monaco: <c>onDidScroll</c>.</summary>
    public event EventHandler<ScrollEvent>? Scroll
    {
        add => _scrollable.Scroll += value;
        remove => _scrollable.Scroll -= value;
    }

    /// <summary>Monaco: <c>onDidContentSizeChange</c>.</summary>
    public event EventHandler<ContentSizeChangedEventArgs>? ContentSizeChanged
    {
        add => _scrollable.ContentSizeChanged += value;
        remove => _scrollable.ContentSizeChanged -= value;
    }

    public bool HasPendingScrollAnimation => _scrollable.HasPendingScrollAnimation;

    public void Dispose()
    {
        _scrollable.Dispose();
    }

    public Scrollable GetScrollable()
    {
        return _scrollable.Scrollable;
    }

    public void OnHeightMaybeChanged()
    {
        UpdateHeight();
    }

    private void ConfigureSmoothScrollDuration()
    {
        _scrollable.SetSmoothScrollDuration(_options.SmoothScrolling ? SmoothScrollingTime : 0);
    }

    // ---- begin view event handlers

    /// <summary>Monaco: <c>onConfigurationChanged</c> for <c>lineHeight</c>, <c>padding</c>, <c>smoothScrolling</c> and the scrollbar options.</summary>
    public void OnOptionsChanged(ViewLayoutOptions options)
    {
        ViewLayoutOptions old = _options;
        _options = options;

        if (old.LineHeight != options.LineHeight)
        {
            _linesLayout.SetDefaultLineHeight(options.LineHeight);
        }

        if (old.PaddingTop != options.PaddingTop || old.PaddingBottom != options.PaddingBottom)
        {
            _linesLayout.SetPadding(options.PaddingTop, options.PaddingBottom);
        }

        UpdateHeight();

        if (old.SmoothScrolling != options.SmoothScrolling)
        {
            ConfigureSmoothScrollDuration();
        }
    }

    /// <summary>Monaco: <c>onConfigurationChanged</c> when <c>layoutInfo</c> changed.</summary>
    public void OnLayoutInfoChanged(EditorLayoutInfo layoutInfo)
    {
        _layoutInfo = layoutInfo;

        int width = layoutInfo.ContentWidth;
        int height = layoutInfo.Height;
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        int contentWidth = scrollDimensions.ContentWidth;
        _scrollable.SetScrollDimensions(new EditorScrollDimensions(
            width,
            scrollDimensions.ContentWidth,
            height,
            GetContentHeight(width, height, contentWidth)));
    }

    /// <summary>
    /// Monaco has no <c>fontInfo</c> branch in <c>onConfigurationChanged</c>: the font only feeds the next content width
    /// computation (<see cref="SetMaxLineWidth"/>), so this only stores it.
    /// </summary>
    public void OnFontInfoChanged(FontInfo fontInfo)
    {
        _fontInfo = fontInfo;
    }

    public void OnFlushed(int lineCount, IReadOnlyList<CustomLineHeightData> customLineHeightData)
    {
        _linesLayout.OnFlushed(lineCount, customLineHeightData);
    }

    public void OnLinesDeleted(int fromLineNumber, int toLineNumber)
    {
        _linesLayout.OnLinesDeleted(fromLineNumber, toLineNumber);
    }

    public void OnLinesInserted(int fromLineNumber, int toLineNumber)
    {
        _linesLayout.OnLinesInserted(fromLineNumber, toLineNumber);
    }

    // ---- end view event handlers

    private int GetHorizontalScrollbarHeight(int width, int scrollWidth)
    {
        if (_options.HorizontalScrollbarVisibility == ScrollbarVisibility.Hidden)
        {
            // horizontal scrollbar not visible
            return 0;
        }

        if (width >= scrollWidth)
        {
            // horizontal scrollbar not visible
            return 0;
        }

        return _options.HorizontalScrollbarSize;
    }

    private int GetContentHeight(int width, int height, int contentWidth)
    {
        int result = _linesLayout.GetLinesTotalHeight();
        if (_options.ScrollBeyondLastLine)
        {
            result += Math.Max(0, height - _options.LineHeight - _options.PaddingBottom);
        }
        else if (!_options.IgnoreHorizontalScrollbarInContentHeight)
        {
            result += GetHorizontalScrollbarHeight(width, contentWidth);
        }

        return result;
    }

    private void UpdateHeight()
    {
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        int width = scrollDimensions.Width;
        int height = scrollDimensions.Height;
        int contentWidth = scrollDimensions.ContentWidth;
        _scrollable.SetScrollDimensions(new EditorScrollDimensions(
            width,
            scrollDimensions.ContentWidth,
            height,
            GetContentHeight(width, height, contentWidth)));
    }

    // ---- Layouting logic

    public Viewport GetCurrentViewport()
    {
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        IScrollPosition position = _scrollable.GetCurrentScrollPosition();
        return new Viewport((int)position.ScrollTop, (int)position.ScrollLeft, scrollDimensions.Width, scrollDimensions.Height);
    }

    public Viewport GetFutureViewport()
    {
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        IScrollPosition position = _scrollable.GetFutureScrollPosition();
        return new Viewport((int)position.ScrollTop, (int)position.ScrollLeft, scrollDimensions.Width, scrollDimensions.Height);
    }

    private int ComputeContentWidth()
    {
        if (_layoutInfo.IsViewportWrapping)
        {
            // Monaco: minimap.enabled && minimap.side === 'right' (a minimap on the left starts at 0).
            bool minimapOnTheRight = _layoutInfo.Minimap.RenderMinimap != RenderMinimap.None && _layoutInfo.Minimap.MinimapLeft != 0;
            if (_maxLineWidth > _layoutInfo.ContentWidth + _fontInfo.TypicalHalfwidthCharacterWidth && minimapOnTheRight)
            {
                // This is a case where viewport wrapping is on, but the line extends above the viewport
                return _maxLineWidth + _layoutInfo.VerticalScrollbarWidth;
            }

            return _maxLineWidth;
        }

        int extraHorizontalSpace = (int)(_options.ScrollBeyondLastColumn * _fontInfo.TypicalHalfwidthCharacterWidth);
        int whitespaceMinWidth = _linesLayout.GetWhitespaceMinWidth();
        return Math.Max(
            Math.Max(_maxLineWidth + extraHorizontalSpace + _layoutInfo.VerticalScrollbarWidth, whitespaceMinWidth),
            _overlayWidgetsMinWidth);
    }

    public void SetMaxLineWidth(int maxLineWidth)
    {
        _maxLineWidth = maxLineWidth;
        UpdateContentWidth();
    }

    public void SetOverlayWidgetsMinWidth(int maxMinWidth)
    {
        _overlayWidgetsMinWidth = maxMinWidth;
        UpdateContentWidth();
    }

    private void UpdateContentWidth()
    {
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        _scrollable.SetScrollDimensions(new EditorScrollDimensions(
            scrollDimensions.Width,
            ComputeContentWidth(),
            scrollDimensions.Height,
            scrollDimensions.ContentHeight));

        // The height might depend on the fact that there is a horizontal scrollbar or not
        UpdateHeight();
    }

    // ---- view state

    public ViewLayoutState SaveState()
    {
        IScrollPosition position = _scrollable.GetFutureScrollPosition();
        int scrollTop = (int)position.ScrollTop;
        int firstLineNumberInViewport = _linesLayout.GetLineNumberAtOrAfterVerticalOffset(scrollTop);
        int whitespaceAboveFirstLine = _linesLayout.GetWhitespaceAccumulatedHeightBeforeLineNumber(firstLineNumberInViewport);
        return new ViewLayoutState(scrollTop, scrollTop - whitespaceAboveFirstLine, position.ScrollLeft);
    }

    // ----

    public bool ChangeWhitespace(Action<IWhitespaceChangeAccessor> callback)
    {
        bool hadAChange = _linesLayout.ChangeWhitespace(callback);
        if (hadAChange)
        {
            OnHeightMaybeChanged();
        }

        return hadAChange;
    }

    public bool ChangeSpecialLineHeights(Action<ILineHeightChangeAccessor> callback)
    {
        bool hadAChange = _linesLayout.ChangeLineHeights(callback);
        if (hadAChange)
        {
            OnHeightMaybeChanged();
        }

        return hadAChange;
    }

    public int GetVerticalOffsetForLineNumber(int lineNumber, bool includeViewZones = false)
    {
        return _linesLayout.GetVerticalOffsetForLineNumber(lineNumber, includeViewZones);
    }

    public int GetVerticalOffsetAfterLineNumber(int lineNumber, bool includeViewZones = false)
    {
        return _linesLayout.GetVerticalOffsetAfterLineNumber(lineNumber, includeViewZones);
    }

    public int GetLineHeightForLineNumber(int lineNumber)
    {
        return _linesLayout.GetLineHeightForLineNumber(lineNumber);
    }

    public bool IsAfterLines(int verticalOffset)
    {
        return _linesLayout.IsAfterLines(verticalOffset);
    }

    public bool IsInTopPadding(int verticalOffset)
    {
        return _linesLayout.IsInTopPadding(verticalOffset);
    }

    public bool IsInBottomPadding(int verticalOffset)
    {
        return _linesLayout.IsInBottomPadding(verticalOffset);
    }

    public int GetLineNumberAtVerticalOffset(int verticalOffset)
    {
        return _linesLayout.GetLineNumberAtOrAfterVerticalOffset(verticalOffset);
    }

    public ViewWhitespaceViewportData? GetWhitespaceAtVerticalOffset(int verticalOffset)
    {
        return _linesLayout.GetWhitespaceAtVerticalOffset(verticalOffset);
    }

    public PartialViewLinesViewportData GetLinesViewportData()
    {
        Viewport visibleBox = GetCurrentViewport();
        return _linesLayout.GetLinesViewportData(visibleBox.Top, visibleBox.Top + visibleBox.Height);
    }

    public PartialViewLinesViewportData GetLinesViewportDataAtScrollTop(int scrollTop)
    {
        // do some minimal validations on scrollTop
        EditorScrollDimensions scrollDimensions = _scrollable.GetScrollDimensions();
        if (scrollTop + scrollDimensions.Height > scrollDimensions.ScrollHeight)
        {
            scrollTop = scrollDimensions.ScrollHeight - scrollDimensions.Height;
        }

        if (scrollTop < 0)
        {
            scrollTop = 0;
        }

        return _linesLayout.GetLinesViewportData(scrollTop, scrollTop + scrollDimensions.Height);
    }

    public IReadOnlyList<ViewWhitespaceViewportData> GetWhitespaceViewportData()
    {
        Viewport visibleBox = GetCurrentViewport();
        return _linesLayout.GetWhitespaceViewportData(visibleBox.Top, visibleBox.Top + visibleBox.Height);
    }

    public IReadOnlyList<IEditorWhitespace> GetWhitespaces()
    {
        return _linesLayout.GetWhitespaces();
    }

    // ----

    public int GetContentWidth()
    {
        return _scrollable.GetScrollDimensions().ContentWidth;
    }

    public int GetScrollWidth()
    {
        return _scrollable.GetScrollDimensions().ScrollWidth;
    }

    public int GetContentHeight()
    {
        return _scrollable.GetScrollDimensions().ContentHeight;
    }

    public int GetScrollHeight()
    {
        return _scrollable.GetScrollDimensions().ScrollHeight;
    }

    public double GetCurrentScrollLeft()
    {
        return _scrollable.GetCurrentScrollPosition().ScrollLeft;
    }

    public double GetCurrentScrollTop()
    {
        return _scrollable.GetCurrentScrollPosition().ScrollTop;
    }

    public IScrollPosition ValidateScrollPosition(NewScrollPosition scrollPosition)
    {
        return _scrollable.ValidateScrollPosition(scrollPosition);
    }

    public void SetScrollPosition(NewScrollPosition position, ScrollType type)
    {
        if (type == ScrollType.Immediate)
        {
            _scrollable.SetScrollPositionNow(position);
        }
        else
        {
            _scrollable.SetScrollPositionSmooth(position);
        }
    }

    public void DeltaScrollNow(double deltaScrollLeft, double deltaScrollTop)
    {
        IScrollPosition current = _scrollable.GetCurrentScrollPosition();
        _scrollable.SetScrollPositionNow(new NewScrollPosition(
            current.ScrollLeft + deltaScrollLeft,
            current.ScrollTop + deltaScrollTop));
    }
}
