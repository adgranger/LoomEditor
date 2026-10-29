using LoomEditor.Core.Comments;
using LoomEditor.View.Features.CommentThreads;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace LoomEditor.Gallery;

// The comment threads demo (phase 11): the "Comment threads" sample, the buttons of the "Comment threads" expander, and the host's handlers —
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
            LogComment($"CommentSubmitted, {Lines(e.Thread.StartLineNumber, e.Thread.LineNumber)}: {e.Text}");
            // The composer closes by itself when the comment is added.
            e.Thread.AddComment(Comment.Create(CommentSamples.You, e.Text));
        };
        Editor.CommentCancelled += (_, e) => LogComment($"CommentCancelled, {Lines(e.Thread.StartLineNumber, e.Thread.LineNumber)}");
        Editor.AddCommentRequested += (_, e) =>
        {
            LogComment($"AddCommentRequested, {Lines(e.StartLineNumber, e.LineNumber)}");
            Editor.AddCommentThread(e.StartLineNumber, e.LineNumber, startComposing: true);
        };
        Editor.SuggestionApplied += (_, e) =>
        {
            LogComment($"SuggestionApplied, {Lines(e.StartLineNumber, e.LineNumber)}");
            e.Thread.State = CommentThreadState.Resolved;
        };

        // The library has no default key for the command: the Gallery adds one at window level.
        var accelerator = new KeyboardAccelerator { Key = VirtualKey.M, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu };
        accelerator.Invoked += (_, e) =>
        {
            e.Handled = Editor is not null && Editor.Commands.Execute(CommentThreadsCommands.Add);
        };
        Editor.KeyboardAccelerators.Add(accelerator);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => CommentZonesStatus.Text = Editor.CommentZonesDiagnostics;
        timer.Start();
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
            CommentSamples.AddThreads((line, title) => Editor.AddCommentThread(line, title), (start, end, title) => Editor.AddCommentThread(start, end, title));
        }
    }

    private void OnCommentGutterToggled(object sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.CommentGutterButtonEnabled = CommentGutterSwitch.IsOn;
        }
    }

    private void OnAddCommentAtCaret(object sender, RoutedEventArgs e) =>
        Editor?.AddCommentThread(Editor.Selections[0].PositionLineNumber, startComposing: true);

    private void OnCommentOnSelection(object sender, RoutedEventArgs e) => Editor?.Commands.Execute(CommentThreadsCommands.Add);

    private void OnAddRange36(object sender, RoutedEventArgs e) => Editor?.AddCommentThread(3, 6, startComposing: true);

    private void OnAddRange63(object sender, RoutedEventArgs e)
    {
        try
        {
            Editor?.AddCommentThread(6, 3);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            LogComment($"ArgumentOutOfRangeException: {ex.ParamName}");
        }
    }

    // Stress: a thread every 3 lines, 20 at most.
    private void OnAdd20Threads(object sender, RoutedEventArgs e)
    {
        if (Editor is null)
        {
            return;
        }

        for (int i = 0, line = 1; i < 20 && line <= Editor.Model.LineCount; i++, line += 3)
        {
            Editor.AddCommentThread(line).AddComment(Comment.Create(CommentSamples.You, $"Stress thread {i + 1}"));
        }
    }

    private void OnRemoveAllThreads(object sender, RoutedEventArgs e) => RemoveAllThreads();

    private void OnResolveFirstThread(object sender, RoutedEventArgs e)
    {
        if (Editor is { CommentThreads.Count: > 0 })
        {
            Editor.CommentThreads[0].State = CommentThreadState.Resolved;
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

    private void LogComment(string message) => CommentLog.Text = $"{DateTime.Now:HH:mm:ss} {message}\n{CommentLog.Text}";
}
