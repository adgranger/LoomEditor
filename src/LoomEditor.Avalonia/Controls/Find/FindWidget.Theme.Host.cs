using Avalonia.Media;

namespace LoomEditor.Avalonia.Controls.Find;

public sealed partial class FindWidget
{
    private void SetBrushColor(string brushKey, byte a, byte r, byte g, byte b) =>
        ((SolidColorBrush)Resources[brushKey]!).Color = global::Avalonia.Media.Color.FromArgb(a, r, g, b);
}
