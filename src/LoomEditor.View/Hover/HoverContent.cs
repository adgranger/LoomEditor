namespace LoomEditor.View.Hover;

/// <summary>What the hover popup shows: the parts of one anchor, sorted.</summary>
/// <remarks>Threading: immutable.</remarks>
/// <param name="Anchor">The anchor the parts were computed for.</param>
/// <param name="Parts">The parts, ascending by <see cref="IHoverPart.Order"/>, then by provider ordinal for Markdown.</param>
/// <param name="IsComplete">True when no more parts will come.</param>
internal sealed record HoverContent(HoverAnchor Anchor, IReadOnlyList<IHoverPart> Parts, bool IsComplete)
{
    /// <summary>Sorts the parts and builds the content.</summary>
    public static HoverContent Create(HoverAnchor anchor, IEnumerable<IHoverPart> parts, bool isComplete) =>
        new(
            anchor,
            [.. parts.OrderBy(p => p.Order).ThenBy(p => p is MarkdownHoverPart m ? m.Ordinal : 0)],
            isComplete);
}
