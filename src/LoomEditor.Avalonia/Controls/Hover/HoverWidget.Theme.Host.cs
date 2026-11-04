using Avalonia.Media;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Controls.Hover;

internal sealed partial class HoverWidget
{
    private static void Set(SolidColorBrush brush, ThemeColor? color)
    {
        ThemeColor c = color ?? default;
        brush.Color = global::Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
