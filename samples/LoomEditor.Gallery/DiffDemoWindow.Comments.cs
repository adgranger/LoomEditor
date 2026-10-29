using LoomEditor.Core.Comments;
using LoomEditor.WinUI.Controls;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

// Comment threads on both sides of the diff (phase 11): the same host handlers as the main window, with the side in the log.
public sealed partial class DiffDemoWindow
{
    private void InitializeComments()
    {
        if (DiffEditor is null)
        {
            return;
        }

        DiffEditor.CommentSubmitted += (_, e) =>
        {
            Log($"CommentSubmitted ({DiffEditor.GetSide(e.Thread)}, line {e.Thread.LineNumber})");
            e.Thread.AddComment(Comment.Create(CommentSamples.You, e.Text));
        };
        DiffEditor.CommentCancelled += (_, e) => Log($"CommentCancelled ({SideOrRemoved(e.Thread)}, line {e.Thread.LineNumber})");
        DiffEditor.SuggestionApplied += (_, e) =>
        {
            Log($"SuggestionApplied ({DiffEditor.GetSide(e.Thread)}, line {e.LineNumber})");
            e.Thread.State = CommentThreadState.Resolved;
        };
        DiffEditor.AddCommentRequested += (_, e) =>
        {
            Log($"AddCommentRequested ({e.Side}, line {e.LineNumber})");
            DiffEditor.AddCommentThread(e.Side, e.LineNumber, startComposing: true);
        };
    }

    private void OnAddOnOriginal(object sender, RoutedEventArgs e) =>
        DiffEditor.AddCommentThread(DiffSide.Original, DiffEditor.Original.Selections[0].PositionLineNumber, startComposing: true);

    private void OnAddOnModified(object sender, RoutedEventArgs e) =>
        DiffEditor.AddCommentThread(DiffSide.Modified, DiffEditor.Modified.Selections[0].PositionLineNumber, startComposing: true);

    // The gutter "+" is a property of each editor, not of the diff editor.
    private void OnCommentGutterChanged(object sender, RoutedEventArgs e)
    {
        bool on = CommentGutterBox.IsChecked == true;
        DiffEditor.Original.CommentGutterButtonEnabled = on;
        DiffEditor.Modified.CommentGutterButtonEnabled = on;
    }

    // Two versions of the comment sample, with its threads on the modified side and one on the original side.
    private void OnLoadCommentSample(object sender, RoutedEventArgs e)
    {
        foreach (CommentThread thread in DiffEditor.CommentThreads)
        {
            DiffEditor.RemoveCommentThread(thread);
        }

        DiffEditor.OriginalText = CommentSamples.Sample.Text;
        DiffEditor.ModifiedText = CommentSamples.ModifiedText;
        CommentSamples.AddThreads((line, title) => DiffEditor.AddCommentThread(DiffSide.Original, line, title));
        DiffEditor.AddCommentThread(DiffSide.Modified, CommentSamples.MaxLine, "On the new constant")
            .AddComment(Comment.Create(new CommentAuthor("Linus"), "Better.", DateTimeOffset.UtcNow.AddMinutes(-5)));
    }

    // Cancel on an empty thread removes it after the event: the side may already be unknown.
    private string SideOrRemoved(CommentThread thread) =>
        DiffEditor.CommentThreads.Contains(thread) ? DiffEditor.GetSide(thread).ToString() : "removed";

    private void Log(string message) => CommentLogText.Text = $"{DateTime.Now:HH:mm:ss} {message}";
}
