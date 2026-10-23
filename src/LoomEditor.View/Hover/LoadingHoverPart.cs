namespace LoomEditor.View.Hover;

/// <summary>The "Loading…" message shown while providers are pending (Monaco <c>createLoadingMessage</c>, ordinal 2000).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LoadingHoverPart(Range Range) : IHoverPart
{
    /// <inheritdoc />
    public int Order => 2000;

    /// <inheritdoc />
    public bool IsValidForHoverAnchor(HoverAnchor anchor) => HoverPartRules.Covers(Range, anchor);
}
