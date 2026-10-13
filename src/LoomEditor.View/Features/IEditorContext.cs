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
    /// The lines of the document, without their end-of-line characters.
    /// Always contains at least one (possibly empty) line.
    /// </summary>
    /// <remarks>Temporary: will be replaced by a <c>TextModel</c> in phase 1.</remarks>
    IReadOnlyList<string> Lines { get; }

    /// <summary>The current selection (a single cursor until multi-cursor support in phase 3).</summary>
    Selection Selection { get; set; }

    /// <summary>Raised after <see cref="Selection"/> changes.</summary>
    event EventHandler? SelectionChanged;
}
