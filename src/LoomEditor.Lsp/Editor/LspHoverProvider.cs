using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

/// <summary>Answers the editor's hover requests with <c>textDocument/hover</c>.</summary>
/// <remarks>Threading: called on the UI thread, completes on a pool thread; never touches the model (ADR 0011 decision 4).</remarks>
internal sealed class LspHoverProvider(LspClient client, string documentUri) : IHoverProvider
{
    public async Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token)
    {
        if (client.State != LspClientState.Running)
        {
            return null;
        }

        try
        {
            TextDocumentPositionParams @params = new()
            {
                TextDocument = new TextDocumentIdentifier { Uri = documentUri },
                Position = LspConvert.ToLsp(position),
            };
            JsonElement? result = await client.RequestRawAsync(
                LspMethods.Hover, JsonSerializer.SerializeToElement(@params, LspJsonContext.Default.TextDocumentPositionParams), token).ConfigureAwait(false);
            return LspConvert.ToHover(result);
        }
        catch (Exception e) when (LspRequestErrors.IsBenign(e))
        {
            return null;
        }
    }
}
