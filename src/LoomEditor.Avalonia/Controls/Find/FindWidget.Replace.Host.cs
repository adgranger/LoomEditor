using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace LoomEditor.Avalonia.Controls.Find;

public sealed partial class FindWidget
{
    private void ShowReplaceRow(bool isVisible)
    {
        ReplaceRow.IsVisible = isVisible;
        ToggleReplaceIcon.Data = this.TryFindResource(isVisible ? "LoomIconChevronDown" : "LoomIconChevronRight", ActualThemeVariant, out object? icon)
            ? icon as Geometry
            : null;
    }

    private void SetMatchesCountStyle(string? toolTip, bool noResults)
    {
        ToolTip.SetTip(MatchesCountText, toolTip);
        MatchesCountText.Foreground = GetBrush(noResults ? "ErrorForegroundBrush" : "WidgetForegroundBrush");
    }

    private void OnReplaceTextChanged(object? sender, TextChangedEventArgs e) => ReplaceTextChanged();

    private void OnPreserveCaseClick(object? sender, RoutedEventArgs e) => PreserveCaseToggled();

    private void OnReplaceClick(object? sender, RoutedEventArgs e) => _controller?.Replace();

    private void OnReplaceAllClick(object? sender, RoutedEventArgs e) => _controller?.ReplaceAll();

    private void OnToggleReplaceClick(object? sender, RoutedEventArgs e) => ToggleReplace();

    private void OnFindInSelectionClick(object? sender, RoutedEventArgs e) => FindInSelectionToggled();
}
