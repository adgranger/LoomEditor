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

    private void OnRemoveAllThreads(object sender, RoutedEventArgs e)
    {
        foreach (var thread in Editor.CommentThreads.ToArray())
        {
            Editor.RemoveCommentThread(thread);
        }
    }
}
