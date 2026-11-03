using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Controls.Comments;

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

    private static void SetOrClear(TemplatedControl control, AvaloniaProperty<IBrush?> property, ThemeColor? color)
    {
        if (color is { } c)
        {
            control.SetValue(property, new SolidColorBrush(global::Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B)));
        }
        else
        {
            control.ClearValue(property);
        }
    }

    private void SetComposerColors(ThemeColor? inputBackground, ThemeColor? inputBorder, ThemeColor? inputForeground, ThemeColor? buttonBackground, ThemeColor? buttonForeground)
    {
        SetOrClear(_textBox, TemplatedControl.BackgroundProperty, inputBackground);
        SetOrClear(_textBox, TemplatedControl.BorderBrushProperty, inputBorder);
        SetOrClear(_textBox, TemplatedControl.ForegroundProperty, inputForeground);
        SetOrClear(_submitButton, TemplatedControl.BackgroundProperty, buttonBackground);
        SetOrClear(_submitButton, TemplatedControl.ForegroundProperty, buttonForeground);
    }

    private static void Set(SolidColorBrush brush, ThemeColor? color, double alpha = 1)
    {
        ThemeColor c = color ?? default;
        brush.Color = global::Avalonia.Media.Color.FromArgb((byte)Math.Round(c.A * alpha), c.R, c.G, c.B);
    }
}
