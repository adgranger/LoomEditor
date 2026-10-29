using LoomEditor.Core.Model;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.Snippet;
using LoomEditor.View.Features.Suggest;

namespace LoomEditor.View.Tests.Features.Snippet;

public partial class SnippetControllerTests
{
    private sealed class FakeHost(string? clipboard) : ISnippetVariableHost
    {
        public string? FilePath => null;

        public string? RelativeFilePath => null;

        public string? WorkspaceName => null;

        public string? WorkspaceFolder => null;

        public string? ReadClipboardText() => clipboard;
    }

    // Monaco: 'Cancelling snippet mode should discard added cursors #68512 (soft cancel)'
    [Fact]
    public void Issue68512_SoftCancel()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert(".REGION ${2:FUNCTION_NAME}\nCREATE.FUNCTION ${1:VOID} ${2:FUNCTION_NAME}(${3:})\n\t${4:}\nEND\n.ENDREGION$0");
        AssertSelections(new Selection(2, 17, 2, 21));

        Ctrl.Next();
        AssertSelections(new Selection(1, 9, 1, 22), new Selection(2, 22, 2, 35));
        AssertContextKeys(true, true, true);

        Context.SetSelections([new Selection(1, 22, 1, 22), new Selection(2, 35, 2, 35)]);
        AssertContextKeys(true, true, true);

