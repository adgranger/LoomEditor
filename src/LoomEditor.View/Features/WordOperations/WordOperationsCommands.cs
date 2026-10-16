namespace LoomEditor.View.Features.WordOperations;

/// <summary>Ids of the commands exposed by <see cref="WordOperationsFeature"/>. Same ids as Monaco.</summary>
public static class WordOperationsCommands
{
    /// <summary>Moves to the start of the previous word.</summary>
    public const string CursorWordStartLeft = "cursorWordStartLeft";

    /// <summary>Moves to the end of the previous word.</summary>
    public const string CursorWordEndLeft = "cursorWordEndLeft";

    /// <summary>Moves to the previous word (Ctrl+Left).</summary>
    public const string CursorWordLeft = "cursorWordLeft";

    /// <summary>Selects to the start of the previous word.</summary>
    public const string CursorWordStartLeftSelect = "cursorWordStartLeftSelect";

    /// <summary>Selects to the end of the previous word.</summary>
    public const string CursorWordEndLeftSelect = "cursorWordEndLeftSelect";

    /// <summary>Selects to the previous word (Ctrl+Shift+Left).</summary>
    public const string CursorWordLeftSelect = "cursorWordLeftSelect";

    /// <summary>Moves to the previous word, like a screen reader does.</summary>
    public const string CursorWordAccessibilityLeft = "cursorWordAccessibilityLeft";

    /// <summary>Selects to the previous word, like a screen reader does.</summary>
    public const string CursorWordAccessibilityLeftSelect = "cursorWordAccessibilityLeftSelect";

    /// <summary>Moves to the start of the next word.</summary>
    public const string CursorWordStartRight = "cursorWordStartRight";

    /// <summary>Moves to the end of the next word.</summary>
    public const string CursorWordEndRight = "cursorWordEndRight";

    /// <summary>Moves to the next word (Ctrl+Right).</summary>
    public const string CursorWordRight = "cursorWordRight";

    /// <summary>Selects to the start of the next word.</summary>
    public const string CursorWordStartRightSelect = "cursorWordStartRightSelect";

    /// <summary>Selects to the end of the next word.</summary>
    public const string CursorWordEndRightSelect = "cursorWordEndRightSelect";

    /// <summary>Selects to the next word (Ctrl+Shift+Right).</summary>
    public const string CursorWordRightSelect = "cursorWordRightSelect";

    /// <summary>Moves to the next word, like a screen reader does.</summary>
    public const string CursorWordAccessibilityRight = "cursorWordAccessibilityRight";

    /// <summary>Selects to the next word, like a screen reader does.</summary>
    public const string CursorWordAccessibilityRightSelect = "cursorWordAccessibilityRightSelect";
}
