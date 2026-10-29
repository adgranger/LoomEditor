using LoomEditor.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

// Colors of the widget, from the editor theme. Missing ids fall back as the hover does (to its own colors or to the text color, dimmed).
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

    /// <summary>Takes the colors from the theme (Monaco: <c>editorWidget.*</c>, <c>descriptionForeground</c>, <c>textLink.foreground</c>, ...).</summary>
    /// <param name="theme">The editor's effective theme: in Windows high contrast, the system high contrast theme.</param>
    /// <param name="highContrast">Windows high contrast is on: the text box and buttons get no brush from code, WinUI's own high contrast resources apply.</param>
    public void ApplyTheme(EditorTheme theme, bool highContrast = false)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ThemeColor? foreground = theme.GetColor("editor.foreground") ?? theme.GetColor("foreground");
        ThemeColor? dim = theme.GetColor("descriptionForeground");
        Set(_background, theme.GetColor("editorWidget.background") ?? theme.GetColor("editorHoverWidget.background"));
        Set(_borderBrush, theme.GetColor("editorWidget.border") ?? theme.GetColor("editorHoverWidget.border"));
        Set(_foreground, foreground);
        // descriptionForeground is not in the built-in defaults: the text color at 70 % is the closest.
        Set(_dim, dim ?? foreground, dim is null ? 0.7 : 1);
        Set(_link, theme.GetColor("textLink.foreground"));
        Set(_codeBackground, theme.GetColor("textCodeBlock.background"));
        ThemeColor? quote = theme.GetColor("textBlockQuote.border");
        Set(_quoteBar, quote ?? dim ?? foreground, quote is null ? 0.5 : 1);
        // Soft red / green when the theme has no diff colors; none in high contrast (the -/+ markers carry the meaning).
        ThemeColor? none = highContrast ? default(ThemeColor) : null;
        Set(_removedLine, theme.GetColor("diffEditor.removedLineBackground") ?? none ?? new ThemeColor(0x33, 0xFF, 0x00, 0x00));
        Set(_insertedLine, theme.GetColor("diffEditor.insertedLineBackground") ?? none ?? new ThemeColor(0x33, 0x00, 0xAA, 0x00));

        // The composer: a missing id keeps the WinUI default (the brush is cleared, not set); in high contrast nothing is set by code.
        SetOrClear(_textBox, Control.BackgroundProperty, highContrast ? null : theme.GetColor("input.background"));
        SetOrClear(_textBox, Control.BorderBrushProperty, highContrast ? null : theme.GetColor("input.border"));
        SetOrClear(_textBox, Control.ForegroundProperty, highContrast ? null : theme.GetColor("input.foreground"));
        SetOrClear(_submitButton, Control.BackgroundProperty, highContrast ? null : theme.GetColor("button.background"));
        SetOrClear(_submitButton, Control.ForegroundProperty, highContrast ? null : theme.GetColor("button.foreground"));
        foreach (Control button in (Control[])[_previewButton, _cancelButton, _replyButton])
        {
            SetPlain(button, highContrast ? null : _foreground);
        }

        _toolbar.SetForeground(highContrast ? null : _foreground);
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

    private static void Set(SolidColorBrush brush, ThemeColor? color, double alpha = 1)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb((byte)Math.Round(c.A * alpha), c.R, c.G, c.B);
    }
}
