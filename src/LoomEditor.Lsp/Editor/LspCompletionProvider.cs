using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

/// <summary>Answers the editor's completion requests with <c>textDocument/completion</c> and resolves items with <c>completionItem/resolve</c>.</summary>
/// <remarks>Threading: the methods are called on the UI thread and the tasks complete on a pool thread (ADR 0011 decision 4).</remarks>
internal sealed class LspCompletionProvider : ICompletionItemProvider
{
    private readonly LspClient _client;
    private readonly string _documentUri;

    public LspCompletionProvider(LspClient client, string documentUri)
    {
        _client = client;
        _documentUri = documentUri;
        TriggerCharacters = [.. client.CompletionTriggerCharacters];
        CanResolve = client.SupportsCompletionResolve;
    }

    public IReadOnlyList<string>? TriggerCharacters { get; }

    public bool CanResolve { get; }

    public async Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token)
    {
        if (_client.State != LspClientState.Running)
        {
            return null;
        }

        try
        {
            CompletionParams @params = new()
            {
                TextDocument = new TextDocumentIdentifier { Uri = _documentUri },
                Position = LspConvert.ToLsp(position),
                Context = LspConvert.ToLspContext(context),
            };
            JsonElement? result = await _client.RequestRawAsync(
                LspMethods.Completion, JsonSerializer.SerializeToElement(@params, LspJsonContext.Default.CompletionParams), token).ConfigureAwait(false);
            return LspConvert.ToCompletionList(result);
        }
        catch (Exception e) when (IsBenign(e))
        {
            return null;
        }
    }

    public async Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token)
    {
        if (item.ProviderData is not LspCompletionItem raw || _client.State != LspClientState.Running)
        {
            return null;
        }

        try
        {
            LspCompletionItem? resolved = await _client.RequestAsync(
                LspMethods.CompletionResolve, raw, LspJsonContext.Default.LspCompletionItem, LspJsonContext.Default.LspCompletionItem, token).ConfigureAwait(false);
            return resolved is null ? null : LspConvert.ApplyResolved(resolved);
        }
        catch (Exception e) when (IsBenign(e))
        {
            return null;
        }
    }

    // The document changed under the server (the next keystroke asks again) or the client stopped.
    private static bool IsBenign(Exception e) => e is LspClientException
        || e is JsonRpcException { Code: JsonRpcErrorCodes.ContentModified or JsonRpcErrorCodes.RequestCancelled or JsonRpcErrorCodes.ServerCancelled };
}
