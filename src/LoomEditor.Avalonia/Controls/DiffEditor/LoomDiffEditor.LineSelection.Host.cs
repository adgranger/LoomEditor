using Avalonia;
using Avalonia.Input;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Controls;

// The selection of changed lines, its checkboxes and the line action. The selection lives with the control, not the view model, so SetSelection works before
// the control is loaded.
public sealed partial class LoomDiffEditor
{
    /// <summary>Identifies the <see cref="LineSelectionEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> LineSelectionEnabledProperty =
        AvaloniaProperty.Register<LoomDiffEditor, bool>(nameof(LineSelectionEnabled), false);

    /// <summary>Identifies the <see cref="LineActionGlyph"/> styled property.</summary>
    public static readonly StyledProperty<string?> LineActionGlyphProperty = RegisterGutterString(nameof(LineActionGlyph));

    /// <summary>Identifies the <see cref="LineActionToolTip"/> styled property.</summary>
    public static readonly StyledProperty<string?> LineActionToolTipProperty = RegisterGutterString(nameof(LineActionToolTip));

    /// <summary>Identifies the <see cref="LineActionKey"/> styled property.</summary>
    public static readonly StyledProperty<Key> LineActionKeyProperty =
        AvaloniaProperty.Register<LoomDiffEditor, Key>(nameof(LineActionKey), Key.S);

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
        get => GetValue(LineSelectionEnabledProperty);
        set => SetValue(LineSelectionEnabledProperty, value);
    }

    /// <summary>
    /// The glyph (a character of <see cref="GlyphFontFamily"/>) of the line action button, shown on the hovered changed line when
    /// <see cref="LineSelectionEnabled"/> is on; null (the default) hides the button. The <see cref="LineActionKey"/> works without it.
    /// </summary>
    public string? LineActionGlyph
    {
        get => GetValue(LineActionGlyphProperty);
        set => SetValue(LineActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the line action button, also the start of its automation name (<c>"{tooltip}, line {n}"</c>). Defaults to null.</summary>
    public string? LineActionToolTip
    {
        get => GetValue(LineActionToolTipProperty);
        set => SetValue(LineActionToolTipProperty, value);
    }

    /// <summary>
    /// The key that raises <see cref="LineActionInvoked"/> when <see cref="LineSelectionEnabled"/> is on in the side-by-side view, no modifier is down and the
    /// focused editor is read-only (an editable side keeps the key for typing). The lines are the selection, else the caret line if it is a changed line.
    /// Defaults to <see cref="Key.S"/>.
    /// </summary>
    public Key LineActionKey
    {
        get => GetValue(LineActionKeyProperty);
        set => SetValue(LineActionKeyProperty, value);
    }

    // Tunnels before the editors see the key (WinUI: OnPreviewKeyDown).
    private void OnLineActionKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != LineActionKey || !LineSelectionEnabled || !_options.RenderSideBySideEffective || _original is null || _modified is null
            || KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS()) != View.Commands.KeyModifiers.None)
        {
            return;
        }

        e.Handled = HandleLineActionKey(_modified.IsKeyboardFocusWithin, _original.IsKeyboardFocusWithin);
    }

    private static void OnLineSelectionEnabledChanged(LoomDiffEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue!)
        {
            editor.ClearSelection();
        }

        editor.UpdateGutterLanes();
    }

}
