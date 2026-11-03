using LoomEditor.View.Comments;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Comments;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Comments;
#endif

// The labels and the enabling of the toolbar buttons; the host part builds the buttons and handles the keys.
internal sealed partial class CommentComposerToolbar
{
    /// <summary>Enables or disables every button (a panel has no <c>IsEnabled</c> of its own).</summary>
    public void SetEnabled(bool enabled)
    {
        foreach (var button in _buttons)
        {
            button.IsEnabled = enabled;
        }
    }

    private static string Label(MarkdownToolbarAction action) => action switch
    {
        MarkdownToolbarAction.Heading => "H",
        MarkdownToolbarAction.Bold => "B",
        MarkdownToolbarAction.Italic => "I",
        MarkdownToolbarAction.Underline => "U",
        MarkdownToolbarAction.Strikethrough => "S",
        MarkdownToolbarAction.InlineCode => "<>",
        MarkdownToolbarAction.CodeBlock => "{ }",
        MarkdownToolbarAction.Link => "Link",
        MarkdownToolbarAction.BulletList => "•",
        MarkdownToolbarAction.NumberedList => "1.",
        MarkdownToolbarAction.Quote => "\"",
        MarkdownToolbarAction.Suggestion => "±",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}
