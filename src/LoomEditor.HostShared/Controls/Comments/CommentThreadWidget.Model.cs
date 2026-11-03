using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;
using LoomEditor.Core.Comments;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Comments;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Comments;
#endif

// What the widget shows: the rebuild of the rows from the view model, and the summary of a collapsed thread. The controls are in the .Host parts.
internal sealed partial class CommentThreadWidget
{
    private void OnChanged(object? sender, EventArgs e)
    {
        Rebuild();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    // No incremental diffing: threads are small. Relative times are computed here only (no timer).
    private void Rebuild()
    {
        // Kept current when the line moves: a move raises Changed.
        SetAutomationName(CommentStrings.ThreadAutomationName(_viewModel.Thread.StartLineNumber, _viewModel.Thread.LineNumber));
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
        SetAutomationHelp(_viewModel.CommentedText is { Length: > 0 } text ? CommentStrings.CommentedText(text) : string.Empty);

    private string CollapsedSummary()
    {
        IReadOnlyList<Comment> comments = _viewModel.Thread.Comments;
        if (comments.Count == 0)
        {
            return "";
        }

        string plain = MarkdownParser.Parse(comments[0].Body, MarkdownParserOptions.Extended).ToPlainText();
        int end = plain.IndexOf('\n', StringComparison.Ordinal);
        string firstLine = end < 0 ? plain : plain[..end];
        return $"{firstLine} · {CommentStrings.CommentCount(comments.Count)}";
    }
}
