using LoomEditor.Themes;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls.Suggest;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Suggest;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// Monaco: `editorSuggestWidget.*`.
internal sealed partial class SuggestWidget
{
    internal void ApplyTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        Set(_background, theme.GetColor("editorSuggestWidget.background"));
        Set(_border, theme.GetColor("editorSuggestWidget.border"));
        ThemeColor fg = theme.GetColor("editorSuggestWidget.foreground") ?? default;
        Set(_foreground, fg);
        // The label details and the description are dimmed (Monaco: opacity .7 on the details and qualifier labels).
        Set(_dim, new ThemeColor(fg.R, fg.G, fg.B, (byte)(fg.A * 7 / 10)));
        Set(_selectedBackground, theme.GetColor("editorSuggestWidget.selectedBackground"));
        Set(_selectedForeground, theme.GetColor("editorSuggestWidget.selectedForeground"));
        Set(_highlight, theme.GetColor("editorSuggestWidget.highlightForeground"));
        Set(_focusHighlight, theme.GetColor("editorSuggestWidget.focusHighlightForeground"));
    }
}
