using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Comments;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffCommentThreadsTests
{
    private static async Task<(LoomDiffEditor Diff, Window Window)> Open()
    {
        var diff = new LoomDiffEditor { OriginalText = "one\ntwo\nthree", ModifiedText = "one\ntwo\nnew\nthree" };
        var window = new Window { Width = 1000, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        await Settle(window);
        return (diff, window);
    }

    private static async Task Settle(Window window)
    {
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(30);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Fact]
    public async Task AddCommentOnModifiedSide_RaisesDiffAddCommentRequested_WithSide()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            diff.Modified.CommentGutterButtonEnabled = true;
            DiffAddCommentRequestedEventArgs? received = null;
            diff.AddCommentRequested += (_, e) => received = e;
            Point inGutter = new(diff.Modified.ViewModel.LayoutInfo.LineNumbersLeft + 4, (2 - 0.5) * diff.Modified.LineHeight);
            window.MouseMove(diff.Modified.TranslatePoint(inGutter, window)!.Value);
            await Settle(window);

            diff.Modified.CommentGutter!.Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.NotNull(received);
            Assert.Equal(DiffSide.Modified, received.Side);
            Assert.Equal(2, received.LineNumber);
            window.Close();
        });
    }

    [Fact]
    public async Task ThreadOnOriginalSide_OpensZoneOnThatSide()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();

            CommentThread thread = diff.AddCommentThread(DiffSide.Original, 2);
            thread.AddComment(Comment.Create(new CommentAuthor("Ada"), "Hello"));
            await Settle(window);

            Assert.Equal(DiffSide.Original, diff.GetSide(thread));
            Assert.True(diff.Original.CommentWidgetOf(thread)!.IsVisible);
            Assert.Null(diff.Modified.CommentWidgetOf(thread));
            Assert.Contains(thread, diff.CommentThreads);
            window.Close();
        });
    }
}
