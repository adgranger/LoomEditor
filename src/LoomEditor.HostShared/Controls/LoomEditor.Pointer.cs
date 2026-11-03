using LoomEditor.View.Controller;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// Mouse: the pointer events of the host drive the pure logic of `MouseDownOperation` (P3-43 to P3-45). Touch and pen are treated like
// the left mouse button (no gestures in phase 3). The host handlers read the framework event and call the decisions below.
public sealed partial class LoomEditor
{
    private MouseDownOperation? _mouseDown;

    /// <summary>
    /// Raised when the left button goes down on the gutter, on a line (not below the last line), before the editor acts on it. Set
    /// <see cref="GutterClickedEventArgs.Handled"/> to keep the editor from selecting the line or toggling a fold.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public event EventHandler<GutterClickedEventArgs>? GutterClicked;

    private void InitializePointer()
    {
        _mouseDown = new MouseDownOperation(
            _viewModel, new ViewController(_viewModel, _context.UserInputEvents), CreateMouseTarget, ScheduleAtNextAnimationFrame, TimeProvider.System);
        _context.MouseStyleChanged += (_, _) => SetTextAreaCursor(false);
    }

    // A press on the text area; the host has stored the pointer in `_pressedPointer`. Returns true when the event is handled.
    private bool HandlePointerPress(EditorMouseEvent mouseEvent, bool isMouse)
    {
        if (_renderer is not { } renderer)
        {
            return false;
        }

        if (TryGetMinimapPoint(mouseEvent.X, mouseEvent.Y, out double minimapX, out double minimapY)
            && renderer.MinimapPointer!.PointerDown(minimapX, minimapY, mouseEvent.LeftButton, isMouse))
        {
            FocusFromPointer();
            CapturePressedPointer();
            return true;
        }

        if (renderer.IsInStickyScroll(mouseEvent.Y))
        {
            // The sticky lines are over the text: the click acts on them and never reaches the text below.
            HandleStickyPress(mouseEvent);
            return true;
        }

        if (HandleZonePress(mouseEvent))
        {
            // A zone that suppresses the mouse down: no caret move, no selection start.
            FocusFromPointer();
            return true;
        }

        MouseTarget target = CreateMouseTarget(mouseEvent.X, mouseEvent.Y);

        // Monaco: `_onMouseDown`.
        bool targetIsContent = target.Type is MouseTargetType.ContentText or MouseTargetType.ContentEmpty;
        bool targetIsGutter = target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations;
        bool targetIsLineNumbers = target.Type == MouseTargetType.GutterLineNumbers;
        bool shouldHandle = mouseEvent.LeftButton || mouseEvent.MiddleButton;

        if (targetIsGutter && mouseEvent.LeftButton && !target.IsAfterLines && target.Position is { } viewPosition)
        {
            var args = new GutterClickedEventArgs(
                _viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition).LineNumber,
                target.Type switch
                {
                    MouseTargetType.GutterGlyphMargin => GutterArea.GlyphMargin,
                    MouseTargetType.GutterLineNumbers => GutterArea.LineNumbers,
                    _ => GutterArea.LineDecorations,
                });
            GutterClicked?.Invoke(this, args);
            if (args.Handled)
            {
                return true;
            }
        }

        bool handled = false;
        if (shouldHandle && (targetIsContent || (targetIsLineNumbers && _viewModel.CursorOptions.SelectOnLineNumbers)))
        {
            FocusFromPointer();
            CapturePressedPointer();
            _mouseDown!.Start(target.Type, mouseEvent);
            handled = true;
        }
        else if (targetIsGutter)
        {
            // Do not steal focus.
            handled = true;
        }

