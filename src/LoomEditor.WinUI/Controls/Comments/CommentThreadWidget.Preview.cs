using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Comments;

// Preview on: the text box is collapsed (it keeps its selection) and the rendered draft shows in its place, at least as tall as the box was.
internal sealed partial class CommentThreadWidget
{
    private readonly StackPanel _preview = new() { Visibility = Visibility.Collapsed };
    private string? _previewDraft;

    private void UpdatePreview()
    {
        bool show = _viewModel.IsComposing && _viewModel.IsPreview;
        if (!show)
        {
            _preview.Visibility = Visibility.Collapsed;
            _textBox.Visibility = Visibility.Visible;
            return;
        }

        if (_textBox.Visibility == Visibility.Visible)
        {
            // No jump: the preview starts at the height the box had.
            _preview.MinHeight = _textBox.ActualHeight;
            _textBox.Visibility = Visibility.Collapsed;
        }

        _preview.Visibility = Visibility.Visible;
        if (_previewDraft == _viewModel.Draft)
        {
            return;
        }

        _previewDraft = _viewModel.Draft;
        _preview.Children.Clear();
        if (string.IsNullOrWhiteSpace(_viewModel.Draft))
        {
            _preview.Children.Add(Text(CommentStrings.NothingToPreview, _dim));
            return;
        }

        // In the Preview a suggestion block has no Apply button (no comment id).
        MarkdownDocument document = MarkdownParser.Parse(_viewModel.Draft, MarkdownParserOptions.Extended);
        foreach (UIElement row in _markdown.Build(document, _fontSize, _codeFont, SuggestionOptions(document, commentId: null)))
        {
            _preview.Children.Add(row);
        }
    }
}
