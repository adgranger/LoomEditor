using LoomEditor.View.Commands;
using LoomEditor.View.Features.TabFocus;

namespace LoomEditor.View.Tests.Commands;

// New tests: keybindings of toggleTabFocusMode.ts (no kbExpr) and of the Tab / outdent rules.
public class TabFocusKeybindingsTests
{
    private static readonly KeybindingContext Focused = new(EditorTextFocus: true, TabDoesNotMoveFocus: true);

    private static string? Resolve(KeyModifiers modifiers, KeyCode key, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, key), context);

    [Fact]
    public void CtrlM_ResolvesToToggleTabFocusMode_WhateverTheContext()
    {
        Assert.Equal(TabFocusCommands.ToggleTabFocusMode, Resolve(KeyModifiers.Ctrl, KeyCode.KeyM, default));
        Assert.Equal(TabFocusCommands.ToggleTabFocusMode, Resolve(KeyModifiers.Ctrl, KeyCode.KeyM, Focused));
    }

    [Fact]
    public void Tab_WithTabDoesNotMoveFocusFalse_ResolvesToNothing() =>
        Assert.Null(Resolve(KeyModifiers.None, KeyCode.Tab, Focused with { TabDoesNotMoveFocus = false }));

    [Fact]
    public void Tab_WithTabDoesNotMoveFocusTrue_ResolvesToTab() =>
        Assert.Equal("tab", Resolve(KeyModifiers.None, KeyCode.Tab, Focused));

    [Fact]
    public void ShiftTab_WithTabDoesNotMoveFocusFalse_ResolvesToNothing() =>
        Assert.Null(Resolve(KeyModifiers.Shift, KeyCode.Tab, Focused with { TabDoesNotMoveFocus = false }));
}
