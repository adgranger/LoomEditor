using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Comments;
using LoomEditor.Themes;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class CommentThreadWidgetTests
{
    private static CommentThread CreateThread(CommentThreadState state = CommentThreadState.Open)
    {
        var thread = new CommentThread("t", 2) { State = state };
        thread.AddComment(Comment.Create(new CommentAuthor("Ada Lovelace"), "First **bold** comment\nsecond line"));
        thread.AddComment(Comment.Create(new CommentAuthor("Bob"), "Reply"));
        return thread;
    }

    private static CommentThreadWidget CreateWidget(CommentThread thread) =>
        new(new CommentThreadViewModel(thread, () => "line"));

    // Every text the widget shows: text blocks, and the runs of the Markdown rows.
    private static List<string> Texts(CommentThreadWidget widget)
    {
        var texts = new List<string>();
        foreach (ILogical logical in widget.GetLogicalDescendants())
        {
            if (logical is TextBlock block)
            {
                texts.Add(block.Text ?? string.Concat(block.Inlines!.OfType<Run>().Select(run => run.Text)));
            }
        }

        return texts;
    }

    [Fact]
    public async Task Widget_ShowsEveryComment()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var widget = CreateWidget(CreateThread());

            List<string> texts = Texts(widget);

            Assert.Contains("Ada Lovelace", texts);
            Assert.Contains("Bob", texts);
            Assert.Contains(texts, text => text.Contains("bold", StringComparison.Ordinal));
            Assert.Contains("Reply", texts);
        });
    }

    [Fact]
    public async Task Header_ShowsResolvedState()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var widget = CreateWidget(CreateThread(CommentThreadState.Resolved));

            Assert.Contains(CommentStrings.Resolved, Texts(widget));
        });
    }

    [Fact]
    public async Task Collapsed_ShowsPreviewOnly()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var widget = CreateWidget(CreateThread(CommentThreadState.Resolved));

            List<string> texts = Texts(widget);

            // The summary line of the first comment, no comment row.
            Assert.DoesNotContain("Bob", texts);
            Assert.Contains(texts, text => text.StartsWith("First bold comment", StringComparison.Ordinal) && text.Contains(CommentStrings.CommentCount(2), StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Avatar_NoImage_ShowsInitials()
    {
        await HeadlessSession.RunAsync(() =>
        {
            Border avatar = CommentAvatar.Create("Ada Lovelace", 14);

            var initials = Assert.IsType<TextBlock>(avatar.Child);
            Assert.Equal("AL", initials.Text);
            Assert.Equal(28, avatar.Width);
        });
    }

    [Fact]
    public async Task Theme_Dark_UsesThemeColors()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var widget = CreateWidget(CreateThread());
            EditorTheme theme = EditorTheme.Dark;

            widget.ApplyTheme(theme);

            ThemeColor expected = theme.GetColor("editorWidget.background") ?? theme.GetColor("editorHoverWidget.background") ?? default;
            global::Avalonia.Media.Color actual = Assert.IsAssignableFrom<ISolidColorBrush>(widget.Box.Background).Color;
            Assert.Equal(global::Avalonia.Media.Color.FromArgb(expected.A, expected.R, expected.G, expected.B), actual);
        });
    }
}
