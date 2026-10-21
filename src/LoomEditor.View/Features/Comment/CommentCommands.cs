namespace LoomEditor.View.Features.Comment;

/// <summary>Ids of the commands exposed by <see cref="CommentFeature"/>.</summary>
public static class CommentCommands
{
    /// <summary>Toggles line comments on the selected lines. Same id as in Monaco.</summary>
    public const string CommentLine = "editor.action.commentLine";

    /// <summary>Adds line comments to the selected lines. Same id as in Monaco.</summary>
    public const string AddCommentLine = "editor.action.addCommentLine";

    /// <summary>Removes line comments from the selected lines. Same id as in Monaco.</summary>
    public const string RemoveCommentLine = "editor.action.removeCommentLine";

    /// <summary>Toggles a block comment around the selection. Same id as in Monaco.</summary>
    public const string BlockComment = "editor.action.blockComment";
}
