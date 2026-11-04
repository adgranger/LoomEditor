using LoomEditor.View.Comments;

#if !LOOM_AVALONIA
using LoomEditor.WinUI.Controls.Markdown;
namespace LoomEditor.WinUI.Controls.Comments;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Controls.Markdown;
namespace LoomEditor.Avalonia.Controls.Comments;
#endif

/// <summary>
/// The box that covers the view zone of one comment thread, drawn like a review: a header, the comments, and a one-line form for a
/// collapsed resolved thread. The comment rows and the composer area are separate panels: a rebuild of the rows keeps the composer's focus.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentThreadWidget
{
    private readonly CommentThreadViewModel _viewModel;
    private readonly MarkdownElementBuilder _markdown;
    private double _fontSize = 14;

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
        _panel.Children.Add(_rows);
        _panel.Children.Add(CreateComposer());
        BuildBox();
        SetAutomationId("LoomEditor.CommentThread");
        _viewModel.Changed += OnChanged;
        Rebuild();
    }

    /// <summary>Raised after the content was rebuilt from the view model.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>The thread shown by this widget.</summary>
    public Core.Comments.CommentThread Thread => _viewModel.Thread;

    /// <summary>Sends a screen-reader announcement through the editor's announcer. Set by the host.</summary>
    public Action<string>? Announce { get; set; }

    /// <summary>Stops listening to the view model.</summary>
    public void Detach() => _viewModel.Changed -= OnChanged;
}
