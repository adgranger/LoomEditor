using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Comments;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class CommentComposerTests
{
    private static readonly RawInputModifiers CommandKey = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    private static (Window Window, CommentThreadWidget Widget, CommentThreadViewModel ViewModel) Open(CommentThread thread, string lineText = "old line")
    {
        var viewModel = new CommentThreadViewModel(thread, () => lineText);
        var widget = new CommentThreadWidget(viewModel);
        var window = new Window { Width = 500, Height = 400, Content = widget };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, widget, viewModel);
    }

    private static void StartComposing(Window window, CommentThreadWidget widget, CommentThreadViewModel viewModel)
    {
        viewModel.StartComposing();
        Dispatcher.UIThread.RunJobs();
        widget.TextBox.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(widget.TextBox.IsFocused, "the text box has no focus");
    }

    [Fact]
    public async Task Composer_CtrlEnter_RaisesCommentSubmitted()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Window window, CommentThreadWidget widget, CommentThreadViewModel viewModel) = Open(new CommentThread("t", 1));
            string? submitted = null;
            viewModel.SubmitRequested += (_, text) => submitted = text;
            StartComposing(window, widget, viewModel);
            widget.TextBox.Text = "hello";

            window.KeyPress(Key.Enter, CommandKey, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("hello", submitted);
            window.Close();
        });
    }

    [Fact]
    public async Task Composer_Escape_Cancels()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Window window, CommentThreadWidget widget, CommentThreadViewModel viewModel) = Open(new CommentThread("t", 1));
            bool focusReturned = false;
            widget.FocusReturnRequested += (_, _) => focusReturned = true;
            StartComposing(window, widget, viewModel);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(viewModel.IsComposing);
            Assert.True(focusReturned);
            window.Close();
        });
    }

    [Fact]
    public async Task Toolbar_Bold_WrapsSelection()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Window window, CommentThreadWidget widget, CommentThreadViewModel viewModel) = Open(new CommentThread("t", 1));
            StartComposing(window, widget, viewModel);
            widget.TextBox.Text = "word";
            widget.TextBox.SelectionStart = 0;
            widget.TextBox.SelectionEnd = 4;
            Button bold = widget.Toolbar.Buttons[(int)MarkdownToolbarAction.Bold];

            bold.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("**word**", widget.TextBox.Text);
            Assert.Equal("**word**", viewModel.Draft);
            window.Close();
        });
    }

    [Fact]
    public async Task Suggestion_Apply_InvokesApplySuggestion()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var thread = new CommentThread("t", 1);
            Comment comment = Comment.Create(new CommentAuthor("Ada"), "Try this:\n```suggestion\nnew line\n```");
            thread.AddComment(comment);
            (Window window, CommentThreadWidget widget, CommentThreadViewModel _) = Open(thread);
            (string Id, int Index, string Range)? applied = null;
            widget.ApplySuggestion = (id, index, range) =>
            {
                applied = (id, index, range);
                return ApplySuggestionResult.Applied;
            };
            widget.Refresh();
            Dispatcher.UIThread.RunJobs();
            Button apply = widget.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == CommentStrings.ApplySuggestion);

            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal((comment.Id, 0, "old line"), applied);
            window.Close();
        });
    }
}
