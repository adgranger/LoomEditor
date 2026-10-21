namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Ids of the commands exposed by <see cref="LinesOperationsFeature"/>. Same ids as in Monaco.</summary>
public static class LinesOperationsCommands
{
    /// <summary>Copies the selected lines above themselves.</summary>
    public const string CopyLinesUp = "editor.action.copyLinesUpAction";

    /// <summary>Copies the selected lines below themselves.</summary>
    public const string CopyLinesDown = "editor.action.copyLinesDownAction";

    /// <summary>Duplicates the selection.</summary>
    public const string DuplicateSelection = "editor.action.duplicateSelection";

    /// <summary>Moves the selected lines up.</summary>
    public const string MoveLinesUp = "editor.action.moveLinesUpAction";

    /// <summary>Moves the selected lines down.</summary>
    public const string MoveLinesDown = "editor.action.moveLinesDownAction";

    /// <summary>Sorts the selected lines in ascending order.</summary>
    public const string SortLinesAscending = "editor.action.sortLinesAscending";

    /// <summary>Sorts the selected lines in descending order.</summary>
    public const string SortLinesDescending = "editor.action.sortLinesDescending";

    /// <summary>Removes the duplicate lines.</summary>
    public const string RemoveDuplicateLines = "editor.action.removeDuplicateLines";

    /// <summary>Reverses the selected lines.</summary>
    public const string ReverseLines = "editor.action.reverseLines";

    /// <summary>Trims the trailing whitespace.</summary>
    public const string TrimTrailingWhitespace = "editor.action.trimTrailingWhitespace";

    /// <summary>Deletes the selected lines.</summary>
    public const string DeleteLines = "editor.action.deleteLines";

    /// <summary>Indents the selected lines.</summary>
    public const string IndentLines = "editor.action.indentLines";

    /// <summary>Outdents the selected lines.</summary>
    public const string OutdentLines = "editor.action.outdentLines";

    /// <summary>Inserts a line above the current line.</summary>
    public const string InsertLineBefore = "editor.action.insertLineBefore";

    /// <summary>Inserts a line below the current line.</summary>
    public const string InsertLineAfter = "editor.action.insertLineAfter";

    /// <summary>Deletes everything to the left of the cursor.</summary>
    public const string DeleteAllLeft = "deleteAllLeft";

    /// <summary>Deletes everything to the right of the cursor.</summary>
    public const string DeleteAllRight = "deleteAllRight";

    /// <summary>Joins the selected lines.</summary>
    public const string JoinLines = "editor.action.joinLines";

    /// <summary>Transposes the letters around the cursor.</summary>
    public const string Transpose = "editor.action.transpose";

    /// <summary>Transforms the selection to upper case.</summary>
    public const string TransformToUppercase = "editor.action.transformToUppercase";

    /// <summary>Transforms the selection to lower case.</summary>
    public const string TransformToLowercase = "editor.action.transformToLowercase";

    /// <summary>Transforms the selection to title case.</summary>
    public const string TransformToTitlecase = "editor.action.transformToTitlecase";

    /// <summary>Transforms the selection to snake case.</summary>
    public const string TransformToSnakecase = "editor.action.transformToSnakecase";

    /// <summary>Transforms the selection to camel case.</summary>
    public const string TransformToCamelcase = "editor.action.transformToCamelcase";

    /// <summary>Transforms the selection to Pascal case.</summary>
    public const string TransformToPascalcase = "editor.action.transformToPascalcase";

    /// <summary>Transforms the selection to kebab case.</summary>
    public const string TransformToKebabcase = "editor.action.transformToKebabcase";
}
