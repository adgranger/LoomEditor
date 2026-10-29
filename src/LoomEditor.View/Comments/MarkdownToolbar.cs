namespace LoomEditor.View.Comments;

/// <summary>Turns a toolbar click, the text and its selection into the new text and selection.</summary>
/// <remarks>Threading: pure static functions, safe to call from any thread.</remarks>
internal static partial class MarkdownToolbar
{
    /// <summary>Applies <paramref name="action"/> to <paramref name="selection"/> in <paramref name="text"/> (<c>\n</c> line breaks only).</summary>
    /// <param name="action">The clicked button.</param>
    /// <param name="text">The current text.</param>
    /// <param name="selection">The current selection.</param>
    /// <param name="suggestionSeed">The text a suggestion block starts with; may hold several lines separated by <c>\n</c> (used by the block actions).</param>
    /// <exception cref="ArgumentOutOfRangeException">The selection is outside the text.</exception>
    public static ToolbarEdit Apply(MarkdownToolbarAction action, string text, ToolbarSelection selection, string? suggestionSeed = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (selection.End > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(selection));
        }

        return action switch
        {
            MarkdownToolbarAction.Bold => ToggleInline(text, selection, "**", "**"),
            MarkdownToolbarAction.Italic => ToggleInline(text, selection, "_", "_"),
            MarkdownToolbarAction.Underline => ToggleInline(text, selection, "<u>", "</u>"),
            MarkdownToolbarAction.Strikethrough => ToggleInline(text, selection, "~~", "~~"),
            MarkdownToolbarAction.InlineCode => ToggleInline(text, selection, "`", "`"),
            MarkdownToolbarAction.Link => InsertLink(text, selection),
            MarkdownToolbarAction.CodeBlock => ToggleCodeBlock(text, selection),
            MarkdownToolbarAction.Suggestion => InsertSuggestion(text, selection, suggestionSeed),
            _ => ToggleLinePrefix(action, text, selection),
        };
    }
}
