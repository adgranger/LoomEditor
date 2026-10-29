namespace LoomEditor.View.Features.Snippet;

/// <summary>The ids of the snippet commands (the Monaco ids).</summary>
internal static class SnippetCommands
{
    /// <summary>Moves to the next tab stop (<c>Tab</c> in snippet mode).</summary>
    public const string JumpToNextSnippetPlaceholder = "jumpToNextSnippetPlaceholder";

    /// <summary>Moves to the previous tab stop (<c>Shift+Tab</c> in snippet mode).</summary>
    public const string JumpToPrevSnippetPlaceholder = "jumpToPrevSnippetPlaceholder";

    /// <summary>Leaves snippet mode and keeps the primary cursor (<c>Escape</c>, <c>Shift+Escape</c>).</summary>
    public const string LeaveSnippet = "leaveSnippet";

    /// <summary>Walks to the end of the snippet (no key).</summary>
    public const string AcceptSnippet = "acceptSnippet";
}
