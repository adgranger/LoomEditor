using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestControllerAcceptTests
{
    private static SuggestItem Item(string label, Position position, Range range, string? insertText = null, params TextEdit[] additionalEdits)
    {
        var completion = new CompletionItem(label, insertText ?? label, CompletionItemKind.Property)
        {
            Range = CompletionRange.FromRange(range),
            AdditionalTextEdits = additionalEdits.Length == 0 ? null : additionalEdits,
        };
        return new SuggestItem(position, completion, new CompletionList([completion], false), new FakeCompletionProvider());
    }

    private static SuggestController Controller(SuggestTestEditor editor) => new(editor.Context, SuggestModelOptions.Default);

    [Fact]
    public void Accept_ReplacesTheTypedPrefix()
    {
        using var editor = new SuggestTestEditor("let value = fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 15);

        bool accepted = controller.Accept(Item("foo", new Position(1, 15), new Range(1, 13, 1, 15)));

        Assert.True(accepted);
        Assert.Equal("let value = foo", editor.Model.GetValue());
        Assert.Equal(new Position(1, 16), editor.Context.Selection.Position);
        Assert.True(editor.Context.Selection.IsEmpty);
    }

    [Fact]
    public void Accept_InsertsAtEmptyWord()
    {
        using var editor = new SuggestTestEditor("let value = ");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 13);

        controller.Accept(Item("foo", new Position(1, 13), new Range(1, 13, 1, 13)));

        Assert.Equal("let value = foo", editor.Model.GetValue());
        Assert.Equal(new Position(1, 16), editor.Context.Selection.Position);
    }

    [Fact]
    public void Accept_UsesCurrentCursor_WhenTypedAfterRequest()
    {
        using var editor = new SuggestTestEditor("let value = foo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 16);

        // requested at column 15, when only "fo" was typed
        controller.Accept(Item("foobar", new Position(1, 15), new Range(1, 13, 1, 15)));

        Assert.Equal("let value = foobar", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_OverwriteAfter_UsesTheInsertEnd()
    {
        using var editor = new SuggestTestEditor("let value = foxx");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 15);

        controller.Accept(Item("foo", new Position(1, 15), new Range(1, 13, 1, 17)));

        Assert.Equal("let value = foo", editor.Model.GetValue());
        Assert.Equal(new Position(1, 16), editor.Context.Selection.Position);
    }

    [Fact]
    public void Accept_InsertText_DiffersFromLabel()
    {
        using var editor = new SuggestTestEditor("fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);

        controller.Accept(Item("foo()", new Position(1, 3), new Range(1, 1, 1, 3), "foo"));

        Assert.Equal("foo", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_AdditionalEdits_AppliedBeforeCursorShift()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(2, 15);

        controller.Accept(Item("foo", new Position(2, 15), new Range(2, 13, 2, 15), null, new TextEdit(new Range(1, 1, 1, 1), "using X;\n")));

        Assert.Equal("using X;\nx\nlet value = foo", editor.Model.GetValue());
        Assert.Equal(new Position(3, 16), editor.Context.Selection.Position);
    }

    [Fact]
    public void Accept_AdditionalEdit_OnSameLineAfterCursor_IsShifted()
    {
        using var editor = new SuggestTestEditor("foo bar");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 4);

        // requested at column 3 ("fo"): the edit at column 4 was before "bar" then, and is at column 5 now
        controller.Accept(Item("foobaz", new Position(1, 3), new Range(1, 1, 1, 3), null, new TextEdit(new Range(1, 4, 1, 4), "X")));

        Assert.Equal("foobaz Xbar", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_IsOneUndoStep()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(2, 15);

        controller.Accept(Item("foo", new Position(2, 15), new Range(2, 13, 2, 15), null, new TextEdit(new Range(1, 1, 1, 1), "using X;\n")));
        editor.Model.Undo();

        Assert.Equal("x\nlet value = fo", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_AnnouncesAdditionalEdits()
    {
        using var editor = new SuggestTestEditor("fo");
        var announcer = new RecordingAnnouncer();
        editor.Context.Announcer = announcer;
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);

        controller.Accept(Item("foo", new Position(1, 3), new Range(1, 1, 1, 3), null, new TextEdit(new Range(1, 1, 1, 1), "// ")));

        Assert.Equal(["Accepting 'foo' made 1 additional edits"], announcer.Messages);
    }

    [Fact]
    public void Accept_CancelsTheSession()
    {
        using var editor = new SuggestTestEditor("abc");
        var presenter = new FakeSuggestPresenter();
        editor.Context.SuggestPresenter = presenter;
        editor.Providers.Register("plaintext", new FakeCompletionProvider
        {
            OnProvide = (_, position, _) => new CompletionList(
                [new CompletionItem("abcd", "abcd", CompletionItemKind.Property) { Range = CompletionRange.FromRange(new Range(1, 1, 1, position.Column)) }],
                false),
        });
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 4);
        controller.Trigger();
        editor.Flush();
        Assert.True(presenter.IsVisible);

        Assert.True(controller.AcceptSelected());

        Assert.Equal("abcd", editor.Model.GetValue());
        Assert.Equal(SuggestState.Idle, controller.Model.State);
        Assert.False(presenter.IsVisible);
    }

    [Fact]
    public void Accept_WrongLine_ReturnsFalse_NoEdit()
    {
        using var editor = new SuggestTestEditor("fo\nbar");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(2, 2);

        bool accepted = controller.Accept(Item("foo", new Position(1, 3), new Range(1, 1, 1, 3)));

        Assert.False(accepted);
        Assert.Equal("fo\nbar", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_ReadOnly_ReturnsFalse()
    {
        using var editor = new SuggestTestEditor("fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);
        editor.Context.ViewModel.SetCursorOptions(EditorCursorOptions.Default with { ReadOnly = true });

        bool accepted = controller.Accept(Item("foo", new Position(1, 3), new Range(1, 1, 1, 3)));

        Assert.False(accepted);
        Assert.Equal("fo", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_OnlyPrimaryCursor()
    {
        using var editor = new SuggestTestEditor("fo\nfo");
        using SuggestController controller = Controller(editor);
        editor.Context.ViewModel.SetSelections("test", [Selection.Collapsed(new Position(1, 3)), Selection.Collapsed(new Position(2, 3))]);
        Position primary = editor.Context.Selection.Position;

        controller.Accept(Item("foo", primary, new Range(primary.LineNumber, 1, primary.LineNumber, 3)));

        string[] lines = [editor.Model.GetLineContent(1), editor.Model.GetLineContent(2)];
        Assert.Equal("foo", lines[primary.LineNumber - 1]);
        Assert.Equal("fo", lines[2 - primary.LineNumber]);
    }

    [Fact]
    public void AcceptSelected_NoSelection_False()
    {
        using var editor = new SuggestTestEditor("fo");
        using SuggestController controller = Controller(editor);

        Assert.False(controller.AcceptSelected());
    }

    [Fact]
    public void Accept_MultiLineInsertText_NotReindented()
    {
        using var editor = new SuggestTestEditor("  fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 5);

        controller.Accept(Item("foo", new Position(1, 5), new Range(1, 3, 1, 5), "foo\nbar"));

        Assert.Equal("  foo\nbar", editor.Model.GetValue());
        Assert.Equal(new Position(2, 4), editor.Context.Selection.Position);
    }
}
