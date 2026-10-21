using LoomEditor.View.Commands;
using LoomEditor.View.Features.Find;
using LoomEditor.WinUI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace LoomEditor.WinUI.Controls.Find;

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
    };

    private bool _replaceInputLastFocused;
    private Control? _lastFocusedElement;

    /// <summary>Resolves a key press with the editor's keybindings (only <see cref="RoutedCommandIds"/> run) and tells whether it was handled.</summary>
    internal Func<VirtualKey, bool>? TryHandleKey { get; set; }

    /// <summary>Whether a focused element is inside the widget (Monaco: the widget's focus tracker).</summary>
    internal bool IsFocusWithin => IsWithin(FocusManager.GetFocusedElement(XamlRoot));

    /// <summary>Monaco: <c>CONTEXT_FIND_INPUT_FOCUSED</c>.</summary>
    internal bool IsFindInputFocused => FindTextBox.FocusState != FocusState.Unfocused;

    /// <summary>Monaco: <c>CONTEXT_REPLACE_INPUT_FOCUSED</c>.</summary>
    internal bool IsReplaceInputFocused => ReplaceTextBox.FocusState != FocusState.Unfocused;

    /// <inheritdoc />
    bool IFindWidget.IsFindInputFocused => IsFindInputFocused;

    /// <inheritdoc />
    bool IFindWidget.WasReplaceInputLastFocused => _replaceInputLastFocused;

    private void InitializeKeyboard()
    {
        PreviewKeyDown += OnWidgetPreviewKeyDown;
        GotFocus += OnWidgetGotFocus;
    }

    // Monaco: `lastFocusedInputWasReplace` and the focus trackers (findWidget.ts:246).
    private void OnWidgetGotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Control control)
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
    private void OnWidgetPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_controller is null)
        {
            return;
        }

        KeyModifiers modifiers = KeyboardInput.CurrentModifiers();
        if (e.Key == VirtualKey.Enter && modifiers == KeyModifiers.Ctrl && e.OriginalSource is TextBox box)
        {
            InsertLineBreak(box);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Down && modifiers == KeyModifiers.Ctrl)
        {
            _focusEditor?.Invoke();
            e.Handled = true;
        }
        else if (TryHandleKey?.Invoke(e.Key) == true)
        {
            e.Handled = true;
        }
    }

    // WinUI's TextBox stores line breaks as `\r`; the change goes through TextChanged like typing.
    private static void InsertLineBreak(TextBox box)
    {
        int start = box.SelectionStart;
        box.Text = box.Text.Remove(start, box.SelectionLength).Insert(start, "\r");
        box.SelectionStart = start + 1;
        box.SelectionLength = 0;
    }

    // Monaco: `focusFindInput` and `focusReplaceInput` (findWidget.ts:840): select all, then focus.
    void IFindWidget.ApplyStartFocus(FindStartFocusAction focus)
    {
        if (focus == FindStartFocusAction.NoFocusChange)
        {
            return;
        }

        // A collapsed replace row cannot take the focus.
        TextBox box = focus == FindStartFocusAction.FocusReplaceInput && ReplaceRow.Visibility == Visibility.Visible ? ReplaceTextBox : FindTextBox;
        box.SelectAll();
        FocusElement(box);
    }

    // Monaco: `focusLastElement` (findWidget.ts:341).
    void IFindWidget.FocusLastElement()
    {
        Control target = _lastFocusedElement is { IsLoaded: true } last
            ? last
            : _replaceInputLastFocused && ReplaceRow.Visibility == Visibility.Visible ? ReplaceTextBox : FindTextBox;
        FocusElement(target);
    }

    // A widget that just became visible may not take the focus in the same tick: retry once, later.
    private void FocusElement(Control element)
    {
        if (!element.Focus(FocusState.Programmatic))
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => element.Focus(FocusState.Programmatic));
        }
    }

    private bool IsWithin(object? element)
    {
        var current = element as DependencyObject;
        while (current is not null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
