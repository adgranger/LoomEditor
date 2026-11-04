using Avalonia.Input;

namespace LoomEditor.Avalonia.Controls;

public sealed partial class LoomDiffEditor
{
    private void OnOverviewWheel(object? sender, PointerWheelEventArgs e) => _modified?.ScrollFromWheel(e);
}
