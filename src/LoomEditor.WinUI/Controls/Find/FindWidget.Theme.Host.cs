using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Find;

public sealed partial class FindWidget
{
    private void SetBrushColor(string brushKey, byte a, byte r, byte g, byte b) =>
        ((SolidColorBrush)Resources[brushKey]).Color = Windows.UI.Color.FromArgb(a, r, g, b);
}
