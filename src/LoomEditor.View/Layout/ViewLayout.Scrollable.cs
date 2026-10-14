// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

internal sealed partial class ViewLayout
{
    /// <summary>Monaco: <c>EditorScrollDimensions</c>. Negative values become 0.</summary>
    private sealed class EditorScrollDimensions : IEquatable<EditorScrollDimensions>
    {
        public EditorScrollDimensions(int width, int contentWidth, int height, int contentHeight)
        {
            Width = Math.Max(0, width);
            ContentWidth = Math.Max(0, contentWidth);
            ScrollWidth = Math.Max(Width, ContentWidth);

            Height = Math.Max(0, height);
            ContentHeight = Math.Max(0, contentHeight);
            ScrollHeight = Math.Max(Height, ContentHeight);
        }

        public int Width { get; }

        public int ContentWidth { get; }

        public int ScrollWidth { get; }

        public int Height { get; }

        public int ContentHeight { get; }

        public int ScrollHeight { get; }

        public bool Equals(EditorScrollDimensions? other)
        {
            return other is not null
                && Width == other.Width
                && ContentWidth == other.ContentWidth
                && Height == other.Height
                && ContentHeight == other.ContentHeight;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EditorScrollDimensions);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, ContentWidth, Height, ContentHeight);
        }
    }

    /// <summary>Monaco: <c>EditorScrollable</c>. A <see cref="Layout.Scrollable"/> that also tracks the content size.</summary>
    private sealed class EditorScrollable : IDisposable
    {
        private readonly Scrollable _scrollable;
        private EditorScrollDimensions _dimensions = new(0, 0, 0, 0);

        public EditorScrollable(
            int smoothScrollDuration, Func<Action, IDisposable> scheduleAtNextAnimationFrame, TimeProvider timeProvider)
        {
            _scrollable = new Scrollable(new ScrollableOptions(true, smoothScrollDuration, scheduleAtNextAnimationFrame, timeProvider));
        }

        public event EventHandler<ScrollEvent>? Scroll
        {
            add => _scrollable.Scroll += value;
            remove => _scrollable.Scroll -= value;
        }

        public event EventHandler<ContentSizeChangedEventArgs>? ContentSizeChanged;

        public Scrollable Scrollable => _scrollable;

        public bool HasPendingScrollAnimation => _scrollable.HasPendingScrollAnimation;

        public void Dispose()
        {
            _scrollable.Dispose();
        }

        public void SetSmoothScrollDuration(int smoothScrollDuration)
        {
            _scrollable.SetSmoothScrollDuration(smoothScrollDuration);
        }

        public IScrollPosition ValidateScrollPosition(NewScrollPosition scrollPosition)
        {
            return _scrollable.ValidateScrollPosition(scrollPosition);
        }

        public EditorScrollDimensions GetScrollDimensions()
        {
            return _dimensions;
        }

        public void SetScrollDimensions(EditorScrollDimensions dimensions)
        {
            if (_dimensions.Equals(dimensions))
            {
                return;
            }

            EditorScrollDimensions oldDimensions = _dimensions;
            _dimensions = dimensions;

            _scrollable.SetScrollDimensions(
                new NewScrollDimensions(dimensions.Width, dimensions.ScrollWidth, dimensions.Height, dimensions.ScrollHeight),
                true);

            bool contentWidthChanged = oldDimensions.ContentWidth != dimensions.ContentWidth;
            bool contentHeightChanged = oldDimensions.ContentHeight != dimensions.ContentHeight;
            if (contentWidthChanged || contentHeightChanged)
            {
                ContentSizeChanged?.Invoke(this, new ContentSizeChangedEventArgs(
                    oldDimensions.ContentWidth, oldDimensions.ContentHeight,
                    dimensions.ContentWidth, dimensions.ContentHeight));
            }
        }

        public IScrollPosition GetFutureScrollPosition()
        {
            return _scrollable.GetFutureScrollPosition();
        }

        public IScrollPosition GetCurrentScrollPosition()
        {
            return _scrollable.GetCurrentScrollPosition();
        }

        public void SetScrollPositionNow(NewScrollPosition update)
        {
            _scrollable.SetScrollPositionNow(update);
        }

        public void SetScrollPositionSmooth(NewScrollPosition update)
        {
            _scrollable.SetScrollPositionSmooth(update);
        }
    }
}
