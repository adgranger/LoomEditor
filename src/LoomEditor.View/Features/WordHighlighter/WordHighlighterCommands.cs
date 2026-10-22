namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>Ids of the commands exposed by <see cref="WordHighlighterFeature"/>. Same ids as Monaco.</summary>
internal static class WordHighlighterCommands
{
    /// <summary>Goes to the next highlighted occurrence (F7).</summary>
    public const string Next = "editor.action.wordHighlight.next";

    /// <summary>Goes to the previous highlighted occurrence (Shift+F7).</summary>
    public const string Prev = "editor.action.wordHighlight.prev";

    /// <summary>Highlights the occurrences of the word at the cursor after 250 ms (no key).</summary>
    public const string Trigger = "editor.action.wordHighlight.trigger";
}
