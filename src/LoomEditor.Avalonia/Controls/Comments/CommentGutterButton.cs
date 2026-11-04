using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using LoomEditor.Themes;
using LoomEditor.View.Comments;

namespace LoomEditor.Avalonia.Controls.Comments;

/// <summary>
/// The 22 x 22 "+" in the gutter that asks for a comment thread on the hovered line. A pointer affordance: not a tab stop (keyboard users run
/// <c>loom.commentThreads.add</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CommentGutterButton : Button
{
    /// <summary>The width and height in DIPs.</summary>
    public const double Size = 22;

    private CommentRange _target;

    public CommentGutterButton()
    {
        var icon = new PathIcon { Width = 10, Height = 10 };
        icon.Bind(PathIcon.DataProperty, icon.GetResourceObservable("LoomIconPlus"));
        Content = icon;
        Width = Size;
        Height = Size;
        Padding = new Thickness(0);
        CornerRadius = new CornerRadius(4);
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
        Focusable = false;
        IsVisible = false;
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Button);

    /// <summary>The model line the button is shown on; 0 when hidden.</summary>
    public int LineNumber { get; private set; }

    /// <summary>The first line the click requests a thread on; equals <see cref="LineNumber"/> for one line.</summary>
    public int StartLineNumber { get; private set; }

    /// <summary>
    /// Sets the requested range and the automation name <c>Add a comment on line {N}</c> / <c>on lines {S} to {E}</c>, or
    /// <c>on the selected text</c> for an exact-text range.
    /// </summary>
    public void SetTarget(CommentRange range)
    {
        if (_target != range)
        {
            _target = range;
            StartLineNumber = range.StartLineNumber;
            LineNumber = range.LineNumber;
            AutomationProperties.SetName(this, range.IsWholeLines
                ? CommentStrings.AddCommentOnLines(range.StartLineNumber, range.LineNumber)
                : CommentStrings.AddCommentOnSelectedText(range.StartLineNumber, range.LineNumber));
        }
    }

    /// <summary><c>button.background</c> / <c>button.foreground</c>; without them the accent button style.</summary>
    public void ApplyTheme(EditorTheme theme) => Gutter.GutterButtonTheme.Apply(this, theme);
}
