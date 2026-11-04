// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorSash.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

/// <summary>
/// The draggable divider between the two editors (Monaco: <c>DiffEditorSash</c>): a transparent hit area with a 1 DIP line.
/// Dragging raises <see cref="SashMoved"/> with the wanted x of the sash, a double click raises <see cref="ResetRequested"/>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffEditorSash : Panel
{
    /// <summary>The width of the hit area, in DIPs.</summary>
    public const double HitWidth = 5;

    private readonly Border _line = new() { Width = 1, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center };
    private double _startPointerX;
    private double _startSashLeft;
    private bool _isSplitEnabled = true;
    private bool _isDragging;

    /// <summary>Creates the sash.</summary>
    public DiffEditorSash()
    {
        Width = HitWidth;
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.SizeWestEast);
        Children.Add(_line);
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
        get => _isSplitEnabled;
        set
        {
            _isSplitEnabled = value;
            Cursor = value ? new Cursor(StandardCursorType.SizeWestEast) : null;
        }
    }

    /// <summary>The color of the line; <c>null</c> hides it (the theme has no <c>diffEditor.border</c>).</summary>
    public IBrush? LineBrush
    {
        set => _line.Background = value;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsSplitEnabled || this.GetVisualParent() is not Visual parent || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        _isDragging = true;
        e.Pointer.Capture(this);
        _startPointerX = e.GetPosition(parent).X;
        _startSashLeft = GetSashLeft();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging && this.GetVisualParent() is Visual parent)
        {
            SashMoved?.Invoke(this, _startSashLeft + (e.GetPosition(parent).X - _startPointerX));
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }
}
