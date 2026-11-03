using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;
using ViewKeyModifiers = LoomEditor.View.Commands.KeyModifiers;

namespace LoomEditor.Avalonia.Controls.Find;

// Keyboard and focus (Monaco: `findWidget.ts` focus trackers, `focusFindInput`, `_onFindInputKeyDown`): the keys that belong to
// the find commands go through the editor's keybindings, the others stay with the text boxes.
public sealed partial class FindWidget
{
    /// <summary>The commands the widget runs for a key press; undo, redo, select all and the clipboard stay with the text boxes.</summary>
    internal static readonly HashSet<string> RoutedCommandIds = new(StringComparer.Ordinal)
    {
        FindCommands.StartFindAction,
        FindCommands.StartFindWithSelection,
        FindCommands.NextMatchFindAction,
        FindCommands.PreviousMatchFindAction,
        FindCommands.NextSelectionMatchFindAction,
        FindCommands.PreviousSelectionMatchFindAction,
        FindCommands.StartFindReplaceAction,
        FindCommands.CloseFindWidgetCommand,
        FindCommands.ToggleCaseSensitiveCommand,
        FindCommands.ToggleWholeWordCommand,
        FindCommands.ToggleRegexCommand,
        FindCommands.ToggleSearchScopeCommand,
        FindCommands.TogglePreserveCaseCommand,
        FindCommands.ReplaceOneAction,
        FindCommands.ReplaceAllAction,
        FindCommands.SelectAllMatchesAction,
        MultiCursorCommands.AddSelectionToNextFindMatch,
        MultiCursorCommands.MoveSelectionToNextFindMatch,
        MultiCursorCommands.SelectHighlights,
        MultiCursorCommands.ChangeAll,
    };

    private bool _replaceInputLastFocused;
    private Control? _lastFocusedElement;

    /// <summary>Resolves a key press with the editor's keybindings (only <see cref="RoutedCommandIds"/> run) and tells whether it was handled.</summary>
    internal Func<KeyEventArgs, bool>? TryHandleKey { get; set; }

    /// <summary>Whether a focused element is inside the widget (Monaco: the widget's focus tracker).</summary>
    internal bool IsFocusWithin => IsKeyboardFocusWithin;

    /// <summary>Monaco: <c>CONTEXT_FIND_INPUT_FOCUSED</c>.</summary>
    internal bool IsFindInputFocused => FindTextBox.IsFocused;

    /// <summary>Monaco: <c>CONTEXT_REPLACE_INPUT_FOCUSED</c>.</summary>
    internal bool IsReplaceInputFocused => ReplaceTextBox.IsFocused;

    /// <inheritdoc />
    bool IFindWidget.IsFindInputFocused => IsFindInputFocused;

    /// <inheritdoc />
    bool IFindWidget.WasReplaceInputLastFocused => _replaceInputLastFocused;

    private void InitializeKeyboard()
    {
        AddHandler(KeyDownEvent, OnWidgetPreviewKeyDown, RoutingStrategies.Tunnel);
        AddHandler(GotFocusEvent, OnWidgetGotFocus);
    }

    // Monaco: `lastFocusedInputWasReplace` and the focus trackers (findWidget.ts:246).
    private void OnWidgetGotFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Control control)
        {
            return;
        }

        _lastFocusedElement = control;
        if (ReferenceEquals(control, ReplaceTextBox))
        {
            _replaceInputLastFocused = true;
        }
        else if (ReferenceEquals(control, FindTextBox))
        {
            _replaceInputLastFocused = false;
        }
    }

    // Monaco: `_onFindInputKeyDown` / `_onReplaceInputKeyDown` (findWidget.ts:893), then the editor's keybindings.
    private void OnWidgetPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (_controller is null)
        {
            return;
        }

        ViewKeyModifiers modifiers = KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS());
        if (e.Key == Key.Enter && modifiers == ViewKeyModifiers.Ctrl && e.Source is TextBox box)
        {
            box.SelectedText = "\n";
            e.Handled = true;
        }
        else if (e.Key == Key.Down && modifiers == ViewKeyModifiers.Ctrl)
        {
            _focusEditor?.Invoke();
            e.Handled = true;
        }
        else if (TryHandleKey?.Invoke(e) == true)
        {
            e.Handled = true;
        }
    }

    // Monaco: `focusFindInput` and `focusReplaceInput` (findWidget.ts:840): select all, then focus.
    void IFindWidget.ApplyStartFocus(FindStartFocusAction focus)
    {
        if (focus == FindStartFocusAction.NoFocusChange)
        {
            return;
        }

        // A collapsed replace row cannot take the focus.
        TextBox box = focus == FindStartFocusAction.FocusReplaceInput && ReplaceRow.IsVisible ? ReplaceTextBox : FindTextBox;
        box.SelectAll();
        FocusElement(box);
    }

    // Monaco: `focusLastElement` (findWidget.ts:341).
    void IFindWidget.FocusLastElement()
    {
        Control target = _lastFocusedElement is { IsLoaded: true } last
            ? last
            : _replaceInputLastFocused && ReplaceRow.IsVisible ? ReplaceTextBox : FindTextBox;
        FocusElement(target);
    }

    // A widget that just became visible may not take the focus in the same tick: retry once, later.
    private static void FocusElement(Control element)
    {
        if (!element.Focus())
        {
            Dispatcher.UIThread.Post(() => element.Focus(), DispatcherPriority.Background);
        }
    }
}
