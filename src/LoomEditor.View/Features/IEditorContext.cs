using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.Scheduling;

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
    /// <summary>Runs actions on the UI thread after a delay. Features use it instead of timers of their own.</summary>
    IEditorScheduler Scheduler { get; }

    /// <summary>The commands of this editor. Features register their commands here.</summary>
    CommandRegistry Commands { get; }

    /// <summary>
    /// The text of the document. Always contains at least one (possibly empty) line.
    /// Subscribe to <see cref="TextModel.ContentChanged"/> to be told about edits.
    /// </summary>
    TextModel Model { get; }

    /// <summary>The primary selection (the first of <see cref="Selections"/>).</summary>
    Selection Selection { get; set; }

    /// <summary>All the selections, one per cursor; the primary one is first.</summary>
    IReadOnlyList<Selection> Selections { get; }

    /// <summary>Replaces the selections; the first one becomes the primary selection.</summary>
    /// <exception cref="ArgumentException"><paramref name="selections"/> is empty.</exception>
    void SetSelections(IReadOnlyList<Selection> selections);

    /// <summary>Raised after <see cref="Selection"/> changes.</summary>
    event EventHandler? SelectionChanged;

    /// <summary>Whether user edits are ignored (Monaco: the <c>readOnly</c> option). Model edits made through <see cref="Model"/> still work.</summary>
    bool IsReadOnly { get; set; }

    /// <summary>Raised after <see cref="IsReadOnly"/> really changes.</summary>
    event EventHandler? ReadOnlyChanged;
}
