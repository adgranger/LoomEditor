using Avalonia.Media;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Controls.Hover;

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
        brush.Color = global::Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
