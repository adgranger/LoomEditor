using LoomEditor.Themes;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Hover;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Hover;
#endif

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
}
