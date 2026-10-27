using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Read-only mode: the editor context owns the flag (the cursor refuses user edits, the keybindings read it); this part
// is the dependency property, and it ends the IME composition when the editor becomes read-only.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnIsReadOnlyChanged));

    /// <summary>
    /// Whether the text cannot be edited by the user (Monaco: <c>readOnly</c>): typing, deleting, cut, paste, undo, redo, IME
    /// and the editing commands do nothing, while selection, copy, find, scrolling and navigation keep working. Changes made
    /// to the text by the application are not blocked. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        bool readOnly = (bool)e.NewValue;
        editor._context.IsReadOnly = readOnly;
        editor._cursorBlinking.Update(editor.HasFocus, readOnly);
        editor._findWidget?.OnReadOnlyChanged();
        if (readOnly && editor.IsComposing && editor.HasFocus)
        {
            // Leaving and re-entering the focus makes the text services end the composition.
            editor._ime?.NotifyFocusLeave();
            editor._ime?.NotifyFocusEnter();
        }
    }
}
