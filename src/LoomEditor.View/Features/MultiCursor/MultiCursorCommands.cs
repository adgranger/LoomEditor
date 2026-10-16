namespace LoomEditor.View.Features.MultiCursor;

/// <summary>Ids of the commands exposed by <see cref="MultiCursorFeature"/>. Same ids as Monaco.</summary>
public static class MultiCursorCommands
{
    /// <summary>Adds a cursor above each cursor.</summary>
    public const string InsertCursorAbove = "editor.action.insertCursorAbove";

    /// <summary>Adds a cursor below each cursor.</summary>
    public const string InsertCursorBelow = "editor.action.insertCursorBelow";

    /// <summary>Puts a cursor at the end of each line of the selections.</summary>
    public const string InsertCursorAtEndOfEachLineSelected = "editor.action.insertCursorAtEndOfEachLineSelected";

    /// <summary>Adds a cursor on every line from the first selection down to the end of the text.</summary>
    public const string AddCursorsToBottom = "editor.action.addCursorsToBottom";

    /// <summary>Adds a cursor on every line from the first selection up to the top of the text.</summary>
    public const string AddCursorsToTop = "editor.action.addCursorsToTop";

    /// <summary>Makes the next cursor the primary cursor.</summary>
    public const string FocusNextCursor = "editor.action.focusNextCursor";

    /// <summary>Makes the previous cursor the primary cursor.</summary>
    public const string FocusPreviousCursor = "editor.action.focusPreviousCursor";
}
