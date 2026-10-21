using LoomEditor.View.Commands;

namespace LoomEditor.View.Tests.Commands;

public class KeybindingDispatcherTests
{
    private static readonly KeybindingContext Focused = new(TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true);
    private static readonly Keybinding CtrlK = new(KeyModifiers.Ctrl, KeyCode.KeyK);
    private static readonly Keybinding CtrlC = new(KeyModifiers.Ctrl, KeyCode.KeyC);
    private static readonly Keybinding F1 = new(KeyModifiers.None, KeyCode.F1);

    private static KeybindingDispatcher CreateDispatcher() =>
        new(new KeybindingResolver(
        [
            new KeybindingRule(CtrlK, "comment", null, 0, Chord: CtrlC),
            new KeybindingRule(F1, "help", null, 0),
        ]));

    [Fact]
    public void Dispatch_ChordThenUnknownKey_LeavesChordModeAndHandlesTheKey()
    {
        KeybindingDispatcher dispatcher = CreateDispatcher();

        Assert.True(dispatcher.Dispatch(CtrlK, Focused, out string? first));
        Assert.Null(first);
        Assert.True(dispatcher.InChordMode);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.None, KeyCode.Escape), Focused, out string? second));
        Assert.Null(second);
        Assert.False(dispatcher.InChordMode);

        // the next key works normally
        Assert.True(dispatcher.Dispatch(F1, Focused, out string? third));
        Assert.Equal("help", third);
    }

    [Fact]
    public void Dispatch_ChordThenSecondKey_ReturnsTheCommandAndLeavesChordMode()
    {
        KeybindingDispatcher dispatcher = CreateDispatcher();

        dispatcher.Dispatch(CtrlK, Focused, out _);
        Assert.True(dispatcher.Dispatch(CtrlC, Focused, out string? commandId));

        Assert.Equal("comment", commandId);
        Assert.False(dispatcher.InChordMode);
    }

    [Fact]
    public void Dispatch_SingleKeyRule_StillReturnsTheCommandOutsideChordMode()
    {
        KeybindingDispatcher dispatcher = CreateDispatcher();

        Assert.True(dispatcher.Dispatch(F1, Focused, out string? commandId));

        Assert.Equal("help", commandId);
        Assert.False(dispatcher.InChordMode);
    }

    [Fact]
    public void Dispatch_UnknownKeyOutsideChordMode_IsNotHandled()
    {
        KeybindingDispatcher dispatcher = CreateDispatcher();

        Assert.False(dispatcher.Dispatch(new Keybinding(KeyModifiers.None, KeyCode.Escape), Focused, out string? commandId));
        Assert.Null(commandId);
    }

    [Fact]
    public void LeaveChordMode_AfterTheFirstKey_ForgetsIt()
    {
        KeybindingDispatcher dispatcher = CreateDispatcher();
        dispatcher.Dispatch(CtrlK, Focused, out _);

        dispatcher.LeaveChordMode();

        Assert.False(dispatcher.InChordMode);
        Assert.False(dispatcher.Dispatch(CtrlC, Focused, out string? commandId));
        Assert.Null(commandId);
    }
}
