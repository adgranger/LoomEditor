using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Comments;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class CommentZoneTests
{
    private static (Editor Editor, Window Window) Open()
    {
        var editor = new Editor { Text = "one\ntwo\nthree\nfour", MinimapEnabled = false };
        return (editor, EditorWindow.Open(editor));
    }

    // The zone host arranges and measures after a draw, on the dispatcher: a few frames settle it.
    private static void Settle(Window window)
    {
        for (int i = 0; i < 4; i++)
        {
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Fact]
    public async Task OpenThread_InsertsZoneBelowLine()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open();
            double before = editor.GetTopForLineNumber(3);

            CommentThread thread = editor.AddCommentThread(1);
            thread.AddComment(Comment.Create(new CommentAuthor("Ada"), "Hello"));
            Settle(window);

            CommentThreadWidget widget = editor.CommentWidgetOf(thread)!;
            Assert.True(widget.IsVisible);
            Assert.True(Canvas.GetTop(widget) >= editor.LineHeight - 1);
            Assert.True(editor.GetTopForLineNumber(3) > before);
            window.Close();
        });
    }

    [Fact]
    public async Task AddComment_GrowsZone()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open();
            CommentThread thread = editor.AddCommentThread(1);
            thread.AddComment(Comment.Create(new CommentAuthor("Ada"), "Hello"));
            Settle(window);
            double before = editor.GetTopForLineNumber(3);

            thread.AddComment(Comment.Create(new CommentAuthor("Bob"), "A reply\nover\nseveral\nlines"));
            Settle(window);

            Assert.True(editor.GetTopForLineNumber(3) > before);
            window.Close();
        });
    }

    [Fact]
    public async Task CloseThread_RemovesZone()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open();
            double before = editor.GetTopForLineNumber(3);
            CommentThread thread = editor.AddCommentThread(1);
            thread.AddComment(Comment.Create(new CommentAuthor("Ada"), "Hello"));
            Settle(window);

            Assert.True(editor.RemoveCommentThread(thread));
            Settle(window);

            Assert.Null(editor.CommentWidgetOf(thread));
            Assert.Equal(before, editor.GetTopForLineNumber(3));
            window.Close();
        });
    }

    [Fact]
    public async Task Peer_IsGroup_WithThreadName()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open();
            CommentThread thread = editor.AddCommentThread(2);
            Settle(window);

            AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(editor.CommentWidgetOf(thread)!);

            Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
            Assert.Contains("2", peer.GetName());
            window.Close();
        });
    }
}
