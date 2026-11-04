using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Themes;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class CommentGutterTests
{
    private static (Editor Editor, Window Window, CommentGutterButton Button) Open()
    {
        var editor = new Editor { Text = "one\ntwo\nthree\nfour", MinimapEnabled = false, CommentGutterButtonEnabled = true };
        Window window = EditorWindow.Open(editor);
        EditorWindow.Capture(window);
        CommentGutterButton button = editor.CommentGutter!.Button;
        return (editor, window, button);
    }

    private static Point InGutter(Editor editor, int line) =>
        new(editor.ViewModel.LayoutInfo.LineNumbersLeft + 4, (line - 0.5) * editor.LineHeight);

    private static void Hover(Window window, Point point)
    {
        window.MouseMove(point);
        Dispatcher.UIThread.RunJobs();
        EditorWindow.Capture(window);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task PointerOverLine_ShowsPlus_AtThatLine()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, CommentGutterButton button) = Open();
            Assert.False(button.IsVisible);

            Hover(window, InGutter(editor, 3));

            Assert.True(button.IsVisible);
            Assert.Equal(3, button.LineNumber);
            Assert.Equal(editor.GetTopForLineNumber(3) + ((editor.LineHeight - CommentGutterButton.Size) / 2), Canvas.GetTop(button));
            window.Close();
        });
    }

    [Fact]
    public async Task ClickPlus_RaisesAddCommentRequested_WithLine()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, CommentGutterButton button) = Open();
            AddCommentRequestedEventArgs? received = null;
            editor.AddCommentRequested += (_, e) => received = e;
            Hover(window, InGutter(editor, 2));

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.NotNull(received);
            Assert.Equal(2, received.LineNumber);
            Assert.Equal(2, received.StartLineNumber);
            Assert.Null(received.TextRange);
            window.Close();
        });
    }

    [Fact]
    public async Task PointerLeaves_HidesPlus()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, CommentGutterButton button) = Open();
            Hover(window, InGutter(editor, 2));
            Assert.True(button.IsVisible);

            Hover(window, new Point(editor.ViewModel.LayoutInfo.ContentLeft + 40, 10));

            Assert.False(button.IsVisible);
            window.Close();
        });
    }

    [Fact]
    public async Task Theme_Change_RecolorsPlus()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window, CommentGutterButton button) = Open();
            editor.Theme = EditorTheme.Dark;
            global::Avalonia.Media.Color dark = Assert.IsAssignableFrom<ISolidColorBrush>(button.Background).Color;

            editor.Theme = EditorTheme.Light;

            global::Avalonia.Media.Color light = Assert.IsAssignableFrom<ISolidColorBrush>(button.Background).Color;
            Assert.NotEqual(dark, light);
            window.Close();
        });
    }
}
