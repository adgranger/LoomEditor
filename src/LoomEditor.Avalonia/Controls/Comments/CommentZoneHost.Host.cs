using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LoomEditor.Core.Comments;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls.Comments;

internal sealed partial class CommentZoneHost
{
    private bool _arrangePending;

    private EditorLayoutInfo EditorLayout => _editor.ViewModel.LayoutInfo;

    /// <summary>The widget of a thread, for the tests.</summary>
    internal CommentThreadWidget? WidgetOf(CommentThread thread) =>
        _zoneIds.TryGetValue(thread, out string? id) && _widgets.TryGetValue(id, out CommentThreadWidget? widget) ? widget : null;

    /// <summary>True when the element is inside the zone-widget layer.</summary>
    public bool Contains(Visual? element) => _layer.Contains(element);

    /// <summary>Arranges the widgets right after the current frame: Avalonia refuses a layout change during the render pass.</summary>
    public void ArrangeAfterDraw()
    {
        if (_arrangePending || _widgets.Count == 0)
        {
            return;
        }

        _arrangePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _arrangePending = false;
            if (!_disposed)
            {
                Arrange();
            }
        });
    }

    private static bool IsShown(CommentThreadWidget widget) => widget.IsVisible;

    private static double GetLeft(CommentThreadWidget widget) => Canvas.GetLeft(widget);

    private static double GetTop(CommentThreadWidget widget) => Canvas.GetTop(widget);

    private IEnumerable<ViewWhitespaceViewportData> GetWhitespaceViewportData() => _editor.ViewModel.ViewLayout.GetWhitespaceViewportData();

    private void InvalidateEditor() => _editor.InvalidateComments();

    private void FocusEditor() => _editor.Focus();

    private CommentThreadWidget CreateWidget(CommentThread thread) =>
        new(_controller.GetViewModel(thread)) { IsVisible = false };

    private void SubscribeSize(CommentThreadWidget widget) => widget.PropertyChanged += OnWidgetPropertyChanged;

    private void UnsubscribeSize(CommentThreadWidget widget) => widget.PropertyChanged -= OnWidgetPropertyChanged;

    private void OnWidgetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // The twin of SizeChanged.
        if (e.Property == Visual.BoundsProperty)
        {
            MarkDirty((CommentThreadWidget)sender!);
        }
    }
}
