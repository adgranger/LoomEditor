// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (InnerMinimap pointer handlers)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>Click, slider drag, touch and hover handling of the minimap. Monaco: the pointer listeners of <c>InnerMinimap</c>.</summary>
/// <remarks>
/// Threading: UI thread only. Coordinates are DIPs relative to the minimap's top-left corner. The orthogonal reset distance
/// applies on every platform because Monaco's <c>platform.isWindows</c> is always true for LoomEditor.
/// </remarks>
internal sealed class MinimapPointerController(IMinimapModel model, Func<MinimapLayout?> getRenderedLayout, IMinimapPresentation presentation)
{
    private bool _dragging;
    private bool _gestureInProgress;
    private double _initialX;
    private double _initialY;
    private MinimapLayout? _initialLayout;

    /// <summary>A slider drag or a touch gesture is in progress, so the pointer must stay captured.</summary>
    public bool IsActive => _dragging || _gestureInProgress;

    /// <summary>Monaco: the <c>mouseover</c> listener of the minimap node.</summary>
    public void PointerEntered() => presentation.MouseOverMinimap = true;

    /// <summary>Monaco: the <c>mouseleave</c> listener of the minimap node.</summary>
    public void PointerExited()
    {
        presentation.MouseOverMinimap = false;
        if (!_dragging)
        {
            presentation.SliderHovered = false;
        }
    }

    /// <summary>Handles a pointer press; returns whether the minimap consumed it.</summary>
    public bool PointerDown(double x, double y, bool isLeftButton, bool isMouse)
    {
        MinimapOptions options = model.Options;
        MinimapLayout? layout = getRenderedLayout();
        if (options.RenderMinimap == RenderMinimap.None || layout is null)
        {
            return false;
        }

        if (!isMouse)
        {
            // Monaco: the gesture `Start` listener.
            presentation.SliderActive = true;
            _gestureInProgress = true;
            ScrollDueToTouchEvent(y, layout);
            return true;
        }

        if (isLeftButton && layout.SliderNeeded && y >= layout.SliderTop && y <= layout.SliderTop + layout.SliderHeight)
        {
            StartSliderDragging(x, y, y, layout);
            return true;
        }

        if (options.Size != MinimapSize.Proportional)
        {
            if (isLeftButton)
            {
                // Pretend the click occurred in the center of the slider.
                StartSliderDragging(x, y, layout.SliderTop + (layout.SliderHeight / 2), layout);
            }

            return true;
        }

        if (isLeftButton)
        {
            double internalOffsetY = options.CanvasInnerHeight / options.CanvasOuterHeight * y;
            int lineIndex = (int)Math.Floor(internalOffsetY / options.MinimapLineHeight);
            int lineNumber = lineIndex + layout.StartLineNumber - layout.TopPaddingLineCount;
            model.RevealLineNumber(Math.Min(lineNumber, model.GetLineCount()));
        }

        return true;
    }

    /// <summary>Handles a pointer move (also outside the minimap while a drag is in progress).</summary>
    public void PointerMove(double x, double y)
    {
        MinimapLayout? layout = getRenderedLayout();
        if (_gestureInProgress)
        {
            if (layout is not null)
            {
                ScrollDueToTouchEvent(y, layout);
            }

            return;
        }

        if (_dragging)
        {
            HandleDragMove(x, y);
        }
        else
        {
            presentation.SliderHovered = layout is { SliderNeeded: true } && y >= layout.SliderTop && y <= layout.SliderTop + layout.SliderHeight
                && x >= 0 && x <= model.Options.MinimapWidth;
        }
    }

    /// <summary>Ends a drag or a touch gesture.</summary>
    public void PointerUp()
    {
        _dragging = false;
        _gestureInProgress = false;
        presentation.SliderActive = false;
    }

    // Monaco: `_startSliderDragging`.
    private void StartSliderDragging(double x, double y, double initialY, MinimapLayout layout)
    {
        _initialX = x;
        _initialY = initialY;
        _initialLayout = layout;
        _dragging = true;
        presentation.SliderActive = true;
        if (y != initialY)
        {
            HandleDragMove(x, y);
        }
    }

    private void HandleDragMove(double x, double y)
    {
        MinimapLayout initial = _initialLayout!;
        double width = model.Options.MinimapWidth;
        double orthogonalDelta = Math.Min(Math.Abs(x - _initialX), Math.Min(Math.Abs(x), Math.Abs(x - width)));
        if (orthogonalDelta > MinimapOptions.PointerDragResetDistance)
        {
            // The pointer has wandered away from the scrollbar: reset dragging.
            model.SetScrollTop(initial.ScrollTop);
            return;
        }

        model.SetScrollTop(initial.GetDesiredScrollTopFromDelta(y - _initialY));
    }

    // Monaco: `scrollDueToTouchEvent`.
    private void ScrollDueToTouchEvent(double y, MinimapLayout layout) => model.SetScrollTop(layout.GetDesiredScrollTopFromTouchLocation(y));
}
