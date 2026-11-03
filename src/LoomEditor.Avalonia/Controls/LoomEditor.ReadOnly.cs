using Avalonia;

namespace LoomEditor.Avalonia.Controls;

// Read-only mode: the editor context owns the flag (the cursor refuses user edits, the keybindings read it); this part
// is the styled property. (Ending the IME composition when the editor becomes read-only comes with P15-13.)
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="IsReadOnly"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(IsReadOnly), false);

    /// <summary>
    /// Whether the text cannot be edited by the user (Monaco: <c>readOnly</c>): typing, deleting, cut, paste, undo, redo, IME
    /// and the editing commands do nothing, while selection, copy, find, scrolling and navigation keep working. Changes made
    /// to the text by the application are not blocked. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private static void InitializeReadOnlyProperties() =>
        IsReadOnlyProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnIsReadOnlyChanged((bool)e.NewValue!));

    private void OnIsReadOnlyChanged(bool readOnly)
    {
        _context.IsReadOnly = readOnly;
        _cursorBlinking.Update(HasFocus, readOnly);
    }
}
