using System.Diagnostics;
using System.Globalization;
using LoomEditor.Core.Comments;
using LoomEditor.Themes;
using LoomEditor.View.Comments;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>Keeps one <see cref="CommentThreadWidget"/> over the <c>Widget</c> view zone of every thread, positioned in the draw pass.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentZoneHost : IDisposable
{
    // The box is inset from the content edges so it does not touch the line numbers or the scroll bar.
    private const int HorizontalInset = 4;

    private readonly LoomEditor _editor;
    private readonly OverlayHost _layer;
    private readonly CommentThreadsController _controller;
    private readonly Dictionary<string, CommentThreadWidget> _widgets = [];
    private readonly Dictionary<CommentThread, string> _zoneIds = [];
    private readonly HashSet<string> _seen = [];
    private readonly Stopwatch _stopwatch = new();
    private EditorTheme? _theme;
    private int _visible;
    private double _lastArrangeMilliseconds;

    public CommentZoneHost(LoomEditor editor, OverlayHost layer, CommentThreadsController controller)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(controller);
        _editor = editor;
        _layer = layer;
        _controller = controller;
        foreach (CommentThread thread in controller.Threads)
        {
            Add(thread);
        }

        controller.ThreadAdded += OnThreadAdded;
        controller.ThreadRemoving += OnThreadRemoving;
    }

    /// <summary>A short status line for the Gallery's manual test.</summary>
    public string Diagnostics => string.Create(CultureInfo.InvariantCulture, $"zones {_widgets.Count} visible {_visible} lastArrange {_lastArrangeMilliseconds:0.00} ms heightPasses {_heightPasses} heightLimitHits {_heightLimitHits}");

    /// <summary>True when the element is inside the zone-widget layer.</summary>
    public bool Contains(DependencyObject? element) => _layer.Contains(element);

    /// <summary>Applies the theme to every widget, and to the ones created later.</summary>
    public void ApplyTheme(EditorTheme theme)
    {
        _theme = theme;
        foreach (CommentThreadWidget widget in _widgets.Values)
        {
            widget.ApplyTheme(theme);
        }

        MarkAllDirty();
    }

    /// <summary>Puts every widget over its zone, or collapses it when its zone is not in the viewport. Called right after the text is drawn.</summary>
    public void Arrange()
    {
        _stopwatch.Restart();
        _seen.Clear();
        _visible = 0;
        EditorLayoutInfo layout = _editor.LayoutInfo;
        double scrollTop = _editor.ScrollTop;
        foreach (ViewWhitespaceViewportData zone in _editor.GetWhitespaceViewportData())
        {
            if (zone.Height < 1 || !_widgets.TryGetValue(zone.Id, out CommentThreadWidget? widget))
            {
                continue;
            }

            _seen.Add(zone.Id);
            _visible++;
            double left = layout.ContentLeft + HorizontalInset;
            double top = zone.VerticalOffset - scrollTop;
            double width = BoxWidth(layout);
            if (widget.Visibility != Visibility.Visible || Canvas.GetLeft(widget) != left || Canvas.GetTop(widget) != top
                || widget.Width != width || widget.Height != zone.Height)
            {
                if (widget.Width != width)
                {
                    MarkDirty(widget);
                }

                widget.Width = width;
                widget.Height = zone.Height;
                _layer.Show(widget, new OverlayRect(left, top, width, zone.Height));
            }
        }

        foreach ((string id, CommentThreadWidget widget) in _widgets)
        {
            if (!_seen.Contains(id) && widget.Visibility != Visibility.Collapsed)
            {
                OverlayHost.Hide(widget);
            }
        }

        _lastArrangeMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>Stops listening and releases the widgets.</summary>
    public void Dispose()
    {
        _disposed = true;
        _controller.ThreadAdded -= OnThreadAdded;
        _controller.ThreadRemoving -= OnThreadRemoving;
        foreach (CommentThreadWidget widget in _widgets.Values)
        {
            Unsubscribe(widget);
            widget.Detach();
            _layer.Remove(widget);
        }

        _widgets.Clear();
        _zoneIds.Clear();
        _dirty.Clear();
    }

    private void OnThreadAdded(object? sender, CommentThread thread)
    {
        Add(thread);
        _editor.InvalidateCanvas();
    }

    /// <summary>Moves the focus into the thread's composer (once its widget is loaded).</summary>
    public void FocusComposer(CommentThread thread)
    {
        if (_zoneIds.TryGetValue(thread, out string? id) && _widgets.TryGetValue(id, out CommentThreadWidget? widget))
        {
            widget.FocusComposer();
        }
    }

    /// <summary>Gives the focus back to the editor with the cursor at the start of the thread's line, revealed only if it is off screen.</summary>
    public void ReturnFocusToEditor(CommentThread thread)
    {
        _editor.Focus(FocusState.Programmatic);
        int line = Math.Clamp(thread.LineNumber, 1, _editor.Model.LineCount);
        _editor.SetSelections((Core.Text.Selection[])[new Core.Text.Selection(line, 1, line, 1)]);
        _editor.RevealRange(new Core.Text.Range(line, 1, line, 1));
    }

    private void OnThreadRemoving(object? sender, CommentThread thread)
    {
        if (_zoneIds.Remove(thread, out string? id) && _widgets.Remove(id, out CommentThreadWidget? widget))
        {
            // Before the element leaves the tree: a removed focused element would leave the focus on the window.
            if (widget.HasFocusWithin())
            {
                ReturnFocusToEditor(thread);
            }

            Unsubscribe(widget);
            _dirty.Remove(widget);
            widget.Detach();
            _layer.Remove(widget);
        }
    }

    private void Add(CommentThread thread)
    {
        if (_controller.Manager.GetZoneId(thread) is not { } id)
        {
            return;
        }

        var widget = new CommentThreadWidget(_controller.GetViewModel(thread)) { Visibility = Visibility.Collapsed };
        if (_theme is not null)
        {
            widget.ApplyTheme(_theme);
        }

        _widgets[id] = widget;
        _zoneIds[thread] = id;
        widget.SizeChanged += OnWidgetSizeChanged;
        widget.FocusReturnRequested += OnWidgetFocusReturnRequested;
        widget.ContentChanged += OnWidgetContentChanged;
        MarkDirty(widget);
    }
}
