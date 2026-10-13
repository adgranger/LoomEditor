// Replaces microsoft/vscode: src/vs/platform/undoRedo/common/undoRedoService.ts (IUndoRedoService), for one model only

namespace LoomEditor.Core.Undo;

/// <summary>The undo and redo lists of one model.</summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco's <c>UndoRedoService</c> handles every resource of the workspace, groups, snapshots and
/// confirmation dialogs. This class is the small per-model part of it: push, last element, undo, redo.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class UndoRedoStack
{
    private readonly List<SingleModelEditStackElement> _past = [];
    private readonly List<SingleModelEditStackElement> _future = [];

    public bool CanUndo => _past.Count > 0;

    public bool CanRedo => _future.Count > 0;

    /// <summary>Adds a step; the redo list is cleared.</summary>
    public void Push(SingleModelEditStackElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _future.Clear();
        _past.Add(element);
    }

    /// <summary>The step that edits can still be appended to, or <c>null</c> when there is none (nothing pushed, or something was undone).</summary>
    public SingleModelEditStackElement? GetLastElement() =>
        _future.Count > 0 || _past.Count == 0 ? null : _past[^1];

    /// <summary>Moves the last step to the redo list, closes it and returns it; <c>null</c> when there is nothing to undo.</summary>
    public SingleModelEditStackElement? Undo() => Move(_past, _future);

    /// <summary>Moves the last undone step back to the undo list, closes it and returns it; <c>null</c> when there is nothing to redo.</summary>
    public SingleModelEditStackElement? Redo() => Move(_future, _past);

    public void Clear()
    {
        _past.Clear();
        _future.Clear();
    }

    private static SingleModelEditStackElement? Move(List<SingleModelEditStackElement> from, List<SingleModelEditStackElement> to)
    {
        if (from.Count == 0)
        {
            return null;
        }

        SingleModelEditStackElement element = from[^1];
        from.RemoveAt(from.Count - 1);
        element.Close();
        to.Add(element);
        return element;
    }
}
