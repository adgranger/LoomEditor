// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorSash.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// The draggable divider between the two editors (Monaco: <c>DiffEditorSash</c>): a transparent hit area with a 1 DIP line.
/// Dragging raises <see cref="SashMoved"/> with the wanted x of the sash, a double click raises <see cref="ResetRequested"/>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class DiffEditorSash : Grid
{
    /// <summary>The width of the hit area, in DIPs.</summary>
    public const double HitWidth = 5;

    private readonly Border _line = new() { Width = 1, HorizontalAlignment = HorizontalAlignment.Center };
    private double _startPointerX;
    private double _startSashLeft;
    private bool _isDragging;

    /// <summary>Creates the sash.</summary>
    public DiffEditorSash()
    {
        Width = HitWidth;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        Children.Add(_line);
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += (_, _) => _isDragging = false;
        DoubleTapped += (_, _) => ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised while dragging, with the wanted x of the sash.</summary>
    public event EventHandler<double>? SashMoved;

    /// <summary>Raised on a double click.</summary>
    public event EventHandler? ResetRequested;

    /// <summary>Gives the current x of the sash when a drag starts.</summary>
    public Func<double> GetSashLeft { get; set; } = static () => 0;

    /// <summary>Whether the sash reacts to the pointer (Monaco: <c>enableSplitViewResizing</c>).</summary>
    public bool IsSplitEnabled
    {
        get;
        set
        {
            field = value;
            ProtectedCursor = value ? InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast) : null;
        }
    } = true;

    /// <summary>The color of the line; <c>null</c> hides it (the theme has no <c>diffEditor.border</c>).</summary>
    public Brush? LineBrush
    {
        set => _line.Background = value;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsSplitEnabled || Parent is not UIElement parent || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isDragging = CapturePointer(e.Pointer);
        _startPointerX = e.GetCurrentPoint(parent).Position.X;
        _startSashLeft = GetSashLeft();
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && Parent is UIElement parent)
        {
            SashMoved?.Invoke(this, _startSashLeft + (e.GetCurrentPoint(parent).Position.X - _startPointerX));
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndDrag(e.Pointer);
    }

    private void EndDrag(Microsoft.UI.Xaml.Input.Pointer pointer)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleasePointerCapture(pointer);
        }
    }
}
