using LoomEditor.View.Controller;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

// Mouse: WinUI pointer events drive the pure logic of `MouseDownOperation` (P3-43 to P3-45). Touch and pen are treated like
// the left mouse button (no gestures in phase 3).
public sealed partial class LoomEditor
{
    private MouseDownOperation? _mouseDown;

    /// <summary>Identifies the <see cref="DragAndDrop"/> dependency property.</summary>
    public static readonly DependencyProperty DragAndDropProperty = DependencyProperty.Register(
        nameof(DragAndDrop), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnDragAndDropChanged));

    /// <summary>
    /// Whether a press inside a non-empty selection starts a drag of the selection (Monaco: <c>dragAndDrop</c>). When
    /// <c>false</c>, such a press collapses the selection as any click. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool DragAndDrop
    {
        get => (bool)GetValue(DragAndDropProperty);
        set => SetValue(DragAndDropProperty, value);
    }

    private static void OnDragAndDropChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        editor._viewModel.SetCursorOptions(editor._viewModel.CursorOptions with { DragAndDrop = (bool)e.NewValue });
    }

    private void InitializePointer()
    {
        _mouseDown = new MouseDownOperation(
            _viewModel, new ViewController(_viewModel, _context.UserInputEvents), CreateMouseTarget, ScheduleAtNextAnimationFrame, TimeProvider.System);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);

        if (_renderer is null || IsFromScrollBar(e.OriginalSource) || IsFromFindWidget(e.OriginalSource) || IsFromOverlay(e.OriginalSource))
        {
            return;
        }

        EditorMouseEvent mouseEvent = PointerInput.Create(e, this, PointerInput.PressDetail);
        if (TryGetMinimapPoint(mouseEvent.X, mouseEvent.Y, out double minimapX, out double minimapY)
            && _renderer.MinimapPointer!.PointerDown(
                minimapX, minimapY, mouseEvent.LeftButton, e.Pointer.PointerDeviceType == PointerDeviceType.Mouse))
        {
            Focus(FocusState.Pointer);
            CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        if (_renderer.IsInStickyScroll(mouseEvent.Y))
        {
            // The sticky lines are over the text: the click acts on them and never reaches the text below.
            HandleStickyPress(mouseEvent, e);
            e.Handled = true;
            return;
        }

        if (HandleZonePress(mouseEvent, e.Pointer))
        {
            // A zone that suppresses the mouse down: no caret move, no selection start.
            Focus(FocusState.Pointer);
            e.Handled = true;
            return;
        }

        MouseTarget target = CreateMouseTarget(mouseEvent.X, mouseEvent.Y);

        // Monaco: `_onMouseDown`.
        bool targetIsContent = target.Type is MouseTargetType.ContentText or MouseTargetType.ContentEmpty;
        bool targetIsGutter = target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations;
        bool targetIsLineNumbers = target.Type == MouseTargetType.GutterLineNumbers;
        bool shouldHandle = mouseEvent.LeftButton || mouseEvent.MiddleButton;

        if (shouldHandle && (targetIsContent || (targetIsLineNumbers && _viewModel.CursorOptions.SelectOnLineNumbers)))
        {
            Focus(FocusState.Pointer);
            CapturePointer(e.Pointer);
            _mouseDown!.Start(target.Type, mouseEvent);
            e.Handled = true;
        }
        else if (targetIsGutter)
        {
            // Do not steal focus.
            e.Handled = true;
        }

        // Monaco (`mouseHandler.ts`): features hear the mouse-down after the operation started.
        _context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(mouseEvent, target));
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);

        if (IsFromFindWidget(e.OriginalSource) || IsFromOverlay(e.OriginalSource))
        {
            return;
        }

        if (_renderer?.MinimapPointer is { } minimap)
        {
            EditorMouseEvent move = PointerInput.Create(e, this, 1);
            if (minimap.IsActive)
            {
                minimap.PointerMove(move.X - MinimapLeft(), move.Y);
                e.Handled = true;
                return;
            }

            if (TryGetMinimapPoint(move.X, move.Y, out double x, out double y))
            {
                minimap.PointerEntered();
                minimap.PointerMove(x, y);
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                return;
            }

            minimap.PointerExited();
        }

        UpdateStickyHover(PointerInput.Create(e, this, 1));

        if (_capturedZoneId is not null)
        {
            HandleZoneMove(PointerInput.Create(e, this, 1), ViewZonePointerPhase.Moved);
            e.Handled = true;
            return;
        }

        if (_mouseDown is { IsActive: true })
        {
            _mouseDown.OnMouseDownThenMove(PointerInput.Create(e, this, 1));
            e.Handled = true;
            return;
        }

        if (_renderer is not null && !IsFromScrollBar(e.OriginalSource))
        {
            EditorMouseEvent mouseEvent = PointerInput.Create(e, this, 1);
            _context.UserInputEvents.EmitMouseMove(new EditorMouseEventArgs(mouseEvent, CreateMouseTarget(mouseEvent.X, mouseEvent.Y)));
            HandleZoneMove(mouseEvent, ViewZonePointerPhase.Moved);
        }

        UpdatePointerShape(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);

        if (IsFromFindWidget(e.OriginalSource) || IsFromOverlay(e.OriginalSource))
        {
            return;
        }

        if (_renderer is not null && !IsFromScrollBar(e.OriginalSource))
        {
            // The buttons are already up in a release: the released one is the button of the event.
            PointerUpdateKind kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            EditorMouseEvent mouseEvent = PointerInput.Create(e, this, 1) with
            {
                LeftButton = kind == PointerUpdateKind.LeftButtonReleased,
                MiddleButton = kind == PointerUpdateKind.MiddleButtonReleased,
            };
            _context.UserInputEvents.EmitMouseUp(new EditorMouseEventArgs(mouseEvent, CreateMouseTarget(mouseEvent.X, mouseEvent.Y)));
        }

        if (_capturedZoneId is not null)
        {
            HandleZoneMove(PointerInput.Create(e, this, 1), ViewZonePointerPhase.Released);
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
            return;
        }

        if (_renderer?.MinimapPointer is { IsActive: true } minimapDrag)
        {
            minimapDrag.PointerUp();
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
            return;
        }

        if (_mouseDown is { IsActive: true })
        {
            _mouseDown.OnPointerUp();
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerCaptureLost(e);

        EndZoneCapture();
        _renderer?.MinimapPointer?.PointerUp();
        _mouseDown?.OnPointerUp();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerExited(e);
        _context.UserInputEvents.EmitMouseLeave();

        SetMarginHovered(false);
        SetHoveredStickyLine(-1);
        if (_renderer?.MinimapPointer is { IsActive: false } minimapPointer)
        {
            minimapPointer.PointerExited();
        }

        if (_mouseDown is not { IsActive: true })
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    // Monaco: the click listener of `StickyScrollController`; the hit parts come from the gutter geometry.
    private void HandleStickyPress(EditorMouseEvent mouseEvent, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
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
        _canvas?.Invalidate();
    }

    private void UpdateStickyHover(EditorMouseEvent move) =>
        SetHoveredStickyLine(_renderer is { } renderer && renderer.IsInStickyScroll(move.Y) ? renderer.GetStickyLineAt(move.Y) : -1);

    private void SetHoveredStickyLine(int index)
    {
        if (_renderer is { } renderer && renderer.HoveredStickyLineIndex != index)
        {
            renderer.HoveredStickyLineIndex = index;
            _canvas?.Invalidate();
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
    private void UpdatePointerShape(PointerRoutedEventArgs e)
    {
        EditorMouseEvent mouseEvent = PointerInput.Create(e, this, 1);
        bool overGutter = mouseEvent.X < _viewModel.LayoutInfo.ContentLeft;
        bool overScrollBar = IsFromScrollBar(e.OriginalSource);
        SetMarginHovered(overGutter);
        ProtectedCursor = InputSystemCursor.Create(
            overGutter || overScrollBar ? InputSystemCursorShape.Arrow : InputSystemCursorShape.IBeam);
    }

    // The folding chevrons of the gutter show while the mouse is over it.
    private void SetMarginHovered(bool hovered)
    {
        if (_renderer is { } renderer && renderer.IsMarginHovered != hovered)
        {
            renderer.IsMarginHovered = hovered;
            _canvas?.Invalidate();
        }
    }

    // The find widget is a child of the control: its pointer events must not reach the text.
    private bool IsFromFindWidget(object? source)
    {
        var element = source as DependencyObject;
        while (element is not null && !ReferenceEquals(element, this))
        {
            if (ReferenceEquals(element, _findWidget))
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    // The widgets of the overlay layer are children of the control: their pointer events must not reach the text.
    private bool IsFromOverlay(object? source) => _overlay?.Contains(source as DependencyObject) == true;

    private bool IsFromScrollBar(object? source)
    {
        var element = source as DependencyObject;
        while (element is not null && !ReferenceEquals(element, this))
        {
            if (element is ScrollBar)
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }
}
