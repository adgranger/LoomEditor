using LoomEditor.Themes;
using LoomEditor.View.Comments;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>The box that covers the view zone of one comment thread: a placeholder (title and plain-text comments) until the real widget replaces its body.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentThreadWidget : UserControl
{
    private readonly CommentThreadViewModel _viewModel;
    private readonly Border _border = new();
    private readonly StackPanel _panel = new() { Spacing = 4 };
    private readonly StackPanel _comments = new() { Spacing = 4 };
    private readonly SolidColorBrush _foreground = new();

    /// <summary>Creates the widget and fills it from the view model; it rebuilds itself when the view model changes.</summary>
    public CommentThreadWidget(CommentThreadViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        _border.BorderThickness = new Thickness(1);
        _border.CornerRadius = new CornerRadius(4);
        _border.Padding = new Thickness(8);
        _border.Background = new SolidColorBrush();
        _border.BorderBrush = new SolidColorBrush();
        _panel.Children.Add(_comments);
        _panel.Children.Add(CreateComposer());
        _border.Child = _panel;
        Content = _border;
        _viewModel.Changed += OnChanged;
        Rebuild();
    }

    /// <summary>Raised after the content was rebuilt from the view model.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>The thread shown by this widget.</summary>
    public Core.Comments.CommentThread Thread => _viewModel.Thread;

    /// <summary>
    /// The height, in whole pixels, the content needs at <paramref name="width"/>. The bordered box is measured, not this control: the
    /// control has the zone's height and would answer with it.
    /// </summary>
    public int MeasureContentHeight(double width)
    {
        _border.Measure(new Windows.Foundation.Size(width, double.PositiveInfinity));
        return (int)Math.Ceiling(_border.DesiredSize.Height);
    }

    /// <summary>Takes the colors from the theme (Monaco: <c>editorWidget.*</c>, falling back to the hover's).</summary>
    public void ApplyTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Set((SolidColorBrush)_border.Background, theme.GetColor("editorWidget.background") ?? theme.GetColor("editorHoverWidget.background"));
        Set((SolidColorBrush)_border.BorderBrush, theme.GetColor("editorWidget.border") ?? theme.GetColor("editorHoverWidget.border"));
        Set(_foreground, theme.GetColor("editor.foreground"));
    }

    /// <summary>Stops listening to the view model.</summary>
    public void Detach() => _viewModel.Changed -= OnChanged;

    private static void Set(SolidColorBrush brush, ThemeColor? color)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B);
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        Rebuild();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Rebuild()
    {
        _comments.Children.Clear();
        _comments.Children.Add(new TextBlock { Text = _viewModel.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = _foreground });
        foreach (Core.Comments.Comment comment in _viewModel.Thread.Comments)
        {
            _comments.Children.Add(new TextBlock { Text = $"{comment.Author.Name}: {comment.Body}", TextWrapping = TextWrapping.Wrap, Foreground = _foreground });
        }

        UpdateComposer();
    }
}
