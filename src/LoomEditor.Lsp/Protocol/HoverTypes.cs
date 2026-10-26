using System.Text.Json;

namespace LoomEditor.Lsp.Protocol;

/// <summary>LSP <c>Hover</c>; <c>contents</c> is a union decoded by <see cref="LspUnions.ParseHoverContents"/>.</summary>
/// <remarks>Threading: a plain data object, not thread-safe.</remarks>
internal sealed class LspHover
{
    public required JsonElement Contents { get; set; }

    public LspRange? Range { get; set; }
}
