using LoomEditor.Themes;
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

    /// <summary>Takes the colors from the theme (Monaco: <c>editorWidget.*</c>, <c>descriptionForeground</c>, <c>textLink.foreground</c>, ...).</summary>
    public void ApplyTheme(EditorTheme theme)
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
    }

    private static void Set(SolidColorBrush brush, ThemeColor? color, double alpha = 1)
    {
        ThemeColor c = color ?? default;
        brush.Color = Windows.UI.Color.FromArgb((byte)Math.Round(c.A * alpha), c.R, c.G, c.B);
    }
}
