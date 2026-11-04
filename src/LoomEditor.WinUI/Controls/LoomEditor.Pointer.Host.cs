using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.WinUI.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls;

// The WinUI pointer events: they build the `EditorMouseEvent`, call the shared decisions and mark the event handled.
public sealed partial class LoomEditor
{
    // The pointer of the press being handled, for the minimap, the text and a zone that ask for the capture.
    private Pointer? _pressedPointer;

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

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);

        if (_renderer is null || IsFromScrollBar(e.OriginalSource) || IsFromFindWidget(e.OriginalSource) || IsFromOverlay(e.OriginalSource))
        {
            return;
        }

        _pressedPointer = e.Pointer;
        if (HandlePointerPress(PointerInput.Create(e, this, PointerInput.PressDetail), e.Pointer.PointerDeviceType == PointerDeviceType.Mouse))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);

        if (IsFromFindWidget(e.OriginalSource) || IsFromOverlay(e.OriginalSource))
        {
            // The widgets inherit this control's cursor: without this they keep the I-beam of the text.
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            return;
        }

        if (HandlePointerMove(PointerInput.Create(e, this, 1), IsFromScrollBar(e.OriginalSource)))
        {
            e.Handled = true;
        }
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

        EditorMouseEvent release = PointerInput.Create(e, this, 1);
        EditorMouseEvent? mouseUp = null;
        if (_renderer is not null && !IsFromScrollBar(e.OriginalSource))
        {
            // The buttons are already up in a release: the released one is the button of the event.
            PointerUpdateKind kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            mouseUp = release with
            {
                LeftButton = kind == PointerUpdateKind.LeftButtonReleased,
                MiddleButton = kind == PointerUpdateKind.MiddleButtonReleased,
            };
        }

        if (HandlePointerRelease(release, mouseUp))
        {
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
        HandlePointerExit();
    }

    private void FocusFromPointer() => Focus(FocusState.Pointer);

    private void CapturePressedPointer() => CapturePointer(_pressedPointer!);

    private void SetTextAreaCursor(bool arrow) => ProtectedCursor = CreateTextAreaCursor(arrow);

    // The mouse style is `Text` outside a drag of the selection. `Copy` is an arrow too: WinUI 3 has no built-in copy cursor.
    private InputSystemCursor CreateTextAreaCursor(bool arrow) =>
        InputSystemCursor.Create(
            arrow || _context.MouseStyle != EditorMouseStyle.Text ? InputSystemCursorShape.Arrow : InputSystemCursorShape.IBeam);

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
    private bool IsFromOverlay(object? source) => _overlay?.Contains(source as DependencyObject) == true || _commentZoneHost?.Contains(source as DependencyObject) == true
        || _hostZoneLayer.Contains(source as DependencyObject) == true;

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
