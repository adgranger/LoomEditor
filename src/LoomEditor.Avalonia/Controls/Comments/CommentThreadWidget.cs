using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.View.Comments;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls.Comments;

/// <summary>
/// The box that covers the view zone of one comment thread, drawn like a review: a header, the comments, and a one-line form for a
/// collapsed resolved thread. The comment rows and the composer area are separate panels: a rebuild of the rows keeps the composer focus.
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
    private FontFamily _codeFont = new(BareFontInfo.DefaultFontFamilyForCurrentOS);

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
        AutomationProperties.SetAutomationId(this, "LoomEditor.CommentThread");
        // A UserControl has no role of its own for UI Automation: without this its name is not exposed as a group.
        AutomationProperties.SetControlTypeOverride(this, AutomationControlType.Group);
        _viewModel.Changed += OnChanged;
        Rebuild();
    }

    /// <summary>Raised after the content was rebuilt from the view model.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>Sends a screen-reader announcement through the editor announcer. Set by the host.</summary>
    public Action<string>? Announce { get; set; }

    /// <summary>The thread shown by this widget.</summary>
    public Core.Comments.CommentThread Thread => _viewModel.Thread;

    /// <summary>The bordered box, for the tests.</summary>
    internal Border Box => _border;

    /// <summary>
    /// The height, in whole pixels, the content needs at <paramref name="width"/>. The bordered box is measured, not this control: the
    /// control has the height of the zone and would answer with it.
    /// </summary>
    public int MeasureContentHeight(double width)
    {
        _border.Measure(new Size(width, double.PositiveInfinity));
        return (int)Math.Ceiling(_border.DesiredSize.Height);
    }

    /// <summary>Takes the UI font size of the editor (the hover one) and its code font, and rebuilds.</summary>
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
        // Kept current when the line moves: a move raises Changed.
        AutomationProperties.SetName(this, CommentStrings.ThreadAutomationName(_viewModel.Thread.StartLineNumber, _viewModel.Thread.LineNumber));
        _rows.Children.Clear();
        _rows.Children.Add(Header());
        if (!_viewModel.IsCollapsed)
        {
            AddCommentRows();
        }

        UpdateComposer();
        UpdateCommentedText();
    }

    // The exact text of an exact-text thread, for UI Automation; the name and the title stay the same (README decision 20).
    private void UpdateCommentedText() =>
        AutomationProperties.SetHelpText(this, _viewModel.CommentedText is { Length: > 0 } text ? CommentStrings.CommentedText(text) : string.Empty);
}
