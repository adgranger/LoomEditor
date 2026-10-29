using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls.Comments;

// The zone height follows the widget: a trigger marks the widget dirty and schedules one coalesced pass on the dispatcher (the
// scheduling of DiffEditorViewZones); the pass measures the dirty widgets and resizes their zones. Never measured in Arrange (the draw).
internal sealed partial class CommentZoneHost
{
    // A new zone height can change the content width (the vertical scroll bar appears) and so the measured heights: a few passes settle it.
    private const int MaxHeightPasses = 3;

    private readonly HashSet<CommentThreadWidget> _dirty = [];
    private bool _heightPassPending;
    private bool _disposed;
    private int _heightPasses;
    private int _heightLimitHits;

    /// <summary>Gives every widget the editor's current font and measures them again.</summary>
    public void OnFontChanged()
    {
        foreach (CommentThreadWidget widget in _widgets.Values)
        {
            widget.ApplyFont(_editor.FontSize, _editor.FontFamily);
        }

        MarkAllDirty();
    }

    /// <summary>Marks every widget for a new measure (theme or font change).</summary>
    public void MarkAllDirty()
    {
        foreach (CommentThreadWidget widget in _widgets.Values)
        {
            MarkDirty(widget);
        }
    }

    private static double BoxWidth(EditorLayoutInfo layout) => Math.Max(0, layout.ContentWidth - (2 * HorizontalInset));

    private void MarkDirty(CommentThreadWidget widget)
    {
        _dirty.Add(widget);
        ScheduleHeightPass();
    }

    private void ScheduleHeightPass()
    {
        if (_heightPassPending || _disposed)
        {
            return;
        }

        _heightPassPending = _editor.DispatcherQueue.TryEnqueue(() =>
        {
            _heightPassPending = false;
            RunHeightPasses();
        });
    }

    private void RunHeightPasses()
    {
        if (_disposed)
        {
            return;
        }

        // A pass that changes no height ends the loop.
        for (int pass = 0; pass < MaxHeightPasses; pass++)
        {
            if (!RunHeightPass())
            {
                return;
            }
        }

        // A fourth pass would be needed: stop here; the widgets stay dirty until the next trigger.
        if (HasVisibleDirty())
        {
            _heightLimitHits++;
        }
    }

    // Returns true when a zone height changed and a dirty widget remains to measure.
    private bool RunHeightPass()
    {
        if (!HasVisibleDirty())
        {
            return false;
        }

        _heightPasses++;
        double width = BoxWidth(_editor.LayoutInfo);
        bool changed = false;
        foreach (CommentThreadWidget widget in _dirty.ToArray())
        {
            // A collapsed (off-screen) widget keeps its zone's last height; it is measured once it shows (its SizeChanged marks it again).
            if (widget.Visibility != Visibility.Visible)
            {
                continue;
            }

            _dirty.Remove(widget);
            int target = widget.MeasureContentHeight(width);
            if (target != _controller.Manager.GetHeight(widget.Thread))
            {
                _controller.Manager.SetHeight(widget.Thread, target);
                changed = true;
            }
        }

        if (changed && BoxWidth(_editor.LayoutInfo) != width)
        {
            MarkAllVisibleDirtyWithoutScheduling();
        }

        return changed && HasVisibleDirty();
    }

    private bool HasVisibleDirty()
    {
        foreach (CommentThreadWidget widget in _dirty)
        {
            if (widget.Visibility == Visibility.Visible)
            {
                return true;
            }
        }

        return false;
    }

    private void MarkAllVisibleDirtyWithoutScheduling()
    {
        foreach (CommentThreadWidget widget in _widgets.Values)
        {
            if (widget.Visibility == Visibility.Visible)
            {
                _dirty.Add(widget);
            }
        }
    }

    private void OnWidgetSizeChanged(object sender, SizeChangedEventArgs e) => MarkDirty((CommentThreadWidget)sender);

    private void OnWidgetContentChanged(object? sender, EventArgs e) => MarkDirty((CommentThreadWidget)sender!);

    private void OnWidgetFocusReturnRequested(object? sender, EventArgs e) => ReturnFocusToEditor(((CommentThreadWidget)sender!).Thread);

    private void Unsubscribe(CommentThreadWidget widget)
    {
        widget.SizeChanged -= OnWidgetSizeChanged;
        widget.FocusReturnRequested -= OnWidgetFocusReturnRequested;
        widget.ContentChanged -= OnWidgetContentChanged;
    }
}
