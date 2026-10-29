using System.Globalization;

namespace LoomEditor.View.Comments;

/// <summary>Every user-visible English string of the comment threads feature.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class CommentStrings
{
    public const string Resolved = "Resolved";
    public const string Reply = "Reply";
    public const string Comment = "Comment";
    public const string Cancel = "Cancel";
    public const string Preview = "Preview";
    public const string CommentPlaceholder = "Leave a comment";
    public const string NothingToPreview = "Nothing to preview";
    public const string Edited = " (edited)";

    public const string SuggestedChange = "Suggested change";
    public const string ApplySuggestion = "Apply suggestion";
    public const string NoChanges = "No changes";
    public const string ApplyFailedStale = "The line changed. Review the new suggestion and try again.";
    public const string ApplyFailedStaleLines = "The lines changed. Review the new suggestion and try again.";
    public const string ApplyFailedReadOnly = "The editor is read-only.";
    public const string DescribeRemoved = "Removed: ";
    public const string DescribeAdded = "Added: ";
    public const string DescribeLineDeleted = "Line deleted.";
    public const string DescribeLinesDeleted = "Lines deleted.";
    public const string DescribeEmpty = "(empty)";

    public const string CommentText = "Comment text";

    public const string CommentPosted = "Comment posted";
    public const string SuggestionApplied = "Suggestion applied";
    public const string CommentDiscarded = "Comment discarded";

    public static string ThreadTitle(int line) => string.Create(CultureInfo.InvariantCulture, $"Comment on line {line}");

    public static string ThreadTitle(int start, int end) => start == end
        ? ThreadTitle(start)
        : string.Create(CultureInfo.InvariantCulture, $"Comment on lines {start} to {end}");

    public static string CommentCount(int n) => n == 1 ? "1 comment" : string.Create(CultureInfo.InvariantCulture, $"{n} comments");

    public static string ThreadAutomationName(int line) => string.Create(CultureInfo.InvariantCulture, $"Comment thread on line {line}");

    public static string ThreadAutomationName(int start, int end) => start == end
        ? ThreadAutomationName(start)
        : string.Create(CultureInfo.InvariantCulture, $"Comment thread on lines {start} to {end}");

    public static string CommentAutomationName(string author, string time) => $"{author}, {time}";

    public static string AddCommentOnLine(int line) => string.Create(CultureInfo.InvariantCulture, $"Add a comment on line {line}");

    public static string AddCommentOnLines(int start, int end) => start == end
        ? AddCommentOnLine(start)
        : string.Create(CultureInfo.InvariantCulture, $"Add a comment on lines {start} to {end}");

    public static string ThreadAdded(int line) => string.Create(CultureInfo.InvariantCulture, $"Comment thread added on line {line}");

    public static string ThreadAdded(int start, int end) => start == end
        ? ThreadAdded(start)
        : string.Create(CultureInfo.InvariantCulture, $"Comment thread added on lines {start} to {end}");

    public const int MaxCommentedTextLength = 80;

    public static string CommentedText(string text)
    {
        string flat = text.Replace("\r\n", " ").Replace('\n', ' ');
        return "Commented text: " + (flat.Length > MaxCommentedTextLength ? flat[..MaxCommentedTextLength] + "…" : flat);
    }

    public static string AddCommentOnSelectedText(int start, int end) => start == end
        ? string.Create(CultureInfo.InvariantCulture, $"Add a comment on the selected text on line {start}")
        : string.Create(CultureInfo.InvariantCulture, $"Add a comment on the selected text on lines {start} to {end}");

    public static string ThreadAddedOnText(int start, int end) => start == end
        ? string.Create(CultureInfo.InvariantCulture, $"Comment thread added on the selected text on line {start}")
        : string.Create(CultureInfo.InvariantCulture, $"Comment thread added on the selected text on lines {start} to {end}");

    public static string ToolbarName(MarkdownToolbarAction action) => action switch
    {
        MarkdownToolbarAction.Heading => "Heading",
        MarkdownToolbarAction.Bold => "Bold",
        MarkdownToolbarAction.Italic => "Italic",
        MarkdownToolbarAction.Underline => "Underline",
        MarkdownToolbarAction.Strikethrough => "Strikethrough",
        MarkdownToolbarAction.InlineCode => "Inline code",
        MarkdownToolbarAction.CodeBlock => "Code block",
        MarkdownToolbarAction.Link => "Link",
        MarkdownToolbarAction.BulletList => "Bulleted list",
        MarkdownToolbarAction.NumberedList => "Numbered list",
        MarkdownToolbarAction.Quote => "Quote",
        MarkdownToolbarAction.Suggestion => "Suggest a change",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}
