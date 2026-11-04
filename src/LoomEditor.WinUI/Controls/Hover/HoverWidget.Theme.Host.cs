using LoomEditor.Themes;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Hover;

internal sealed partial class HoverWidget
{
    private static void Set(SolidColorBrush brush, ThemeColor? color)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
