using Avalonia.Controls;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Controls.Comments;

// P15-22 replaces this file with the composer (toolbar, buttons, keyboard) and the suggestion blocks. Until then the composer area only holds the text
// box and its preview, which the view model shows while it composes.
internal sealed partial class CommentThreadWidget
{
    private readonly StackPanel _composer = new() { Spacing = 6 };
    private readonly TextBox _textBox = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 96,
        MaxHeight = 280,
        PlaceholderText = CommentStrings.CommentPlaceholder,
    };

    private StackPanel CreateComposer()
    {
        _composer.Children.Add(_textBox);
        _composer.Children.Add(_preview);
        return _composer;
    }

    private void UpdateComposer()
    {
        _composer.IsVisible = _viewModel.IsComposing;
        UpdatePreview();
    }

    // No suggestion blocks yet: every code block is the plain one.
    private static MarkdownBuildOptions? SuggestionOptions(MarkdownDocument document, string? commentId)
    {
        _ = document;
        _ = commentId;
        return null;
    }
}
