namespace LoomEditor.Rendering;

/// <summary>
/// Where the current line is highlighted. Monaco: the <c>renderLineHighlight</c> editor option.
/// </summary>
/// <remarks>Threading: an immutable value, safe to use from any thread.</remarks>
internal enum RenderLineHighlight
{
    /// <summary>No highlight.</summary>
    None,

    /// <summary>Highlight in the gutter only.</summary>
    Gutter,

    /// <summary>Highlight in the content only (Monaco's default).</summary>
    Line,

    /// <summary>Highlight in the gutter and in the content.</summary>
    All,
}
