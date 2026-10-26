using LoomEditor.Themes;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Hover;

// Monaco: `editorHoverWidget.*` and `textLink.foreground`.
internal sealed partial class HoverWidget
{
    internal void ApplyTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        Set(_background, theme.GetColor("editorHoverWidget.background"));
        Set(_foreground, theme.GetColor("editorHoverWidget.foreground"));
        Set(_border, theme.GetColor("editorHoverWidget.border"));
        Set(_link, theme.GetColor("textLink.foreground"));
    }

    private static void Set(SolidColorBrush brush, ThemeColor? color)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
