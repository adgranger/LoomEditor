using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Controls.Gutter;

/// <summary>The colors of the buttons the editor shows in its gutter (the comment "+", the diff block actions, the diff line checkboxes).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class GutterButtonTheme
{
    /// <summary>
    /// <c>button.background</c> / <c>button.foreground</c>; without them the accent button style of the Fluent theme for a <see cref="Button"/>
    /// (the <c>accent</c> class), the default look for any other button.
    /// </summary>
    public static void Apply(Button button, EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(theme);
        if (theme.GetColor("button.background") is { } background && theme.GetColor("button.foreground") is { } foreground)
        {
            button.Classes.Remove("accent");
            button.Background = new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(background.A, background.R, background.G, background.B));
            button.Foreground = new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(foreground.A, foreground.R, foreground.G, foreground.B));
            return;
        }

        button.ClearValue(TemplatedControl.BackgroundProperty);
        button.ClearValue(TemplatedControl.ForegroundProperty);
        if (!button.Classes.Contains("accent"))
        {
            button.Classes.Add("accent");
        }
    }
}
