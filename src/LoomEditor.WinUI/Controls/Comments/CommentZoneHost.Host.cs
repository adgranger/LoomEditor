using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Comments;

internal sealed partial class CommentZoneHost
{
    private EditorLayoutInfo EditorLayout => _editor.LayoutInfo;

    /// <summary>True when the element is inside the zone-widget layer.</summary>
    public bool Contains(DependencyObject? element) => _layer.Contains(element);

    private static bool IsShown(CommentThreadWidget widget) => widget.Visibility == Visibility.Visible;

    private static bool IsPlaced(CommentThreadWidget widget, double left, double top, double width, double height) =>
        Canvas.GetLeft(widget) == left && Canvas.GetTop(widget) == top && widget.Width == width && widget.Height == height;

    private static bool HasWidth(CommentThreadWidget widget, double width) => widget.Width == width;

    private IEnumerable<ViewWhitespaceViewportData> GetWhitespaceViewportData() => _editor.GetWhitespaceViewportData();

    private void InvalidateEditor() => _editor.InvalidateCanvas();

    private void FocusEditor() => _editor.Focus(FocusState.Programmatic);

    private CommentThreadWidget CreateWidget(Core.Comments.CommentThread thread) =>
        new(_controller.GetViewModel(thread)) { Visibility = Visibility.Collapsed };

    private void SubscribeSize(CommentThreadWidget widget) => widget.SizeChanged += OnWidgetSizeChanged;

    private void UnsubscribeSize(CommentThreadWidget widget) => widget.SizeChanged -= OnWidgetSizeChanged;

    private void OnWidgetSizeChanged(object sender, SizeChangedEventArgs e) => MarkDirty((CommentThreadWidget)sender);
}
