using LoomEditor.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Gutter;

/// <summary>The colors of the buttons the editor shows in its gutter (the comment "+", the diff block actions).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class GutterButtonTheme
{
    /// <summary><c>button.background</c> / <c>button.foreground</c>; without them the system accent button style.</summary>
    public static void Apply(Button button, EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(theme);
        if (theme.GetColor("button.background") is { } background && theme.GetColor("button.foreground") is { } foreground)
        {
            button.ClearValue(FrameworkElement.StyleProperty);
            button.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(background.A, background.R, background.G, background.B));
            button.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(foreground.A, foreground.R, foreground.G, foreground.B));
            return;
        }

        button.ClearValue(Control.BackgroundProperty);
        button.ClearValue(Control.ForegroundProperty);
        if (Application.Current?.Resources.TryGetValue("AccentButtonStyle", out object? style) == true && style is Style accent)
        {
            button.Style = accent;
        }
    }
}
