using LoomEditor.Core.Comments;

namespace LoomEditor.Gallery;

// The comment threads demo (phase 11): the "Comment threads" sample and the host's handlers —
// the model of use: the editor only raises events, the host adds comments and threads.
public sealed partial class MainWindow
{
    private void InitializeComments()
    {
        // Guarded like every handler that can run early: the self-checks start the window without user input.
        if (Editor is null)
        {
            return;
        }

        Editor.CommentSubmitted += (_, e) =>
        {
            LogComment($"CommentSubmitted, {Where(e.Thread.StartLineNumber, e.Thread.LineNumber, TextOf(e.Thread))}: {e.Text}");
            // The composer closes by itself when the comment is added.
            e.Thread.AddComment(Comment.Create(CommentSamples.You, e.Text));
        };
        Editor.CommentCancelled += (_, e) => LogComment($"CommentCancelled, {Where(e.Thread.StartLineNumber, e.Thread.LineNumber, TextOf(e.Thread))}");
        Editor.AddCommentRequested += (_, e) =>
        {
            LogComment($"AddCommentRequested, {Where(e.StartLineNumber, e.LineNumber, e.TextRange)}");
            if (e.TextRange is { } text)
            {
                Editor.AddCommentThread(text, startComposing: true);
            }
            else
            {
                Editor.AddCommentThread(e.StartLineNumber, e.LineNumber, startComposing: true);
            }
        };
        Editor.SuggestionApplied += (_, e) =>
        {
            LogComment($"SuggestionApplied, {Lines(e.StartLineNumber, e.LineNumber)}");
            e.Thread.State = CommentThreadState.Resolved;
        };

        InitializeCommentsHost();
    }

    // Called after the Features picker loaded a sample: the comment sample comes with its threads.
    private void OnFeatureSampleLoaded(FeatureSample sample)
    {
        if (Editor is null)
        {
            return;
        }

        RemoveAllThreads();
        if (sample.Name == CommentSamples.Name)
        {
            CommentSamples.AddThreads((line, title) => Editor.AddCommentThread(line, title), (start, end, title) => Editor.AddCommentThread(start, end, title),
                (range, title) => Editor.AddCommentThread(range, title));
        }
    }

    private void RemoveAllThreads()
    {
        foreach (CommentThread thread in Editor?.CommentThreads.ToArray() ?? [])
        {
            Editor!.RemoveCommentThread(thread);
        }
    }

    private static string Lines(int start, int end) => start == end ? $"line {end}" : $"lines {start}\u2013{end}";

    private static Range? TextOf(CommentThread thread) =>
        thread.IsWholeLines ? null : new Range(thread.StartLineNumber, thread.StartColumn, thread.LineNumber, thread.EndColumn);

    private static string Where(int start, int end, Range? text) =>
        text is { } t ? $"{Lines(start, end)}, text {t.StartLineNumber}:{t.StartColumn}\u2013{t.EndLineNumber}:{t.EndColumn}" : Lines(start, end);

    private void LogComment(string message) => CommentLog.Text = $"{DateTime.Now:HH:mm:ss} {message}\n{CommentLog.Text}";
}
