namespace LoomEditor.View.Features.CursorUndo;

/// <summary>Ids of the commands exposed by <see cref="CursorUndoFeature"/>. Same ids as in Monaco.</summary>
public static class CursorUndoCommands
{
    /// <summary>Restores the previous cursor positions (<c>Ctrl+U</c>).</summary>
    public const string CursorUndo = "cursorUndo";

    /// <summary>Restores the cursor positions undone by <see cref="CursorUndo"/> (no key).</summary>
    public const string CursorRedo = "cursorRedo";
}
