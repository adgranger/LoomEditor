using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features;

/// <summary>
/// What a feature can see and do in the editor it is attached to.
/// </summary>
/// <remarks>
/// <para>
/// This interface will grow as phases are delivered (text model in phase 1, cursors in phase 3, …).
/// Only add a member when a feature actually needs it.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
public interface IEditorContext
{
    /// <summary>The commands of this editor. Features register their commands here.</summary>
    CommandRegistry Commands { get; }

    /// <summary>
    /// The text of the document. Always contains at least one (possibly empty) line.
    /// Subscribe to <see cref="TextModel.ContentChanged"/> to be told about edits.
    /// </summary>
    TextModel Model { get; }

    /// <summary>The current selection (a single cursor until multi-cursor support in phase 3).</summary>
    Selection Selection { get; set; }

    /// <summary>Raised after <see cref="Selection"/> changes.</summary>
    event EventHandler? SelectionChanged;
}
