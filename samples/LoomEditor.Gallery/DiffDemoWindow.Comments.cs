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
            Log($"CommentSubmitted ({DiffEditor.GetSide(e.Thread)}, {Where(e.Thread.StartLineNumber, e.Thread.LineNumber, TextOf(e.Thread))})");
            e.Thread.AddComment(Comment.Create(CommentSamples.You, e.Text));
        };
        DiffEditor.CommentCancelled += (_, e) => Log($"CommentCancelled ({SideOrRemoved(e.Thread)}, {Where(e.Thread.StartLineNumber, e.Thread.LineNumber, TextOf(e.Thread))})");
        DiffEditor.SuggestionApplied += (_, e) =>
        {
            Log($"SuggestionApplied ({DiffEditor.GetSide(e.Thread)}, {Lines(e.StartLineNumber, e.LineNumber)})");
            e.Thread.State = CommentThreadState.Resolved;
        };
        DiffEditor.AddCommentRequested += (_, e) =>
        {
            Log($"AddCommentRequested ({e.Side}, {Where(e.StartLineNumber, e.LineNumber, e.TextRange)})");
            if (e.TextRange is { } text)
            {
                DiffEditor.AddCommentThread(e.Side, text, startComposing: true);
            }
            else
            {
                DiffEditor.AddCommentThread(e.Side, e.StartLineNumber, e.LineNumber, startComposing: true);
            }
        };
    }

    private void OnAddOnOriginal(object sender, RoutedEventArgs e) => AddOnSelection(DiffSide.Original, DiffEditor.Original);

    private void OnAddOnModified(object sender, RoutedEventArgs e) => AddOnSelection(DiffSide.Modified, DiffEditor.Modified);

    // The text of the side's selection (the cursor line when empty), like the add command.
    private void AddOnSelection(DiffSide side, LoomEditor.WinUI.Controls.LoomEditor editor) =>
        DiffEditor.AddCommentThread(side, editor.Selections[0].Range, startComposing: true);

    private static string Lines(int start, int end) => start == end ? $"line {end}" : $"lines {start}\u2013{end}";

    private static Range? TextOf(CommentThread thread) =>
        thread.IsWholeLines ? null : new Range(thread.StartLineNumber, thread.StartColumn, thread.LineNumber, thread.EndColumn);

    private static string Where(int start, int end, Range? text) =>
        text is { } t ? $"{Lines(start, end)}, text {t.StartLineNumber}:{t.StartColumn}\u2013{t.EndLineNumber}:{t.EndColumn}" : Lines(start, end);

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
        CommentSamples.AddThreads(
            (line, title) => DiffEditor.AddCommentThread(DiffSide.Original, line, title),
            (start, end, title) => DiffEditor.AddCommentThread(DiffSide.Original, start, end, title),
            (range, title) => DiffEditor.AddCommentThread(DiffSide.Original, range, title));
        DiffEditor.AddCommentThread(DiffSide.Modified, CommentSamples.MaxLine, "On the new constant")
            .AddComment(Comment.Create(new CommentAuthor("Linus"), "Better.", DateTimeOffset.UtcNow.AddMinutes(-5)));
    }

    // Cancel on an empty thread removes it after the event: the side may already be unknown.
    private string SideOrRemoved(CommentThread thread) =>
        DiffEditor.CommentThreads.Contains(thread) ? DiffEditor.GetSide(thread).ToString() : "removed";

    private void Log(string message) => CommentLogText.Text = $"{DateTime.Now:HH:mm:ss} {message}";
}
