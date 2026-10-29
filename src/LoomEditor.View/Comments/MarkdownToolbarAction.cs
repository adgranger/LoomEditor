namespace LoomEditor.View.Comments;

/// <summary>An action of the comment editor's Markdown toolbar, in the order of the buttons.</summary>
/// <remarks>Threading: an enum, safe to use from any thread.</remarks>
internal enum MarkdownToolbarAction
{
    Heading,
    Bold,
    Italic,
    Underline,
    Strikethrough,
    InlineCode,
    CodeBlock,
    Link,
    BulletList,
    NumberedList,
    Quote,
    Suggestion,
}
