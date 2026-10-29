using LoomEditor.Core.Comments;
using LoomEditor.Themes;
using LoomEditor.View.Comments;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>
/// Shows the gutter "+" (<see cref="CommentGutterButton"/>) on the model line under the pointer while the pointer is over the gutter or the
/// button, and asks the controller for a thread on click (ADR 0014: the button never adds one itself).
/// </summary>
/// <remarks>
/// The line comes from the pointer code's own hit test (<see cref="MouseTarget"/>); the gutter targets carry the model line of the hovered row,
/// so a wrapped line shows the button at its first row. Threading: UI thread only.
/// </remarks>
internal sealed class CommentGutterController : IDisposable
{
    private readonly LoomEditor _editor;
    private readonly OverlayHost _layer;
    private readonly CommentThreadsController _controller;
    private readonly Func<double, double, MouseTarget> _hitTest;
    private readonly CommentGutterButton _button = new();
    private bool _enabled;
    private bool _overGutter;
    private bool _overButton;
    private double _pointerX;
    private double _pointerY;
    private int _hitLine;
    private int _candidateLine;
    private int _shownLine;

    public CommentGutterController(LoomEditor editor, OverlayHost layer, CommentThreadsController controller, Func<double, double, MouseTarget> hitTest)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(hitTest);
        _editor = editor;
        _layer = layer;
        _controller = controller;
        _hitTest = hitTest;
        _button.Click += OnClick;
        _button.PointerEntered += OnButtonPointerEntered;
        _button.PointerExited += OnButtonPointerExited;
        _controller.ThreadAdded += OnThreadsChanged;
        _controller.ThreadRemoving += OnThreadsChanged;
    }

    /// <summary>The <c>CommentGutterButtonEnabled</c> property; <see langword="false"/> hides the button.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Update();
        }
    }

    /// <summary>The pointer moved over the editor (not a drag); <paramref name="target"/> is the hit test the pointer code already made.</summary>
    public void OnPointerMoved(MouseTarget target, double x, double y)
    {
        _pointerX = x;
        _pointerY = y;
        _overGutter = x < _editor.LayoutInfo.ContentLeft;
        _hitLine = LineOf(target);
        Update();
    }

    /// <summary>The pointer left the editor, or a drag selection started.</summary>
    public void OnPointerLeft()
    {
        _overGutter = false;
        Update();
    }

    /// <summary>Called after each draw: a scroll under a still pointer changes the hovered line and the button's place.</summary>
    public void Arrange()
    {
        if (_overGutter && _enabled)
        {
            _hitLine = LineOf(_hitTest(_pointerX, _pointerY));
        }

        Update();
    }

    /// <summary>Applies the button colors.</summary>
    public void ApplyTheme(EditorTheme theme) => _button.ApplyTheme(theme);

    public void Dispose()
    {
        _button.Click -= OnClick;
        _button.PointerEntered -= OnButtonPointerEntered;
        _button.PointerExited -= OnButtonPointerExited;
        _controller.ThreadAdded -= OnThreadsChanged;
        _controller.ThreadRemoving -= OnThreadsChanged;
        _layer.Remove(_button);
    }

    private static int LineOf(MouseTarget target) =>
        target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations
        && !target.IsAfterLines && target.Position is { } position
            ? position.LineNumber
            : 0;

    private void Update()
    {
        // Over the button itself it stays where it is (the pointer left the gutter for the button).
        int line = !_enabled ? 0 : _overButton ? _candidateLine : _overGutter ? _hitLine : 0;
        if (line != _candidateLine)
        {
            // One box per line from the gutter; looked up only when the hovered line changes, not on every move.
            _candidateLine = line;
            _shownLine = line > 0 && !HasThreadOn(line) ? line : 0;
        }

        if (_shownLine == 0)
        {
            OverlayHost.Hide(_button);
            return;
        }

        EditorLayoutInfo layout = _editor.LayoutInfo;
        double top = _editor.GetTopForLineNumber(_shownLine) - _editor.ScrollTop + ((_editor.LineHeight - CommentGutterButton.Size) / 2);
        _button.SetLine(_shownLine);
        _layer.Show(_button, new OverlayRect(layout.LineNumbersLeft, top, CommentGutterButton.Size, CommentGutterButton.Size));
    }

    private bool HasThreadOn(int line)
    {
        foreach (CommentThread thread in _controller.Threads)
        {
            if (thread.LineNumber == line)
            {
                return true;
            }
        }

        return false;
    }

    private void OnThreadsChanged(object? sender, CommentThread thread)
    {
        _candidateLine = -1;
        Update();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (_shownLine > 0)
        {
            _controller.RequestAdd(_shownLine);
        }
    }

    private void OnButtonPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _overButton = true;
        Update();
    }

    private void OnButtonPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _overButton = false;
        Update();
    }
}
