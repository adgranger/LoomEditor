using LoomEditor.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

internal sealed partial class CommentThreadWidget
{
    private readonly SolidColorBrush _background = new();
    private readonly SolidColorBrush _borderBrush = new();
    private readonly SolidColorBrush _foreground = new();
    private readonly SolidColorBrush _dim = new();
    private readonly SolidColorBrush _link = new();
    private readonly SolidColorBrush _codeBackground = new();
    private readonly SolidColorBrush _quoteBar = new();
    private readonly SolidColorBrush _removedLine = new();
    private readonly SolidColorBrush _insertedLine = new();

    private static void SetOrClear(Control control, DependencyProperty property, ThemeColor? color)
    {
        if (color is { } c)
        {
            control.SetValue(property, new SolidColorBrush(Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B)));
        }
        else
        {
            control.ClearValue(property);
        }
    }

    /// <remarks>In Windows high contrast the colors arrive as <see langword="null"/>: WinUI's own high contrast resources apply.</remarks>
    private void SetComposerColors(ThemeColor? inputBackground, ThemeColor? inputBorder, ThemeColor? inputForeground, ThemeColor? buttonBackground, ThemeColor? buttonForeground)
    {
        SetOrClear(_textBox, Control.BackgroundProperty, inputBackground);
        SetOrClear(_textBox, Control.BorderBrushProperty, inputBorder);
        SetOrClear(_textBox, Control.ForegroundProperty, inputForeground);
        SetOrClear(_submitButton, Control.BackgroundProperty, buttonBackground);
        SetOrClear(_submitButton, Control.ForegroundProperty, buttonForeground);
    }

    private static void Set(SolidColorBrush brush, ThemeColor? color, double alpha = 1)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb((byte)Math.Round(c.A * alpha), c.R, c.G, c.B);
    }

    /// <summary>A transparent button with this text color; <see langword="null"/> clears both (the WinUI defaults apply).</summary>
    internal static void SetPlain(Control button, Brush? foreground)
    {
        if (foreground is null)
        {
            button.ClearValue(Control.BackgroundProperty);
            button.ClearValue(Control.ForegroundProperty);
        }
        else
        {
            button.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            button.Foreground = foreground;
        }
    }
}
