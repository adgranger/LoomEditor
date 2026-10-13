using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features;

/// <summary>
/// The default <see cref="IEditorContext"/>: holds the text, the selection and the commands of one editor.
/// </summary>
/// <remarks>Threading: UI thread only. Not thread-safe.</remarks>
public sealed class EditorContext : IEditorContext
{
    private Selection _selection = Selection.Collapsed(new Position(1, 1));

    /// <inheritdoc />
    public CommandRegistry Commands { get; } = new();

    /// <inheritdoc />
    public TextModel Model { get; } = new(string.Empty);

    /// <inheritdoc />
    public Selection Selection
    {
        get => _selection;
        set
        {
            if (_selection == value)
            {
                return;
            }

            _selection = value;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Replaces the whole text. Accepts <c>\r\n</c>, <c>\n</c> and <c>\r</c> line breaks.
    /// The selection is reset to the start of the document.
    /// </summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Model.SetValue(text);
        Selection = Selection.Collapsed(new Position(1, 1));
    }
}
