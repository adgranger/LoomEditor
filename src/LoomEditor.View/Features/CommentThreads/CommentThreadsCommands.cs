namespace LoomEditor.View.Features.CommentThreads;

/// <summary>Ids of the commands exposed by <see cref="CommentThreadsFeature"/>.</summary>
public static class CommentThreadsCommands
{
    /// <summary>Asks the host for a thread on the selected text, on the selected whole lines, or on the cursor line (see CommentRange). No default keybinding.</summary>
    public const string Add = "loom.commentThreads.add";
}
