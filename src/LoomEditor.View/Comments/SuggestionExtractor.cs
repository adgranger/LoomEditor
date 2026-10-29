using LoomEditor.View.Markdown;

namespace LoomEditor.View.Comments;

/// <summary>Lists the <c>```suggestion</c> blocks of a comment body.</summary>
/// <remarks>
/// Threading: pure and thread-safe. Known limit: an empty block deletes the line, so replacing a line by a
/// blank line cannot be expressed.
/// </remarks>
internal static class SuggestionExtractor
{
    /// <summary>Returns the top-level suggestion blocks (language compared ordinally); quotes are ignored.</summary>
    public static IReadOnlyList<SuggestionBlock> Extract(string? body)
    {
        var result = new List<SuggestionBlock>();
        if (string.IsNullOrEmpty(body))
        {
            return result;
        }

        foreach (MarkdownBlock block in MarkdownParser.Parse(body, MarkdownParserOptions.Extended).Blocks)
        {
            if (block is MarkdownCodeBlock { Language: "suggestion" } code)
            {
                result.Add(new SuggestionBlock(result.Count, code.Code));
            }
        }

        return result;
    }
}
