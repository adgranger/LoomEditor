using LoomEditor.View.Comments;
using LoomEditor.WinUI.Controls.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>
/// The box that covers the view zone of one comment thread, drawn like a review: a header, the comments, and a one-line form for a
/// collapsed resolved thread. The comment rows and the composer area are separate panels: a rebuild of the rows keeps the composer's focus.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentThreadWidget : UserControl
{
    private readonly CommentThreadViewModel _viewModel;
    private readonly Border _border = new();
    private readonly StackPanel _panel = new() { Spacing = 4 };
    private readonly StackPanel _rows = new() { Spacing = 4 };
    private readonly MarkdownElementBuilder _markdown;
    private double _fontSize = 14;
    private FontFamily _codeFont = new("Cascadia Mono, Consolas, Courier New");

    /// <summary>Creates the widget and fills it from the view model; it rebuilds itself when the view model changes.</summary>
    public CommentThreadWidget(CommentThreadViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        _markdown = new MarkdownElementBuilder(new MarkdownTheme
        {
            Foreground = _foreground,
            LinkBrush = _link,
            CodeBackground = _codeBackground,
            QuoteBar = _quoteBar,
            Separator = _borderBrush,
        });
        _border.BorderThickness = new Thickness(1);
        _border.CornerRadius = new CornerRadius(6);
        _border.Padding = new Thickness(8);
        _border.Background = _background;
        _border.BorderBrush = _borderBrush;
        _panel.Children.Add(_rows);
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

    /// <summary>Takes the editor's UI font size (the hover's) and its code font, and rebuilds.</summary>
    public void ApplyFont(double fontSize, FontFamily codeFont)
    {
        ArgumentNullException.ThrowIfNull(codeFont);
        _fontSize = fontSize;
        _codeFont = codeFont;
        Rebuild();
    }

    /// <summary>Stops listening to the view model.</summary>
    public void Detach() => _viewModel.Changed -= OnChanged;

    private void OnChanged(object? sender, EventArgs e)
    {
        Rebuild();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    // No incremental diffing: threads are small. Relative times are computed here only (no timer).
    private void Rebuild()
    {
        _rows.Children.Clear();
        _rows.Children.Add(Header());
        if (!_viewModel.IsCollapsed)
        {
            AddCommentRows();
        }

        UpdateComposer();
    }
}
