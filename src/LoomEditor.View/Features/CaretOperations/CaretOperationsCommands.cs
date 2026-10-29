namespace LoomEditor.View.Features.CaretOperations;

/// <summary>Ids of the commands exposed by <see cref="CaretOperationsFeature"/>. Same ids as in Monaco ("Carret" is Monaco's spelling).</summary>
public static class CaretOperationsCommands
{
    /// <summary>Moves the selected text one character to the left.</summary>
    public const string MoveCaretLeft = "editor.action.moveCarretLeftAction";

    /// <summary>Moves the selected text one character to the right.</summary>
    public const string MoveCaretRight = "editor.action.moveCarretRightAction";
}
