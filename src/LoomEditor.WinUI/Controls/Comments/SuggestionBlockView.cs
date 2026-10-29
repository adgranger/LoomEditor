using LoomEditor.View.Comments;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

/// <summary>The brushes and code font of a <see cref="SuggestionBlockView"/>.</summary>
/// <remarks>Threading: UI thread only (XAML brushes).</remarks>
internal sealed record SuggestionBlockTheme(Brush Foreground, Brush Border, Brush RemovedLine, Brush InsertedLine, FontFamily CodeFont, double FontSize);

/// <summary>
/// A ```suggestion block drawn as a diff of the commented line: a "Suggested change" header with an optional Apply button, then one row per
/// <see cref="SuggestionDiffRow"/> with its <c>-</c> / <c>+</c> marker (so color is never the only signal). Long lines scroll, never wrap.
/// </summary>
/// <remarks>A <see cref="UserControl"/>: WinUI's <c>Border</c> is sealed. Threading: UI thread only.</remarks>
internal sealed partial class SuggestionBlockView : UserControl
{
    private const double MarkerWidth = 20;

    /// <param name="diff">The rows to draw.</param>
    /// <param name="canApply">False disables the Apply button (read-only editor).</param>
    /// <param name="apply">The Apply action; <see langword="null"/> draws no button (Preview).</param>
    /// <param name="theme">Brushes and code font.</param>
    /// <param name="applyToolTip">The Apply button's tooltip (the reason of the last failure, or why it is disabled).</param>
    public SuggestionBlockView(SuggestionDiffResult diff, bool canApply, Action? apply, SuggestionBlockTheme theme, string? applyToolTip = null)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ArgumentNullException.ThrowIfNull(theme);

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock
        {
            Text = diff.IsNoOp ? CommentStrings.NoChanges : CommentStrings.SuggestedChange,
            FontWeight = FontWeights.SemiBold,
            Foreground = theme.Foreground,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (apply is not null && !diff.IsNoOp)
        {
            var button = new Button { Content = CommentStrings.ApplySuggestion, IsEnabled = canApply };
            button.Click += (_, _) => apply();
            AutomationProperties.SetFullDescription(button, diff.Describe());
            if (applyToolTip is not null)
            {
                ToolTipService.SetToolTip(button, applyToolTip);
            }

            Grid.SetColumn(button, 1);
            header.Children.Add(button);
        }

        var rows = new StackPanel();
        foreach (SuggestionDiffRow row in diff.Rows)
        {
            rows.Children.Add(Row(row, theme));
        }

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Auto,
            VerticalScrollMode = ScrollMode.Disabled,
            Content = rows,
        };
        // The row backgrounds span the block even when every line is shorter than it.
        scroll.SizeChanged += (_, e) => rows.MinWidth = e.NewSize.Width;

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(header);
        panel.Children.Add(scroll);
        Content = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = theme.Border,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 4, 0, 4),
            Child = panel,
        };
        AutomationProperties.SetName(this, CommentStrings.SuggestedChange);
        AutomationProperties.SetFullDescription(this, diff.Describe());
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CommentThreadAutomationPeer(this);

    /// <summary>The text of the lines the diff was computed against (sent back to the controller on Apply to detect a stale block).</summary>
    public string RenderedRangeText { get; init; } = "";

    private static Grid Row(SuggestionDiffRow row, SuggestionBlockTheme theme)
    {
        var grid = new Grid
        {
            Background = row.Kind switch
            {
                SuggestionDiffRowKind.Removed => theme.RemovedLine,
                SuggestionDiffRowKind.Added => theme.InsertedLine,
                _ => null,
            },
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(MarkerWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        // The marker is never empty (a space for context), so an empty line is still one line high.
        grid.Children.Add(Code(row.Marker.ToString(), theme));
        TextBlock text = Code(row.Text, theme);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    private static TextBlock Code(string text, SuggestionBlockTheme theme) => new()
    {
        Text = text,
        FontFamily = theme.CodeFont,
        FontSize = theme.FontSize,
        Foreground = theme.Foreground,
        TextWrapping = TextWrapping.NoWrap,
    };
}
