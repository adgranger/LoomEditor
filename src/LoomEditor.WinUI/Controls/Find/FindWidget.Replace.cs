using LoomEditor.View.Features.Find;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls.Find;

// The replace row, the "n of m" label, find in selection and the enabled state of the buttons (Monaco: `findWidget.ts`
// `_updateButtons`, `_updateMatchesCount`, the replace input and the toggle replace button).
public sealed partial class FindWidget
{
    private static readonly string ChevronDown = ((char)0xE70D).ToString();
    private static readonly string ChevronRight = ((char)0xE76C).ToString();

    private bool _isReplaceVisible;

    // Monaco: `_updateButtons`.
    private void UpdateButtons()
    {
        if (_controller is not { } controller || _viewModel is not { } viewModel)
        {
            return;
        }

        FindReplaceState state = controller.State;
        bool visible = state.IsRevealed;
        bool nonEmpty = state.SearchString.Length > 0;
        bool hasMatches = state.MatchesCount != 0;

        FindTextBox.IsEnabled = visible;
        ReplaceTextBox.IsEnabled = visible && _isReplaceVisible;
        UpdateFindInSelectionToggle();
        CloseButton.IsEnabled = visible;
        PreviousButton.IsEnabled = visible && nonEmpty && hasMatches && state.CanNavigateBack();
        NextButton.IsEnabled = visible && nonEmpty && hasMatches && state.CanNavigateForward();
        ReplaceButton.IsEnabled = visible && _isReplaceVisible && nonEmpty;
        ReplaceAllButton.IsEnabled = visible && _isReplaceVisible && nonEmpty;

        ReplaceRow.Visibility = _isReplaceVisible ? Visibility.Visible : Visibility.Collapsed;
        ToggleReplaceButton.Content = _isReplaceVisible ? ChevronDown : ChevronRight;
        ToggleReplaceButton.IsEnabled = visible && !viewModel.CursorConfig.ReadOnly;
    }

    // Monaco: `_updateToggleSelectionFindButton`.
    internal void UpdateFindInSelectionToggle()
    {
        if (_controller is not { } controller || _viewModel is not { } viewModel)
        {
            return;
        }

        bool isSelection = !viewModel.GetSelection().IsEmpty;
        FindInSelectionToggle.IsEnabled = controller.State.IsRevealed && (FindInSelectionToggle.IsChecked == true || isSelection);
    }

    // Monaco: the editor's read-only change (findWidget.ts:198): the replace part goes away.
    internal void OnReadOnlyChanged()
    {
        if (_controller is { } controller && _viewModel is { } viewModel && viewModel.CursorConfig.ReadOnly)
        {
            controller.State.Change(new NewFindReplaceState { IsReplaceRevealed = false }, false);
        }

        UpdateButtons();
    }

    // Monaco: `_updateMatchesCount` and the `no-results` class (without the screen reader alert).
    private void UpdateMatchesCount(FindReplaceState state)
    {
        MatchesCountText.Text = FindMatchesLabel.Format(state.MatchesPosition, state.MatchesCount);
        ToolTipService.SetToolTip(MatchesCountText, state.MatchesCount >= FindConstants.MatchesLimit ? FindMatchesLabel.LimitTitle : null);
        bool noResults = state.SearchString.Length > 0 && state.MatchesCount == 0;
        MatchesCountText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Resources[noResults ? "ErrorForegroundBrush" : "WidgetForegroundBrush"];
    }

    // Monaco: `_onStateChanged` for `replaceString`.
    private void SetReplaceText(string replaceString)
    {
        string text = replaceString.Replace('\n', '\r');
        if (ReplaceTextBox.Text == text)
        {
            return;
        }

        _ignoreChangeEvent = true;
        try
        {
            ReplaceTextBox.Text = text;
        }
        finally
        {
            _ignoreChangeEvent = false;
        }
    }

    // Monaco: `_onStateChanged` for `isReplaceRevealed`.
    private void UpdateReplaceRevealed(FindReplaceState state)
    {
        if (state.IsReplaceRevealed)
        {
            if (_viewModel is { } viewModel && !viewModel.CursorConfig.ReadOnly && !_isReplaceVisible)
            {
                _isReplaceVisible = true;
                UpdateButtons();
            }
        }
        else if (_isReplaceVisible)
        {
            _isReplaceVisible = false;
            UpdateButtons();
        }
    }

    // Monaco: the replace input's `onDidChange` (findWidget.ts:1176).
    private void OnReplaceTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ignoreChangeEvent || _controller is not { } controller)
        {
            return;
        }

        string replaceString = ReplaceTextBox.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        controller.State.Change(new NewFindReplaceState { ReplaceString = replaceString }, false);
    }

    // Monaco: the replace input's `onDidOptionChange` (findWidget.ts:1182).
    private void OnPreserveCaseClick(object sender, RoutedEventArgs e) =>
        _controller?.State.Change(new NewFindReplaceState { PreserveCase = PreserveCaseToggle.IsChecked == true }, true);

    private void OnReplaceClick(object sender, RoutedEventArgs e) => _controller?.Replace();

    private void OnReplaceAllClick(object sender, RoutedEventArgs e) => _controller?.ReplaceAll();

    // Monaco: the toggle replace button (findWidget.ts:1246).
    private void OnToggleReplaceClick(object sender, RoutedEventArgs e) =>
        _controller?.State.Change(new NewFindReplaceState { IsReplaceRevealed = !_isReplaceVisible }, false);

    // Monaco: the find in selection toggle's `onChange` (findWidget.ts:1109).
    private void OnFindInSelectionClick(object sender, RoutedEventArgs e)
    {
        if (_controller is not { } controller || _viewModel is not { } viewModel)
        {
            return;
        }

        if (FindInSelectionToggle.IsChecked != true)
        {
            controller.State.Change(new NewFindReplaceState { SearchScope = null }, true);
            return;
        }

        var ranges = new List<Range>();
        foreach (Core.Text.Selection selection in viewModel.GetSelections())
        {
            Range range = selection.Range;
            if (range.EndColumn == 1 && range.EndLineNumber > range.StartLineNumber)
            {
                range = new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber - 1, viewModel.Model.GetLineMaxColumn(range.EndLineNumber - 1));
            }

            if (!range.IsEmpty)
            {
                ranges.Add(range);
            }
        }

        if (ranges.Count > 0)
        {
            controller.State.Change(new NewFindReplaceState { SearchScope = ranges }, true);
        }
    }
}
