namespace LoomEditor.View.Features.WordPartOperations;

/// <summary>Ids of the commands exposed by <see cref="WordPartOperationsFeature"/>. Same ids as Monaco.</summary>
public static class WordPartOperationsCommands
{
    /// <summary>Deletes the word part to the left of the cursor.</summary>
    public const string DeleteWordPartLeft = "deleteWordPartLeft";

    /// <summary>Deletes the word part to the right of the cursor.</summary>
    public const string DeleteWordPartRight = "deleteWordPartRight";

    /// <summary>Moves to the previous word part.</summary>
    public const string CursorWordPartLeft = "cursorWordPartLeft";

    /// <summary>Selects to the previous word part.</summary>
    public const string CursorWordPartLeftSelect = "cursorWordPartLeftSelect";

    /// <summary>Moves to the next word part.</summary>
    public const string CursorWordPartRight = "cursorWordPartRight";

    /// <summary>Selects to the next word part.</summary>
    public const string CursorWordPartRightSelect = "cursorWordPartRightSelect";
}