        // Monaco (`mouseHandler.ts`): features hear the mouse-down after the operation started.
        _context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(mouseEvent, target));
        return handled;
    }

    // A move over the control. `overScrollBar`: the pointer is over a scroll bar (or another child that keeps its own pointer).
    // Returns true when the event is handled.
    private bool HandlePointerMove(EditorMouseEvent move, bool overScrollBar)
    {
        if (_renderer?.MinimapPointer is { } minimap)
        {
            if (minimap.IsActive)
            {
                minimap.PointerMove(move.X - MinimapLeft(), move.Y);
                return true;
            }

            if (TryGetMinimapPoint(move.X, move.Y, out double x, out double y))
            {
                minimap.PointerEntered();
                minimap.PointerMove(x, y);
                SetTextAreaCursor(true);
                return false;
            }

            minimap.PointerExited();
        }

        UpdateStickyHover(move);

        if (_capturedZoneId is not null)
        {
            HandleZoneMove(move, ViewZonePointerPhase.Moved);
            return true;
        }

        if (_mouseDown is { IsActive: true })
        {
            _commentGutter?.OnPointerLeft();
            _diffGutter?.OnPointerLeft();
            _mouseDown.OnMouseDownThenMove(move);
            return true;
        }

        if (_renderer is not null && !overScrollBar)
        {
            MouseTarget target = CreateMouseTarget(move.X, move.Y);
            _context.UserInputEvents.EmitMouseMove(new EditorMouseEventArgs(move, target));
            _commentGutter?.OnPointerMoved(target, move.X, move.Y);
            _diffGutter?.OnPointerMoved(target, move.X, move.Y);
            HandleZoneMove(move, ViewZonePointerPhase.Moved);
        }

        UpdatePointerShape(move, overScrollBar);
        return false;
    }

    private void HandlePointerExit()
    {
        _context.UserInputEvents.EmitMouseLeave();

        SetMarginHovered(false);
        SetHoveredStickyLine(-1);
        _commentGutter?.OnPointerLeft();
        _diffGutter?.OnPointerLeft();
        if (_renderer?.MinimapPointer is { IsActive: false } minimapPointer)
        {
            minimapPointer.PointerExited();
        }

        if (_mouseDown is not { IsActive: true })
        {
            SetTextAreaCursor(true);
        }
    }

    // Monaco: the click listener of `StickyScrollController`; the hit parts come from the gutter geometry.
    private void HandleStickyPress(EditorMouseEvent mouseEvent)
    {
        FocusFromPointer();
        if (!mouseEvent.LeftButton || _context.StickyScrollFeature is not { } feature || _renderer is not { } renderer)
        {
            return;
        }

        int index = renderer.GetStickyLineAt(mouseEvent.Y);
        if (index < 0)
        {
            return;
        }

        EditorLayoutInfo layout = _viewModel.LayoutInfo;
        StickyScrollHitPart part = mouseEvent.X < layout.DecorationsLeft ? StickyScrollHitPart.LineNumberGutter
            : mouseEvent.X < layout.ContentLeft ? StickyScrollHitPart.FoldingIcon
            : StickyScrollHitPart.Content;
        int? column = null;
        if (part == StickyScrollHitPart.Content && renderer.GetStickyPositionAt(index, mouseEvent.X, layout) is { } viewPosition)
        {
            column = _viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition).Column;
        }

        feature.HandleClick(index, part, column, mouseEvent.ShiftKey, mouseEvent.CtrlKey || mouseEvent.AltKey || mouseEvent.MetaKey);
        InvalidateCanvas();
    }

    private void UpdateStickyHover(EditorMouseEvent move) =>
        SetHoveredStickyLine(_renderer is { } renderer && renderer.IsInStickyScroll(move.Y) ? renderer.GetStickyLineAt(move.Y) : -1);

    private void SetHoveredStickyLine(int index)
    {
        if (_renderer is { } renderer && renderer.HoveredStickyLineIndex != index)
        {
            renderer.HoveredStickyLineIndex = index;
            InvalidateCanvas();
        }
    }

    // The minimap rectangle is the one of the last frame; the point is made relative to its top-left corner.
    private bool TryGetMinimapPoint(double x, double y, out double minimapX, out double minimapY)
    {
        minimapX = minimapY = 0;
        if (_renderer is { } renderer && renderer.TryGetMinimapRectangle(out double left, out double top, out double width, out double height)
            && x >= left && x < left + width && y >= top && y < top + height)
        {
            (minimapX, minimapY) = (x - left, y - top);
            return true;
        }

        return false;
    }

    private double MinimapLeft() =>
        _renderer is { } renderer && renderer.TryGetMinimapRectangle(out double left, out _, out _, out _) ? left : 0;

    // The hit test of P3-43 on the layouts that are drawn; before the first draw there is nothing to hit.
    private MouseTarget CreateMouseTarget(double x, double y) =>
        _renderer is { } renderer
            ? MouseTargetFactory.CreateMouseTarget(_viewModel, renderer.CreateLineHitTester(_viewModel), x, y)
            : new MouseTarget(MouseTargetType.Unknown, null, 1, null, false, null);

    // An I-beam over the text, an arrow over the gutter and the scrollbars (which keep their own pointer).
    private void UpdatePointerShape(EditorMouseEvent mouseEvent, bool overScrollBar)
    {
        bool overGutter = mouseEvent.X < _viewModel.LayoutInfo.ContentLeft;
        SetMarginHovered(overGutter);
        SetTextAreaCursor(overGutter || overScrollBar);
    }

    // The folding chevrons of the gutter show while the mouse is over it.
    private void SetMarginHovered(bool hovered)
    {
        if (_renderer is { } renderer && renderer.IsMarginHovered != hovered)
        {
            renderer.IsMarginHovered = hovered;
            InvalidateCanvas();
        }
    }
}
