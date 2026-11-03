using LoomEditor.View.Features.Find;
using LoomEditor.View.Layout;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls.Find;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Find;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// The state handling of the widget: connect, refresh, the find row. The controls and the framework calls are in FindWidget.Host.cs.
public sealed partial class FindWidget
{
    private FindController? _controller;
    private Action? _focusEditor;
    private ViewModel? _viewModel;
    private bool _ignoreChangeEvent;

    // Monaco: `_onStateChanged` and `_buildDomNode` subscribe the widget to the state.
    internal void Connect(FindController controller, ViewModel viewModel, Action focusEditor)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(focusEditor);

        Disconnect();
        _controller = controller;
        _viewModel = viewModel;
        _focusEditor = focusEditor;
        controller.Widget = this;
        controller.State.Change(new NewFindReplaceState { Loop = controller.Options.Loop }, false);
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
        _viewModel = null;
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
            UpdateButtons();
        }

        if (e.ReplaceString)
        {
            SetReplaceText(state.ReplaceString);
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

        if (e.IsReplaceRevealed)
        {
            UpdateReplaceRevealed(state);
        }

        if (e.PreserveCase)
        {
            PreserveCaseToggle.IsChecked = state.PreserveCase;
        }

        if (e.SearchScope)
        {
            FindInSelectionToggle.IsChecked = state.SearchScope is not null;
            UpdateFindInSelectionToggle();
        }

        if (e.SearchString || e.MatchesCount || e.MatchesPosition)
        {
            UpdateMatchesCount(state);
            UpdateButtons();
        }

        if (e.Loop || e.IsRevealed)
        {
            UpdateButtons();
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
        PreserveCaseToggle.IsChecked = state.PreserveCase;
        FindInSelectionToggle.IsChecked = state.SearchScope is not null;
        SetReplaceText(state.ReplaceString);
        _isReplaceVisible = false;
        UpdateReplaceRevealed(state);
        UpdateMatchesCount(state);
        SetVisible(state.IsRevealed);
        UpdateButtons();
    }

    // Monaco: `_reveal` and `_hide(true)`, without animation or view zone.
    private void Reveal(bool revealed)
    {
        SetVisible(revealed);
        if (!revealed)
        {
            _focusEditor?.Invoke();
        }
    }

    // The text boxes may use "\r\n" or "\r" for a line break: the state always holds "\n".
    private static string ToStateText(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private void SetFindText(string searchString)
    {
        if (ToStateText(FindTextBox.Text) == searchString)
        {
            return;
        }

        _ignoreChangeEvent = true;
        try
        {
            FindTextBox.Text = ToBoxText(searchString);
        }
        finally
        {
            _ignoreChangeEvent = false;
        }
    }

    // Monaco: the find input's `onInput` (findWidget.ts:1020).
    private void FindTextChanged()
    {
        if (_ignoreChangeEvent || _controller is not { } controller || !controller.Options.FindOnType)
        {
            return;
        }

        controller.State.Change(new NewFindReplaceState { SearchString = ToStateText(FindTextBox.Text) }, true);
    }

    // Monaco: `onDidOptionChange` (findWidget.ts:1033).
    private void OptionToggled() =>
        _controller?.State.Change(
            new NewFindReplaceState
            {
                IsRegex = RegexToggle.IsChecked == true,
                WholeWord = WholeWordToggle.IsChecked == true,
                MatchCase = MatchCaseToggle.IsChecked == true,
            },
            true);

    private void PreviousMatch()
    {
        if (_controller is { } controller)
        {
            FindActions.PreviousMatch(controller);
        }
    }

    private void NextMatch()
    {
        if (_controller is { } controller)
        {
            FindActions.NextMatch(controller);
        }
    }

    // Monaco: the close button (findWidget.ts:1143).
    private void CloseWidget() =>
        _controller?.State.Change(new NewFindReplaceState { IsRevealed = false, SearchScope = null }, false);
}
