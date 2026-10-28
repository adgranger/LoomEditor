using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Comment;
using LoomEditor.View.Features.LinesOperations;
using LoomEditor.View.Features.SelectAll;

namespace LoomEditor.View.Tests.Features;

// Monaco guards the edits in the cursor (cursor.ts: readOnly checks) and with the `writable` precondition of the actions:
// the tests of this repository are all new (EditorContext driven, no WinUI).
public class ReadOnlyTests : IDisposable
{
    private sealed class FakeClipboard : IClipboardService
    {
        public string? Text { get; set; }

        public void WriteText(string text) => Text = text;

        public Task<string?> ReadTextAsync() => Task.FromResult(Text);
    }

    private readonly EditorContext _context = new();
    private readonly FakeClipboard _clipboard = new();
    private readonly EditorFeatureHost _host;
    private readonly IDisposable _clipboardCommands;

    public ReadOnlyTests()
    {
        _context.SetText("one\ntwo\nthree");
        _host = new EditorFeatureHost(_context, EditorFeatures.Default);
        _clipboardCommands = ClipboardCommands.Register(_context.Commands, _context.ViewModel, _clipboard);
        _context.IsReadOnly = true;
    }

    private string Text => _context.Model.GetValue();

    public void Dispose()
    {
        _clipboardCommands.Dispose();
        _host.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ReadOnly_Type_DoesNotChangeText()
    {
        _context.ViewModel.Type("x", "keyboard");

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_Backspace_Delete_DoNothing()
    {
        _context.Selection = Selection.Collapsed(new Position(1, 2));

        _context.Commands.Execute("deleteLeft");
        _context.Commands.Execute("deleteRight");

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public async Task ReadOnly_Paste_Cut_DoNothing_CopyStillWorks()
    {
        _context.Selection = new Selection(1, 1, 1, 4);

        _context.Commands.Execute("editor.action.clipboardCutAction");
        Assert.Equal("one", _clipboard.Text);
        Assert.Equal("one\ntwo\nthree", Text);

        _context.Commands.Execute("editor.action.clipboardCopyAction");
        _clipboard.Text = "pasted";
        await ClipboardCommands.PasteAsync(_context.ViewModel, _clipboard);

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_UndoRedo_DoNothing()
    {
        _context.IsReadOnly = false;
        _context.Selection = Selection.Collapsed(new Position(1, 1));
        _context.ViewModel.Type("x", "keyboard");
        _context.IsReadOnly = true;

        _context.Commands.Execute("undo");
        Assert.Equal("xone\ntwo\nthree", Text);

        _context.IsReadOnly = false;
        _context.Commands.Execute("undo");
        Assert.Equal("one\ntwo\nthree", Text);

        _context.IsReadOnly = true;
        _context.Commands.Execute("redo");
        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_MoveLinesDown_DoesNothing()
    {
        _context.Commands.Execute(LinesOperationsCommands.MoveLinesDown);

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_CommentLine_DoesNothing()
    {
        _context.Commands.Execute(CommentCommands.CommentLine);

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_Indent_DoesNothing()
    {
        _context.Commands.Execute(LinesOperationsCommands.IndentLines);
        _context.Commands.Execute("tab");

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_Composition_Ignored()
    {
        _context.ViewModel.StartComposition();
        _context.ViewModel.CompositionType("せ", 0, 0, 0);
        _context.ViewModel.EndComposition("keyboard");

        Assert.Equal("one\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_SelectAll_Navigation_Find_StillWork()
    {
        _context.Commands.Execute(SelectAllCommands.SelectAll);
        Assert.Equal(new Selection(1, 1, 3, 6), _context.Selection);

        _context.Selection = Selection.Collapsed(new Position(1, 1));
        _context.Commands.Execute("cursorDown");
        Assert.Equal(Selection.Collapsed(new Position(2, 1)), _context.Selection);

        Assert.True(_context.Commands.Execute("actions.find"));
        Assert.True(_context.FindController!.State.IsRevealed);
    }

    [Fact]
    public void ReadOnly_ModelApplyEdits_StillWorks()
    {
        _context.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "x")]);

        Assert.Equal("xone\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnly_SetFalse_EditingWorksAgain()
    {
        _context.IsReadOnly = false;

        _context.ViewModel.Type("x", "keyboard");

        Assert.Equal("xone\ntwo\nthree", Text);
    }

    [Fact]
    public void ReadOnlyChanged_RaisedOnChangeOnly()
    {
        int raised = 0;
        _context.ReadOnlyChanged += (_, _) => raised++;

        _context.IsReadOnly = true;
        Assert.Equal(0, raised);

        _context.IsReadOnly = false;
        _context.IsReadOnly = false;
        Assert.Equal(1, raised);
        Assert.False(_context.IsReadOnly);
    }

    [Fact]
    public void KeybindingContext_EditorReadOnly_ReflectsProperty()
    {
        var writable = new KeybindingContext();
        var readOnly = writable with { EditorReadOnly = true };

        Assert.True(WhenClauses.Writable(writable));
        Assert.False(WhenClauses.Writable(readOnly));
        Assert.True(_context.IsReadOnly);
    }

    [Fact]
    public void Keybinding_Indent_NotResolvedWhenReadOnly()
    {
        var indent = new Keybinding(KeyModifiers.Ctrl, KeyCode.BracketRight);
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true);

        Assert.Equal(LinesOperationsCommands.IndentLines, KeybindingResolver.Default.Resolve(indent, focused));
        Assert.Null(KeybindingResolver.Default.Resolve(indent, focused with { EditorReadOnly = true }));
    }
}
