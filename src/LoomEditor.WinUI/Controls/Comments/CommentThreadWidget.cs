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
        _border.Child = _panel;
        Content = _border;
        _viewModel.Changed += OnChanged;
        Rebuild();
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

    private void OnChanged(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        _panel.Children.Clear();
        _panel.Children.Add(new TextBlock { Text = _viewModel.Title, FontWeight = FontWeights.SemiBold, Foreground = _foreground });
        foreach (Core.Comments.Comment comment in _viewModel.Thread.Comments)
        {
            _panel.Children.Add(new TextBlock { Text = $"{comment.Author.Name}: {comment.Body}", TextWrapping = TextWrapping.Wrap, Foreground = _foreground });
        }
    }
}
