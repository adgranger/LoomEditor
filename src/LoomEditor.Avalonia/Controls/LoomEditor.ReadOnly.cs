using Avalonia;

namespace LoomEditor.Avalonia.Controls;

// Read-only mode: the editor context owns the flag (the cursor refuses user edits, the keybindings read it); this part
// is the styled property.
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
        _findWidget?.OnReadOnlyChanged();
        _cursorBlinking.Update(HasFocus, readOnly);
        if (readOnly)
        {
            // WinUI leaves and re-enters the focus to make the text services end the composition.
            _ime.Reset();
        }
    }
}
