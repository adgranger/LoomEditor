using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;

namespace LoomEditor.View.Tests.Controller;

// The clipboard metadata suite of Monaco uses browser events and is not ported: all tests are new.
public class ClipboardCommandsTests
{
    private sealed class FakeClipboard : IClipboardService
    {
        public string? Text { get; set; }

        public void WriteText(string text) => Text = text;

        public Task<string?> ReadTextAsync() => Task.FromResult(Text);
    }

    private static (EditorContext Context, FakeClipboard Clipboard) Create(string text, EditorCursorOptions? options = null)
    {
        var context = new EditorContext();
        context.SetText(text);
        if (options is not null)
        {
            context.ViewModel.SetCursorOptions(options);
        }

        var clipboard = new FakeClipboard();
        ClipboardCommands.Register(context.Commands, context.ViewModel, clipboard);
        return (context, clipboard);
    }

    [Fact]
    public async Task Copy_TwoCursors_PutsOneTextPerCursorAndPasteSpreadsThem()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("a\nb\nc");
        context.SetSelections([new Selection(1, 1, 1, 2), new Selection(3, 1, 3, 2)]);

        context.Commands.Execute("editor.action.clipboardCopyAction");

        Assert.Equal("a\nc", clipboard.Text);

        context.SetSelections([new Selection(2, 1, 2, 1), new Selection(3, 1, 3, 1)]);
        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("a\nab\ncc", context.Model.GetValue());
    }

    [Fact]
    public async Task Copy_EmptySelection_CopiesTheWholeLineAndPasteInsertsItAboveTheLine()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("first\nsecond\nthird");
        context.Selection = Selection.Collapsed(new Position(1, 3));

        context.Commands.Execute("editor.action.clipboardCopyAction");

        Assert.Equal("first\r\n", clipboard.Text);

        context.Selection = Selection.Collapsed(new Position(3, 2));
        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("first\nsecond\nfirst\nthird", context.Model.GetValue());
    }

    [Fact]
    public async Task Paste_ClipboardChangedByAnotherApp_IgnoresTheMetadata()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("first\nsecond");
        context.Selection = Selection.Collapsed(new Position(1, 3));
        context.Commands.Execute("editor.action.clipboardCopyAction");
        clipboard.Text = "other";

        context.Selection = Selection.Collapsed(new Position(2, 1));
        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("first\nothersecond", context.Model.GetValue());
    }

    [Fact]
    public void Cut_EmptySelection_RemovesTheLine()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("first\nsecond\nthird");
        context.Selection = Selection.Collapsed(new Position(2, 3));

        context.Commands.Execute("editor.action.clipboardCutAction");

        Assert.Equal("second\r\n", clipboard.Text);
        Assert.Equal("first\nthird", context.Model.GetValue());
    }

    [Fact]
    public void Cut_EmptySelectionClipboardOff_DoesNothing()
    {
        (EditorContext context, FakeClipboard clipboard) = Create(
            "first\nsecond", new EditorCursorOptions { EmptySelectionClipboard = false });
        context.Selection = Selection.Collapsed(new Position(2, 3));

        context.Commands.Execute("editor.action.clipboardCutAction");

        Assert.Null(clipboard.Text);
        Assert.Equal("first\nsecond", context.Model.GetValue());
    }

    [Fact]
    public void Copy_NonEmptySelection_CopiesTheTextAndKeepsTheModel()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("hello world");
        context.Selection = new Selection(1, 1, 1, 6);

        context.Commands.Execute("editor.action.clipboardCopyAction");

        Assert.Equal("hello", clipboard.Text);
        Assert.Equal("hello world", context.Model.GetValue());
    }

    [Fact]
    public void Execute_Paste_ReturnsTrueAndInsertsTheText()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("ab");
        clipboard.Text = "XY";
        context.Selection = Selection.Collapsed(new Position(1, 2));

        bool ran = context.Commands.Execute("editor.action.clipboardPasteAction");

        Assert.True(ran);
        Assert.Equal("aXYb", context.Model.GetValue());
    }

    [Fact]
    public async Task PasteAsync_EmptyOrMissingClipboard_DoesNothing()
    {
        (EditorContext context, FakeClipboard clipboard) = Create("ab");

        clipboard.Text = null;
        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);
        clipboard.Text = string.Empty;
        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("ab", context.Model.GetValue());
    }
}
