using LoomEditor.View.DiffEditor;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace LoomEditor.WinUI.Controls;

// The selection of changed lines, its checkboxes and the line action. The selection lives with the control, not the view model, so SetSelection works before
// the control is loaded.
public sealed partial class LoomDiffEditor
{
    /// <summary>Identifies the <see cref="LineSelectionEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty LineSelectionEnabledProperty = DependencyProperty.Register(
        nameof(LineSelectionEnabled), typeof(bool), typeof(LoomDiffEditor), new PropertyMetadata(false, OnLineSelectionEnabledChanged));

    /// <summary>Identifies the <see cref="LineActionGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty LineActionGlyphProperty = RegisterGutterString(nameof(LineActionGlyph));

    /// <summary>Identifies the <see cref="LineActionToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty LineActionToolTipProperty = RegisterGutterString(nameof(LineActionToolTip));

    /// <summary>Identifies the <see cref="LineActionKey"/> dependency property.</summary>
    public static readonly DependencyProperty LineActionKeyProperty = DependencyProperty.Register(
        nameof(LineActionKey), typeof(VirtualKey), typeof(LoomDiffEditor), new PropertyMetadata(VirtualKey.S));

    private readonly DiffLineSelection _lineSelection = new();
    private int _selectionOriginalVersionId = -1;
    private int _selectionModifiedVersionId = -1;

    /// <summary>Raised when <see cref="SelectedLines"/> changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Raised by the line action button or the <see cref="LineActionKey"/>, on the UI thread. The host decides what the action does; the editor changes nothing.
    /// </summary>
    public event EventHandler<DiffLineActionInvokedEventArgs>? LineActionInvoked;

    /// <summary>
    /// Whether every changed line of the side-by-side view has a checkbox in the glyph margin (deleted lines on the left, added lines on the right): a click
    /// toggles the line, Ctrl+click the line and its partner on the other side, Shift+click adds the changed lines from the last clicked one on the same side.
    /// Turning it off clears the selection. No checkbox in the inline view. Defaults to false.
    /// </summary>
    public bool LineSelectionEnabled
    {
        get => (bool)GetValue(LineSelectionEnabledProperty);
        set => SetValue(LineSelectionEnabledProperty, value);
    }

    /// <summary>
    /// The glyph (a character of the Segoe Fluent Icons / Segoe MDL2 Assets font) of the line action button, shown on the hovered changed line when
    /// <see cref="LineSelectionEnabled"/> is on; null (the default) hides the button. The <see cref="LineActionKey"/> works without it.
    /// </summary>
    public string? LineActionGlyph
    {
        get => (string?)GetValue(LineActionGlyphProperty);
        set => SetValue(LineActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the line action button, also the start of its automation name (<c>"{tooltip}, line {n}"</c>). Defaults to null.</summary>
    public string? LineActionToolTip
    {
        get => (string?)GetValue(LineActionToolTipProperty);
        set => SetValue(LineActionToolTipProperty, value);
    }

    /// <summary>
    /// The key that raises <see cref="LineActionInvoked"/> when <see cref="LineSelectionEnabled"/> is on in the side-by-side view, no modifier is down and the
    /// focused editor is read-only (an editable side keeps the key for typing). The lines are the selection, else the caret line if it is a changed line.
    /// Defaults to <see cref="VirtualKey.S"/>.
    /// </summary>
    public VirtualKey LineActionKey
    {
        get => (VirtualKey)GetValue(LineActionKeyProperty);
        set => SetValue(LineActionKeyProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPreviewKeyDown(e);
        if (e.Handled || e.Key != LineActionKey || !LineSelectionEnabled || !_options.RenderSideBySideEffective || _original is null || _modified is null
            || IsDown(VirtualKey.Control) || IsDown(VirtualKey.Shift) || IsDown(VirtualKey.Menu) || IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
        {
            return;
        }

        e.Handled = HandleLineActionKey(_modified.FocusState != FocusState.Unfocused, _original.FocusState != FocusState.Unfocused);
    }

    private static void OnLineSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomDiffEditor)d;
        if (!(bool)e.NewValue)
        {
            editor.ClearSelection();
        }

        editor.UpdateGutterLanes();
    }

    private static bool IsDown(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

}
