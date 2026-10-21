namespace LoomEditor.View.Features.BracketMatching;

/// <summary>Ids of the commands exposed by <see cref="BracketMatchingFeature"/>. Same ids as Monaco.</summary>
internal static class BracketMatchingCommands
{
    /// <summary>Moves each cursor to the matching or enclosing bracket.</summary>
    public const string JumpToBracket = "editor.action.jumpToBracket";

    /// <summary>Selects the text inside and including the brackets.</summary>
    public const string SelectToBracket = "editor.action.selectToBracket";

    /// <summary>Removes the brackets that match or enclose each cursor.</summary>
    public const string RemoveBrackets = "editor.action.removeBrackets";
}
