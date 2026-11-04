using Avalonia.Input;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Commands;
using AvaloniaKeyModifiers = global::Avalonia.Input.KeyModifiers;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Tests.Input;

// The twin of the WinUI key mapping: no upstream test, all tests are new.
public class KeyMappingTests
{
    [Fact]
    public void ToModifiers_Windows_CtrlIsCtrl()
    {
        Assert.Equal(ViewKeyModifiers.Ctrl, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Control, isMacOS: false));
        Assert.Equal(ViewKeyModifiers.Meta, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Meta, isMacOS: false));
        Assert.Equal(ViewKeyModifiers.Shift | ViewKeyModifiers.Alt, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Shift | AvaloniaKeyModifiers.Alt, isMacOS: false));
    }

    [Fact]
    public void ToModifiers_MacOS_CmdIsCtrl()
    {
        Assert.Equal(ViewKeyModifiers.Ctrl, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Meta, isMacOS: true));
        Assert.Equal(ViewKeyModifiers.Ctrl | ViewKeyModifiers.Shift, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Meta | AvaloniaKeyModifiers.Shift, isMacOS: true));
    }

    [Fact]
    public void ToModifiers_MacOS_ControlIsMeta()
    {
        Assert.Equal(ViewKeyModifiers.Meta, KeyMapping.ToModifiers(AvaloniaKeyModifiers.Control, isMacOS: true));
    }

    [Fact]
    public void ToKeyCode_Keys_MapLikeWinUI()
    {
        Assert.Equal(KeyCode.KeyA, KeyMapping.ToKeyCode(Key.A));
        Assert.Equal(KeyCode.KeyZ, KeyMapping.ToKeyCode(Key.Z));
        Assert.Equal(KeyCode.F12, KeyMapping.ToKeyCode(Key.F12));
        Assert.Equal(KeyCode.Digit9, KeyMapping.ToKeyCode(Key.D9));
        Assert.Equal(KeyCode.Backslash, KeyMapping.ToKeyCode(Key.OemPipe));
        Assert.Equal(KeyCode.LeftArrow, KeyMapping.ToKeyCode(Key.Left));
        Assert.Null(KeyMapping.ToKeyCode(Key.LeftCtrl));
    }
}
