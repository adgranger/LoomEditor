using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomDiffEditor
{
    private void OnOverviewWheel(object? sender, PointerRoutedEventArgs e) => _modified?.ScrollFromWheel(e);
}