        Context.SetSelections([new Selection(2, 1, 2, 1), new Selection(2, 36, 2, 36)]);
        AssertContextKeys(false, false, false);
        AssertSelections(new Selection(2, 1, 2, 1), new Selection(2, 36, 2, 36));
    }

    // Monaco: 'Cancelling snippet mode should discard added cursors #68512 (hard cancel)'
    [Fact]
    public void Issue68512_HardCancel()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert(".REGION ${2:FUNCTION_NAME}\nCREATE.FUNCTION ${1:VOID} ${2:FUNCTION_NAME}(${3:})\n\t${4:}\nEND\n.ENDREGION$0");
        AssertSelections(new Selection(2, 17, 2, 21));

        Ctrl.Next();
        AssertSelections(new Selection(1, 9, 1, 22), new Selection(2, 22, 2, 35));
        AssertContextKeys(true, true, true);

        Context.SetSelections([new Selection(1, 22, 1, 22), new Selection(2, 35, 2, 35)]);
        AssertContextKeys(true, true, true);

        Ctrl.Cancel(true);
        AssertContextKeys(false, false, false);
        AssertSelections(new Selection(1, 22, 1, 22));
    }

    // Monaco: 'User defined snippet tab stops ignored #72862'
    [Fact]
    public void Issue72862_UserDefinedSnippetTabStops()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("export default $1");
        AssertContextKeys(true, false, true);
    }

    // Monaco: 'Optional tabstop in snippets #72358'
    [Fact]
    public void Issue72358_OptionalTabstop()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("${1:prop: {$2\\},}\nmore$0");
        AssertContextKeys(true, false, true);

        AssertSelections(new Selection(1, 1, 1, 10));
        Context.ViewModel.Cut("test");

        AssertSelections(new Selection(1, 1, 1, 1));

        Ctrl.Next();
        AssertSelections(new Selection(2, 5, 2, 5));
        AssertContextKeys(false, false, false);
    }

    // Monaco: 'issue #90135: confusing trim whitespace edits'
    [Fact]
    public void Issue90135_ConfusingTrimWhitespaceEdits()
    {
        SetValue(string.Empty);
        Context.Commands.Execute("tab");

        Insert("\nfoo");
        AssertSelections(new Selection(2, 8, 2, 8));
    }

    // Monaco: 'issue #145727: insertSnippet can put snippet selections in wrong positions (1 of 2)'
    [Fact]
    public void Issue145727_Part1()
    {
        SetValue(string.Empty);
        Context.Commands.Execute("tab");

        Insert("\naProperty: aClass<${2:boolean}> = new aClass<${2:boolean}>();\n", new SnippetInsertOptions { AdjustWhitespace = false });
        AssertSelections(new Selection(2, 19, 2, 26), new Selection(2, 41, 2, 48));
    }

    // Monaco: 'issue #145727: insertSnippet can put snippet selections in wrong positions (2 of 2)'
    [Fact]
    public void Issue145727_Part2()
    {
        SetValue(string.Empty);
        Context.Commands.Execute("tab");

        Insert("\naProperty: aClass<${2:boolean}> = new aClass<${2:boolean}>();\n");

        // This inserts \n    aProperty....
        AssertSelections(new Selection(2, 23, 2, 30), new Selection(2, 45, 2, 52));
    }

    // Monaco: 'leading TAB by snippets won't replace by spaces #101870'
    [Fact]
    public void Issue101870_LeadingTab()
    {
        SetValue(string.Empty);
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: true, TabSize: 4));
        Insert("\tHello World\n\tNew Line");
        Assert.Equal("    Hello World\n    New Line", Model.GetValue());
    }

    // Monaco: 'leading TAB by snippets won't replace by spaces #101870 (part 2)'
    [Fact]
    public void Issue101870_LeadingTab_Part2()
    {
        SetValue(string.Empty);
        Model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: true, TabSize: 4));
        Insert("\tHello World\n\tNew Line\n${1:\tmore}");
        Assert.Equal("    Hello World\n    New Line\n    more", Model.GetValue());
    }

    // Monaco: 'Bug: cursor position $0 with user snippets #163808'
    [Fact]
    public void Issue163808_CursorPositionFinalTabstop()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("<Element1 Attr1=\"foo\" $1>\n  <Element2 Attr1=\"$2\"/>\n$0\"\n</Element1>");
        AssertSelections(new Selection(1, 23, 1, 23));
        Insert("Qualifier=\"$0\"");
        Assert.Equal("<Element1 Attr1=\"foo\" Qualifier=\"\">\n  <Element2 Attr1=\"\"/>\n\"\n</Element1>", Model.GetValue());
        AssertSelections(new Selection(1, 34, 1, 34));
    }

    [Fact]
    public void Insert_PlainText_NoSnippetMode()
    {
        Insert("abc");

        Assert.False(Ctrl.IsInSnippet);
        AssertContextKeys(false, false, false);
    }

    [Fact]
    public void Insert_WithTabstops_EntersSnippetMode_SelectsFirstPlaceholder()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);

        Insert("a${1:b}c$0");

        Assert.True(Ctrl.IsInSnippet);
        AssertSelections(new Selection(1, 2, 1, 3));
        Assert.NotNull(Ctrl.GetSessionEnclosingRange());
    }

    [Fact]
    public void Tab_JumpsToNextPlaceholder_ThenLeavesAtFinal()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("${1:a}-${2:b}$0");

        Assert.True(Context.Commands.Execute(SnippetCommands.JumpToNextSnippetPlaceholder));
        AssertSelections(new Selection(1, 3, 1, 4));
        Assert.True(Context.Commands.Execute(SnippetCommands.JumpToNextSnippetPlaceholder));

        AssertSelections(new Selection(1, 4, 1, 4));
        AssertContextKeys(false, false, false);
        Assert.False(Context.Commands.Execute(SnippetCommands.JumpToNextSnippetPlaceholder));
    }

    [Fact]
    public void ShiftTab_GoesBack()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("${1:a}-${2:b}$0");
        Assert.False(Context.Commands.Execute(SnippetCommands.JumpToPrevSnippetPlaceholder));
        Ctrl.Next();

        Assert.True(Context.Commands.Execute(SnippetCommands.JumpToPrevSnippetPlaceholder));

        AssertSelections(new Selection(1, 1, 1, 2));
        AssertContextKeys(true, false, true);
    }

    [Fact]
    public void Escape_LeavesSnippet_KeepsText()
    {
        Insert("a${1:b}c$0");
        string text = Model.GetValue();

        Assert.True(Context.Commands.Execute(SnippetCommands.LeaveSnippet));

        AssertContextKeys(false, false, false);
        Assert.Equal(text, Model.GetValue());
        Assert.Single(Context.Selections);
    }

    [Fact]
    public void Undo_AfterInsert_LeavesSnippetMode()
    {
        Insert("a${1:b}c$0");
        Assert.True(Ctrl.IsInSnippet);

        Model.Undo();

        Assert.Equal("if\n    $state\nfi", Model.GetValue());
        AssertContextKeys(false, false, false);
    }

    [Fact]
    public void CaretOutsidePlaceholders_LeavesSnippetMode()
    {
        Insert("a${1:b}c$0");

        Context.SetSelections([new Selection(3, 1, 3, 1), new Selection(3, 2, 3, 2)]);

        AssertContextKeys(false, false, false);
    }

    [Fact]
    public void Tab_NotInSnippetMode_InsertsTab()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        KeybindingContext keys = EditorKeys();

        Assert.Equal("tab", KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Tab), keys));
        Context.Commands.Execute("tab");

        Assert.NotEqual(string.Empty, Model.GetValue());
    }

    [Fact]
    public void Tab_InSnippetMode_JumpsAndEscapeLeaves()
    {
        Insert("${1:a}-${2:b}$0");
        KeybindingContext keys = EditorKeys();
        var tab = new Keybinding(KeyModifiers.None, KeyCode.Tab);

        Assert.Equal(SnippetCommands.JumpToNextSnippetPlaceholder, KeybindingResolver.Default.Resolve(tab, keys));
        Assert.Equal(SnippetCommands.LeaveSnippet, KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Escape), keys));
        Assert.Equal(SnippetCommands.LeaveSnippet, KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Shift, KeyCode.Escape), keys));
        Assert.Equal("outdent", KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Shift, KeyCode.Tab), keys));
    }

    [Fact]
    public void Tab_WithSuggestWidgetVisible_AcceptsSuggestion()
    {
        Insert("${1:a}-${2:b}$0");
        KeybindingContext keys = EditorKeys() with { SuggestWidgetVisible = true };

        string? command = KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Tab), keys);
        string? escape = KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Escape), keys);

        Assert.Equal(SuggestCommands.AcceptSelectedSuggestion, command);
        Assert.Equal(SuggestCommands.HideSuggestWidget, escape);
    }

    [Fact]
    public void ContextKeys_HasNextHasPrev()
    {
        Insert("${1:a}${2:b}${3:c}$0");
        AssertContextKeys(true, false, true);

        Ctrl.Next();
        AssertContextKeys(true, true, true);

        Ctrl.Next();
        AssertContextKeys(true, true, true);

        Ctrl.Prev();
        Ctrl.Prev();
        AssertContextKeys(true, false, true);
    }

    [Fact]
    public void Finish_WalksToTheEnd()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("${1:a}-${2:b}$0");

        Assert.True(Context.Commands.Execute(SnippetCommands.AcceptSnippet));

        AssertSelections(new Selection(1, 4, 1, 4));
        AssertContextKeys(false, false, false);
    }

    [Fact]
    public void Insert_MultiCursor_AllGetPlaceholders()
    {
        Insert("x${1:y}$0");

        Assert.Equal("xyif\n    xy$state\nfi", Model.GetValue());
        AssertSelections(new Selection(1, 2, 1, 3), new Selection(2, 6, 2, 7));
    }

    [Fact]
    public void Insert_ReadOnly_DoesNothing()
    {
        Context.IsReadOnly = true;

        Insert("x${1:y}$0");

        Assert.Equal("if\n    $state\nfi", Model.GetValue());
        Assert.False(Ctrl.IsInSnippet);
    }

    [Fact]
    public void Insert_Nested_MergesIntoActivePlaceholder()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("a${1:b}c$0");

        Insert("[${1:x}]$0");

        Assert.Equal("a[x]c", Model.GetValue());
        AssertSelections(new Selection(1, 3, 1, 4));
        Assert.True(Ctrl.IsInSnippet);
    }

    [Fact]
    public void Typing_InPlaceholder_ReplacesDefaultText()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("a${1:foo}b$0");

        Type("X");

        Assert.Equal("aXb", Model.GetValue());
        Assert.True(Ctrl.IsInSnippet);
    }

    [Fact]
    public void Typing_InPlaceholder_UpdatesMirroredPlaceholders()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Insert("${1:i}-$1-$1$0");
        AssertSelections(new Selection(1, 1, 1, 2), new Selection(1, 3, 1, 4), new Selection(1, 5, 1, 6));

        Type("k");

        Assert.Equal("k-k-k", Model.GetValue());
    }

    [Fact]
    public void ClipboardVariable_UsesClipboardService()
    {
        SetValue(string.Empty);
        Context.SetSelections([new Selection(1, 1, 1, 1)]);
        Context.SnippetVariableHost = new FakeHost("clip");

        Insert("[$CLIPBOARD]");
        Assert.Equal("[clip]", Model.GetValue());

        SetValue(string.Empty);
        Insert("[$CLIPBOARD]", new SnippetInsertOptions { ClipboardText = "opt" });
        Assert.Equal("[opt]", Model.GetValue());
    }

    private KeybindingContext EditorKeys() => new(
        TextInputFocus: true,
        EditorTextFocus: true,
        TabDoesNotMoveFocus: true,
        InSnippetMode: Context.InSnippetMode,
        HasNextTabstop: Context.HasNextTabstop,
        HasPrevTabstop: Context.HasPrevTabstop);
}
