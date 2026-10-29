using LoomEditor.Core.Comments;
using LoomEditor.View.Features.CommentThreads;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

// Temporary (P11-12): the buttons of the "Comment threads (P11-12)" expander; P11-23 replaces them with the real demo.
public sealed partial class MainWindow
{
    private int _commentsSubmitted;

    private void InitializeCommentZonesStatus()
    {
        // The editor only raises the event: the host adds the comment.
        Editor.CommentSubmitted += (_, e) =>
        {
            _commentsSubmitted++;
            e.Thread.AddComment(Comment.Create(SampleAuthor, e.Text));
        };
        // The editor never adds a thread by itself: the gutter "+" and the command only ask.
        Editor.AddCommentRequested += (_, e) => Editor.AddCommentThread(e.LineNumber, startComposing: true);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => CommentZonesStatus.Text = $"{Editor.CommentZonesDiagnostics} · CommentSubmitted {_commentsSubmitted}";
        timer.Start();
    }

    private void OnCommentGutterToggled(object sender, RoutedEventArgs e) => Editor.CommentGutterButtonEnabled = CommentGutterSwitch.IsOn;

    // The command loom.commentThreads.add, as a keyboard binding would run it.
    private void OnAddCommentAtCaret(object sender, RoutedEventArgs e) => Editor.Commands.Execute(CommentThreadsCommands.Add);

    private void OnStartComposingOnLine5(object sender, RoutedEventArgs e)
    {
        if (Editor.Model.LineCount >= 5)
        {
            Editor.AddCommentThread(5, startComposing: true);
        }
    }

    // P11-16: one comment; three comments with Markdown; a resolved thread; a very long line.
    private void OnAddReviewThreads(object sender, RoutedEventArgs e)
    {
        if (Editor.Model.LineCount < 33)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var ada = new CommentAuthor("Ada Lovelace");
        var linus = new CommentAuthor("Linus");
        var grace = new CommentAuthor("Grace Hopper");

        Editor.AddCommentThread(3).AddComment(Comment.Create(ada, "Should this line be `const`?", now.AddMinutes(-5)));

        CommentThread review = Editor.AddCommentThread(12, "Review of the loop");
        review.AddComment(Comment.Create(linus, ReviewMarkdown, now.AddHours(-3)));
        review.AddComment(Comment.Create(ada, "Agreed, **but** keep the _old_ name in a comment.", now.AddHours(-2)) with { UpdatedAt = now.AddHours(-1) });
        review.AddComment(Comment.Create(grace, "Done.", now.AddMinutes(-30)));

        CommentThread resolved = Editor.AddCommentThread(25);
        resolved.AddComment(Comment.Create(grace, "Typo in the **name**.\nSecond line, hidden when collapsed.", now.AddDays(-2)));
        resolved.AddComment(Comment.Create(ada, "Fixed.", now.AddDays(-1)));
        resolved.State = CommentThreadState.Resolved;

        Editor.AddCommentThread(33).AddComment(Comment.Create(linus, string.Concat(Enumerable.Repeat("a-very-long-line-without-spaces ", 20)), now.AddSeconds(-20)));
    }

    private const string ReviewMarkdown = """
        This loop does **bold**, *italic*, ~~strike~~, <u>underline</u>, `inline code` and a [link](https://example.com).

        - first point
        - second point

        > A quote with a remark.

        ```csharp
        for (int i = 0; i < n; i++) { }
        ```
        """;

    // P11-19: type `int max = 10;` on a line first. One-line, empty (deletes the line) and two-line suggestions.
    private void OnAddSuggestionThread(object sender, RoutedEventArgs e)
    {
        CommentThread thread = Editor.AddCommentThread(Editor.Selections[0].PositionLineNumber, "Suggestions");
        var grace = new CommentAuthor("Grace Hopper");
        thread.AddComment(Comment.Create(grace, SuggestionMarkdown("const int Max = 10;", "Use a constant:")));
        thread.AddComment(Comment.Create(grace, SuggestionMarkdown("", "Or delete the line:")));
        thread.AddComment(Comment.Create(grace, SuggestionMarkdown("// The largest count.\nconst int Max = 10;", "Or document it:")));
    }

    // An empty code makes an empty block (no line between the fences): the suggestion deletes the line.
    private static string SuggestionMarkdown(string code, string intro) =>
        code.Length == 0 ? $"{intro}\n\n```suggestion\n```" : $"{intro}\n\n```suggestion\n{code}\n```";

    private void OnAddThreadAtCaret(object sender, RoutedEventArgs e) => Editor.AddCommentThread(Editor.Selections[0].PositionLineNumber);

    private void OnAddThreeThreads(object sender, RoutedEventArgs e)
    {
        foreach (int line in new[] { 5, 6, 40 })
        {
            if (line <= Editor.Model.LineCount)
            {
                Editor.AddCommentThread(line, $"Thread on line {line}");
            }
        }
    }

    private void OnAdd200Threads(object sender, RoutedEventArgs e)
    {
        for (int line = 1; line <= Math.Min(200, Editor.Model.LineCount); line++)
        {
            Editor.AddCommentThread(line);
        }
    }

    private static readonly CommentAuthor SampleAuthor = new("Gallery");

    private const string ThreeParagraphs =
        "First paragraph: the box and its gap grow together, and the line below stays touching the box.\n\n" +
        "Second paragraph: narrow the window so this text wraps on more rows; the gap follows once the resize stops.\n\n" +
        "Third paragraph: the diagnostics line shows heightPasses going up by one or two and heightLimitHits staying at 0.";

    private const string LongTitle =
        "A very long title that wraps on several rows when the window is narrow enough, to check that the zone height follows the title too";

    private CommentThread? FirstThread => Editor.CommentThreads.Count > 0 ? Editor.CommentThreads[0] : null;

    private void OnAddCommentToFirstThread(object sender, RoutedEventArgs e) =>
        FirstThread?.AddComment(Comment.Create(SampleAuthor, ThreeParagraphs));

    private void OnToggleLongTitle(object sender, RoutedEventArgs e)
    {
        if (FirstThread is { } thread)
        {
            thread.Title = thread.Title == LongTitle ? null : LongTitle;
        }
    }

    private void OnRemoveAllThreads(object sender, RoutedEventArgs e)
    {
        foreach (var thread in Editor.CommentThreads.ToArray())
        {
            Editor.RemoveCommentThread(thread);
        }
    }
}
