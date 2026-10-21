using LoomEditor.Themes;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Find;

// The colors of the widget come from the editor theme (Monaco: the widget colors of `findWidget.ts` `registerThemingParticipant`).
public sealed partial class FindWidget
{
    // Monaco: `editorWidget.*`, `input.*` and `inputOption.*` colors.
    internal void ApplyTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ThemeColor? foreground = theme.GetColor("editorWidget.foreground");
        Set("WidgetBackgroundBrush", theme.GetColor("editorWidget.background"));
        Set("WidgetForegroundBrush", foreground);
        Set("WidgetBorderBrush", theme.GetColor("editorWidget.border"));
        Set("InputBackgroundBrush", theme.GetColor("input.background"));
        Set("InputForegroundBrush", theme.GetColor("input.foreground"));
        Set("InputBorderBrush", theme.GetColor("input.border"));
        Set("FocusBorderBrush", theme.GetColor("focusBorder"));
        Set("OptionActiveBackgroundBrush", theme.GetColor("inputOption.activeBackground"));
        Set("OptionActiveBorderBrush", theme.GetColor("inputOption.activeBorder"));
        Set("OptionActiveForegroundBrush", theme.GetColor("inputOption.activeForeground"));
        Set("ErrorForegroundBrush", theme.GetColor("errorForeground"));
        Set("HoverBackgroundBrush", foreground is { } f ? f with { A = 0x1F } : null);
    }

    private void Set(string brushKey, ThemeColor? color)
    {
        ThemeColor c = color ?? default;
        ((SolidColorBrush)Resources[brushKey]).Color = Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
