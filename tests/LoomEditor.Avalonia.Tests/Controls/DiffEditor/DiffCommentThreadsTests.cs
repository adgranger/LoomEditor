using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Comments;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffCommentThreadsTests
{
    [Fact]
    public async Task AddCommentOnModifiedSide_RaisesDiffAddCommentRequested_WithSide()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await DiffEditorWindow.OpenAsync("one\ntwo\nthree", "one\ntwo\nnew\nthree");
            diff.Modified.CommentGutterButtonEnabled = true;
            DiffAddCommentRequestedEventArgs? received = null;
            diff.AddCommentRequested += (_, e) => received = e;
            Point inGutter = new(diff.Modified.ViewModel.LayoutInfo.LineNumbersLeft + 4, (2 - 0.5) * diff.Modified.LineHeight);
            window.MouseMove(diff.Modified.TranslatePoint(inGutter, window)!.Value);
            await DiffEditorWindow.SettleAsync(window);

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
            (LoomDiffEditor diff, Window window) = await DiffEditorWindow.OpenAsync("one\ntwo\nthree", "one\ntwo\nnew\nthree");

            CommentThread thread = diff.AddCommentThread(DiffSide.Original, 2);
            thread.AddComment(Comment.Create(new CommentAuthor("Ada"), "Hello"));
            await DiffEditorWindow.SettleAsync(window);

            Assert.Equal(DiffSide.Original, diff.GetSide(thread));
            Assert.True(diff.Original.CommentWidgetOf(thread)!.IsVisible);
            Assert.Null(diff.Modified.CommentWidgetOf(thread));
            Assert.Contains(thread, diff.CommentThreads);
            window.Close();
        });
    }
}
