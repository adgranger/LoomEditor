using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomDiffEditor
{
    private void FocusModified() => _modified!.Focus(FocusState.Programmatic);
}
