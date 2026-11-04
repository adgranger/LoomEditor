using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Find;

public sealed partial class FindWidget
{
    private static readonly string ChevronDown = ((char)0xE70D).ToString();
    private static readonly string ChevronRight = ((char)0xE76C).ToString();

    private void ShowReplaceRow(bool isVisible)
    {
        ReplaceRow.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        ToggleReplaceButton.Content = isVisible ? ChevronDown : ChevronRight;
    }

    private void SetMatchesCountStyle(string? toolTip, bool noResults)
    {
        ToolTipService.SetToolTip(MatchesCountText, toolTip);
        MatchesCountText.Foreground = (Brush)Resources[noResults ? "ErrorForegroundBrush" : "WidgetForegroundBrush"];
    }

    private void OnReplaceTextChanged(object sender, TextChangedEventArgs e) => ReplaceTextChanged();

    private void OnPreserveCaseClick(object sender, RoutedEventArgs e) => PreserveCaseToggled();

    private void OnReplaceClick(object sender, RoutedEventArgs e) => _controller?.Replace();

    private void OnReplaceAllClick(object sender, RoutedEventArgs e) => _controller?.ReplaceAll();

    private void OnToggleReplaceClick(object sender, RoutedEventArgs e) => ToggleReplace();

    private void OnFindInSelectionClick(object sender, RoutedEventArgs e) => FindInSelectionToggled();
}
