using LoomEditor.Core.Comments;
using LoomEditor.WinUI.Controls;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

// Temporary (P11-22): comment threads on either side of the diff; the log shows each event with its side. P11-23 finalizes this.
public sealed partial class DiffDemoWindow
{
    private static readonly CommentAuthor Reviewer = new("Gallery");

    private void InitializeComments()
    {
        DiffEditor.CommentSubmitted += (_, e) =>
        {
            Log($"CommentSubmitted ({DiffEditor.GetSide(e.Thread)})");
            e.Thread.AddComment(Comment.Create(Reviewer, e.Text));
        };
        DiffEditor.CommentCancelled += (_, e) => Log($"CommentCancelled ({SideOrRemoved(e.Thread)})");
        DiffEditor.SuggestionApplied += (_, e) => Log($"SuggestionApplied ({DiffEditor.GetSide(e.Thread)}, line {e.LineNumber})");
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

    // Cancel on an empty thread removes it after the event: the side may already be unknown.
    private string SideOrRemoved(CommentThread thread) =>
        DiffEditor.CommentThreads.Contains(thread) ? DiffEditor.GetSide(thread).ToString() : "removed";

    private void Log(string message) => CommentLogText.Text = $"{DateTime.Now:HH:mm:ss} {message}";
}
