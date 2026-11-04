using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;

namespace LoomEditor.Avalonia.Controls;

// The Avalonia pointer events: they build the `EditorMouseEvent`, call the shared decisions and mark the event handled.
public sealed partial class LoomEditor
{
    private Cursor? _textCursor;
    private Cursor? _arrowCursor;
    private IPointer? _capturedPointer;

    // The pointer of the press being handled, for the minimap, the text and a zone that ask for the capture.
    private IPointer? _pressedPointer;

    /// <summary>Identifies the <see cref="DragAndDrop"/> styled property.</summary>
    public static readonly StyledProperty<bool> DragAndDropProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(DragAndDrop), true);

    /// <summary>
    /// Whether a press inside a non-empty selection starts a drag of the selection (Monaco: <c>dragAndDrop</c>). When
    /// <c>false</c>, such a press collapses the selection as any click. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool DragAndDrop
    {
        get => GetValue(DragAndDropProperty);
        set => SetValue(DragAndDropProperty, value);
    }

    private static void InitializePointerProperties() =>
        DragAndDropProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) =>
            editor._viewModel.SetCursorOptions(editor._viewModel.CursorOptions with { DragAndDrop = (bool)e.NewValue! }));

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);

        if (_renderer is null || IsFromChildControl(e.Source))
        {
            return;
        }

        _pressedPointer = e.Pointer;
        if (HandlePointerPress(PointerInput.Create(e, this, e.ClickCount), e.Pointer.Type == PointerType.Mouse))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);

        if (HandlePointerMove(PointerInput.Create(e, this, 1), IsFromChildControl(e.Source)))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);

        EditorMouseEvent release = PointerInput.Create(e, this, 1);
        EditorMouseEvent? mouseUp = null;
        if (_renderer is not null && !IsFromChildControl(e.Source))
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
            ReleaseCapture();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerCaptureLost(e);

        _capturedPointer = null;
        EndZoneCapture();
        _renderer?.MinimapPointer?.PointerUp();
        _mouseDown?.OnPointerUp();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerExited(e);
        HandlePointerExit();
    }

    private void FocusFromPointer() => Focus(NavigationMethod.Pointer);

    private void CapturePressedPointer() => Capture(_pressedPointer!);

    private void Capture(IPointer pointer)
    {
        pointer.Capture(this);
        _capturedPointer = pointer;
    }

    private void ReleaseCapture()
    {
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
    }

    private void SetTextAreaCursor(bool arrow) => Cursor = CreateTextAreaCursor(arrow);

    // The mouse style is `Text` outside a drag of the selection. `Copy` is an arrow too, as in the WinUI control.
    // shortcut: the two cursors are created once and not disposed (they are tiny platform handles); dispose them when the control leaves the tree if that ever matters.
    private Cursor CreateTextAreaCursor(bool arrow) =>
        arrow || _context.MouseStyle != EditorMouseStyle.Text
            ? _arrowCursor ??= new Cursor(StandardCursorType.Arrow)
            : _textCursor ??= new Cursor(StandardCursorType.Ibeam);

    // The scrollbars and the find widget are children of the control: their pointer events are not the text's.
    private bool IsFromChildControl(object? source) => IsFromScrollBar(source) || IsFromFindWidget(source);

    private bool IsFromScrollBar(object? source)
    {
        for (var element = source as Visual; element is not null && !ReferenceEquals(element, this); element = element.GetVisualParent())
        {
            if (element is ScrollBar)
            {
                return true;
            }
        }

        return false;
    }
}
