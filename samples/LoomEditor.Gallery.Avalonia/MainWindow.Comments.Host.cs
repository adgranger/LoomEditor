using LoomEditor.Core.Comments;
using LoomEditor.View.Features.CommentThreads;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace LoomEditor.Gallery;

// The comment threads demo (phase 11): the "Comment threads" sample, the buttons of the "Comment threads" expander, and the host's handlers —
// the model of use: the editor only raises events, the host adds comments and threads.
public sealed partial class MainWindow
{
    private void InitializeCommentsHost()
    {
        // The library has no default key for the command: the Gallery adds one at editor level (Ctrl+Alt+M).
        Editor.AddHandler(
            KeyDownEvent,
            (_, e) =>
            {
                if (e.Key == Key.M && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Alt))
                {
                    e.Handled = Editor.Commands.Execute(CommentThreadsCommands.Add);
                }
            },
            RoutingStrategies.Tunnel);
    
    }

    private void OnCommentGutterToggled(object? sender, RoutedEventArgs e)
    {
        if (Editor is not null)
        {
            Editor.CommentGutterButtonEnabled = CommentGutterSwitch.IsChecked == true;
        }
    }

    private void OnAddCommentAtCaret(object? sender, RoutedEventArgs e) =>
        Editor?.AddCommentThread(Editor.Selections[0].PositionLineNumber, startComposing: true);

    private void OnCommentOnSelection(object? sender, RoutedEventArgs e) => Editor?.Commands.Execute(CommentThreadsCommands.Add);

    private void OnAddRange36(object? sender, RoutedEventArgs e) => Editor?.AddCommentThread(3, 6, startComposing: true);

    private void OnAddRange63(object? sender, RoutedEventArgs e)
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

    private void OnAddOnSelection(object? sender, RoutedEventArgs e) => Editor?.AddCommentThread(Editor.Selections[0].Range, startComposing: true);

    private void OnAddTextOutOfDocument(object? sender, RoutedEventArgs e)
    {
        try
        {
            Editor?.AddCommentThread(new Range(Editor.Model.LineCount + 1, 1, Editor.Model.LineCount + 1, 2));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            LogComment($"ArgumentOutOfRangeException: {ex.ParamName}");
        }
    }

    // Stress: a thread every 3 lines, 20 at most.
    private void OnAdd20Threads(object? sender, RoutedEventArgs e)
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

    private void OnRemoveAllThreads(object? sender, RoutedEventArgs e) => RemoveAllThreads();

    private void OnResolveFirstThread(object? sender, RoutedEventArgs e)
    {
        if (Editor is { CommentThreads.Count: > 0 })
        {
            Editor.CommentThreads[0].State = CommentThreadState.Resolved;
        }
    }
}
