using LoomEditor.Core.Comments;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

// Temporary (P11-12): the buttons of the "Comment threads (P11-12)" expander; P11-23 replaces them with the real demo.
public sealed partial class MainWindow
{
    private void InitializeCommentZonesStatus()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => CommentZonesStatus.Text = Editor.CommentZonesDiagnostics;
        timer.Start();
    }

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
