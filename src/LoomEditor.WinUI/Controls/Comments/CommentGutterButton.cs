using LoomEditor.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>
/// The 22 × 22 "+" in the gutter that asks for a comment thread on the hovered line. A pointer affordance: not a tab stop (keyboard users run
/// <c>loom.commentThreads.add</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class CommentGutterButton : Button
{
    /// <summary>The width and height in DIPs.</summary>
    public const double Size = 22;

    public CommentGutterButton()
    {
        Content = "+";
        Width = Size;
        Height = Size;
        Padding = new Thickness(0);
        CornerRadius = new CornerRadius(4);
        IsTabStop = false;
        Visibility = Visibility.Collapsed;
    }

    /// <summary>The model line the button is shown on; 0 when hidden.</summary>
    public int LineNumber { get; private set; }

    /// <summary>Sets the line and the automation name <c>Add a comment on line {N}</c>.</summary>
    public void SetLine(int lineNumber)
    {
        if (LineNumber != lineNumber)
        {
            LineNumber = lineNumber;
            AutomationProperties.SetName(this, View.Comments.CommentStrings.AddCommentOnLine(lineNumber));
        }
    }

    /// <summary><c>button.background</c> / <c>button.foreground</c>; without them the system accent button style.</summary>
    public void ApplyTheme(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        if (theme.GetColor("button.background") is { } background && theme.GetColor("button.foreground") is { } foreground)
        {
            ClearValue(StyleProperty);
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(background.A, background.R, background.G, background.B));
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(foreground.A, foreground.R, foreground.G, foreground.B));
            return;
        }

        ClearValue(BackgroundProperty);
        ClearValue(ForegroundProperty);
        if (Application.Current?.Resources.TryGetValue("AccentButtonStyle", out object? style) == true && style is Style accent)
        {
            Style = accent;
        }
    }
}
