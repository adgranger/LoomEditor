using LoomEditor.View.Commands;

namespace LoomEditor.View.Tests.Commands;

// Monaco has no Insert keybinding (the workbench owns it): all tests are new.
public class OvertypeKeybindingTests
{
    private static readonly KeybindingContext Focused = new(
        TextInputFocus: true, EditorTextFocus: true, TabDoesNotMoveFocus: true, EditorFocus: true);

    [Fact]
    public void Insert_Key_ResolvesToToggleOvertype()
    {
        Assert.Equal(
            "editor.action.toggleOvertype",
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Insert), Focused));
    }

    [Fact]
    public void Insert_Key_NotWhenFindInputFocused()
    {
        KeybindingContext findFocused = Focused with { EditorTextFocus = false, FindWidgetVisible = true, FindInputFocused = true };

        Assert.Null(KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.None, KeyCode.Insert), findFocused));
    }
}
