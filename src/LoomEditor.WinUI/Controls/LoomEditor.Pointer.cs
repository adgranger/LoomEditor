using LoomEditor.View.Controller;
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

    private void InitializePointer()
    {
        _mouseDown = new MouseDownOperation(
            _viewModel, new ViewController(_viewModel), CreateMouseTarget, ScheduleAtNextAnimationFrame, TimeProvider.System);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);

        if (_renderer is null || IsFromScrollBar(e.OriginalSource) || IsFromFindWidget(e.OriginalSource))
        {
            return;
        }

        EditorMouseEvent mouseEvent = PointerInput.Create(e, this, PointerInput.PressDetail);
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
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);

        if (IsFromFindWidget(e.OriginalSource))
        {
            return;
        }

        if (_mouseDown is { IsActive: true })
        {
            _mouseDown.OnMouseDownThenMove(PointerInput.Create(e, this, 1));
            e.Handled = true;
            return;
        }

        UpdatePointerShape(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);

        if (IsFromFindWidget(e.OriginalSource))
        {
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

        _mouseDown?.OnPointerUp();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerExited(e);

        SetMarginHovered(false);
        if (_mouseDown is not { IsActive: true })
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

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
