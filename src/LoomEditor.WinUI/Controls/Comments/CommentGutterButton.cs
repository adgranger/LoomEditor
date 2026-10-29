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

    private View.Comments.CommentRange _target;

    /// <summary>The model line the button is shown on; 0 when hidden.</summary>
    public int LineNumber { get; private set; }

    /// <summary>The first line the click requests a thread on; equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; private set; }

    /// <summary>
    /// Sets the requested range and the automation name <c>Add a comment on line {N}</c> / <c>on lines {S} to {E}</c>, or
    /// <c>on the selected text</c> for an exact-text range.
    /// </summary>
    public void SetTarget(View.Comments.CommentRange range)
    {
        if (_target != range)
        {
            _target = range;
            StartLineNumber = range.StartLineNumber;
            LineNumber = range.LineNumber;
            AutomationProperties.SetName(this, range.IsWholeLines
                ? View.Comments.CommentStrings.AddCommentOnLines(range.StartLineNumber, range.LineNumber)
                : View.Comments.CommentStrings.AddCommentOnSelectedText(range.StartLineNumber, range.LineNumber));
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
