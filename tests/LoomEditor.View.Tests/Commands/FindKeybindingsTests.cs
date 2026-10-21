using LoomEditor.View.Commands;

namespace LoomEditor.View.Tests.Commands;

public class FindKeybindingsTests
{
    private static readonly KeybindingContext Focused = new(
        TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true, EditorFocus: true);

    private static string? Resolve(KeyModifiers modifiers, KeyCode key, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, key), context);

    [Fact]
    public void Escape_FindWidgetVisible_ClosesFindWidget()
    {
        KeybindingContext context = Focused with { FindWidgetVisible = true, HasMultipleSelections = true, HasNonEmptySelection = true };

        Assert.Equal("closeFindWidget", Resolve(KeyModifiers.None, KeyCode.Escape, context));
        Assert.Equal("closeFindWidget", Resolve(KeyModifiers.Shift, KeyCode.Escape, context));
    }

    [Fact]
    public void Escape_FindWidgetHidden_CancelsSelection()
    {
        Assert.Equal("cancelSelection", Resolve(KeyModifiers.None, KeyCode.Escape, Focused with { HasNonEmptySelection = true }));
    }

    [Fact]
    public void Enter_FindInputFocused_NextMatch()
    {
        KeybindingContext context = Focused with { FindWidgetVisible = true, FindInputFocused = true };

        Assert.Equal("editor.action.nextMatchFindAction", Resolve(KeyModifiers.None, KeyCode.Enter, context));
        Assert.Equal("editor.action.previousMatchFindAction", Resolve(KeyModifiers.Shift, KeyCode.Enter, context));
    }

    [Fact]
    public void Enter_ReplaceInputFocused_ReplaceOne()
    {
        KeybindingContext context = Focused with { FindWidgetVisible = true, ReplaceInputFocused = true };

        Assert.Equal("editor.action.replaceOne", Resolve(KeyModifiers.None, KeyCode.Enter, context));
    }

    [Fact]
    public void F3_EditorFocus_NextMatch()
    {
        Assert.Equal("editor.action.nextMatchFindAction", Resolve(KeyModifiers.None, KeyCode.F3, Focused));
        Assert.Null(Resolve(KeyModifiers.None, KeyCode.F3, default));
    }

    [Fact]
    public void CtrlShift1_FindWidgetVisible_ReplaceOne()
    {
        const KeyModifiers CtrlShift = KeyModifiers.Ctrl | KeyModifiers.Shift;

        Assert.Equal("editor.action.replaceOne", Resolve(CtrlShift, KeyCode.Digit1, Focused with { FindWidgetVisible = true }));
        Assert.Null(Resolve(CtrlShift, KeyCode.Digit1, Focused));
    }

    [Fact]
    public void AltC_EditorFocus_ToggleCaseSensitive()
    {
        Assert.Equal("toggleFindCaseSensitive", Resolve(KeyModifiers.Alt, KeyCode.KeyC, Focused));
    }

    [Fact]
    public void CtrlFAndCtrlH_AlwaysOpenFind()
    {
        Assert.Equal("actions.find", Resolve(KeyModifiers.Ctrl, KeyCode.KeyF, default));
        Assert.Equal("editor.action.startFindReplaceAction", Resolve(KeyModifiers.Ctrl, KeyCode.KeyH, default));
    }
}
