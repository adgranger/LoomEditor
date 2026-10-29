namespace LoomEditor.View.Comments;

/// <summary>One <c>```suggestion</c> block of a comment body.</summary>
/// <param name="Index">0-based order among the suggestion blocks of the body.</param>
/// <param name="Text">The code of the block, without trailing newline.</param>
/// <remarks>Threading: immutable, thread-safe.</remarks>
internal sealed record SuggestionBlock(int Index, string Text)
{
    /// <summary>The lines of <see cref="Text"/>; empty for a deletion.</summary>
    public IReadOnlyList<string> Lines => Text.Length == 0 ? [] : Text.Split('\n');

    /// <summary>An empty block means "delete the line" (as on GitHub).</summary>
    public bool IsDeletion => Text.Length == 0;
}
