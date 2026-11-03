using LoomEditor.WinUI.Input;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    private static WindowsClipboardService CreateClipboardService() => new();
}
