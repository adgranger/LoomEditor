using LoomEditor.View.Features.Find;
using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Find;

/// <summary>
/// The find widget of the editor: a find box with the match case, whole word and regex toggles, and the previous, next and
/// close buttons (Monaco: <c>FindWidget</c>, behavior only, not its DOM).
/// </summary>
/// <remarks>
/// Public only because the XAML compiler needs it; every member except the constructor is internal. The editor control
/// connects it to its find controller; without a controller it does nothing.
/// Threading: UI thread only.
/// </remarks>
public sealed partial class FindWidget : UserControl, IFindWidget
{
    private FindController? _controller;
    private Action? _focusEditor;
    private bool _ignoreChangeEvent;

    /// <summary>Creates the widget.</summary>
    public FindWidget()
    {
        InitializeComponent();
        InitializeKeyboard();
    }

    // Monaco: `_onStateChanged` and `_buildDomNode` subscribe the widget to the state.
    internal void Connect(FindController controller, ViewModel viewModel, Action focusEditor)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(focusEditor);

        Disconnect();
        _controller = controller;
        _focusEditor = focusEditor;
        controller.Widget = this;
        controller.State.Changed += OnStateChanged;
        Refresh();
    }

    internal void Disconnect()
    {
        if (_controller is null)
        {
            return;
        }

        _controller.State.Changed -= OnStateChanged;
        _controller.Widget = null;
        _controller = null;
        _focusEditor = null;
        TryHandleKey = null;
    }

    void IFindWidget.FocusEditor() => _focusEditor?.Invoke();

    void IFindWidget.HighlightFindOptions(bool findWidgetRevealed)
    {
        // Monaco flashes the toggles; not ported in phase 5.
    }

    // Monaco: `_onStateChanged` (searchString, isRevealed, isRegex, wholeWord, matchCase).
    private void OnStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
    {
        if (_controller is not { } controller)
        {
            return;
        }

        FindReplaceState state = controller.State;
        if (e.SearchString)
        {
            SetFindText(state.SearchString);
        }

        if (e.IsRegex)
        {
            RegexToggle.IsChecked = state.IsRegex;
        }

        if (e.WholeWord)
        {
            WholeWordToggle.IsChecked = state.WholeWord;
        }

        if (e.MatchCase)
        {
            MatchCaseToggle.IsChecked = state.MatchCase;
        }

        if (e.IsRevealed)
        {
            Reveal(state.IsRevealed);
        }
    }

    // The widget may be connected while the state already holds values (e.g. after the control was reloaded).
    private void Refresh()
    {
        FindReplaceState state = _controller!.State;
        SetFindText(state.SearchString);
        RegexToggle.IsChecked = state.IsRegex;
        WholeWordToggle.IsChecked = state.WholeWord;
        MatchCaseToggle.IsChecked = state.MatchCase;
        Visibility = state.IsRevealed ? Visibility.Visible : Visibility.Collapsed;
    }

    // Monaco: `_reveal` and `_hide(true)`, without animation or view zone.
    private void Reveal(bool revealed)
    {
        Visibility = revealed ? Visibility.Visible : Visibility.Collapsed;
        if (!revealed)
        {
            _focusEditor?.Invoke();
        }
    }

    // WinUI's TextBox stores line breaks as `\r`.
    private void SetFindText(string searchString)
    {
        string text = searchString.Replace('\n', '\r');
        if (FindTextBox.Text == text)
        {
            return;
        }

        _ignoreChangeEvent = true;
        try
        {
            FindTextBox.Text = text;
        }
        finally
        {
            _ignoreChangeEvent = false;
        }
    }

    // Monaco: the find input's `onInput` (findWidget.ts:1020).
    private void OnFindTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ignoreChangeEvent || _controller is not { } controller || !controller.Options.FindOnType)
        {
            return;
        }

        string searchString = FindTextBox.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        controller.State.Change(new NewFindReplaceState { SearchString = searchString }, true);
    }

    // Monaco: `onDidOptionChange` (findWidget.ts:1033).
    private void OnOptionToggleClick(object sender, RoutedEventArgs e) =>
        _controller?.State.Change(
            new NewFindReplaceState
            {
                IsRegex = RegexToggle.IsChecked == true,
                WholeWord = WholeWordToggle.IsChecked == true,
                MatchCase = MatchCaseToggle.IsChecked == true,
            },
            true);

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        if (_controller is { } controller)
        {
            FindActions.PreviousMatch(controller);
        }
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_controller is { } controller)
        {
            FindActions.NextMatch(controller);
        }
    }

    // Monaco: the close button (findWidget.ts:1143).
    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        _controller?.State.Change(new NewFindReplaceState { IsRevealed = false, SearchScope = null }, false);
}
