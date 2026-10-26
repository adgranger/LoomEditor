using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Client;

internal sealed partial class LspClient
{
    private static readonly JsonElement NullResult = JsonElement.Parse("null");
    private static readonly JsonElement NotApplied = JsonElement.Parse(
        """{"applied":false,"failureReason":"LoomEditor does not apply workspace edits"}""");

    private Task<JsonElement?> HandleServerRequestAsync(string method, JsonElement? @params, CancellationToken token)
    {
        switch (method)
        {
            case LspMethods.WorkspaceConfiguration:
                return Task.FromResult<JsonElement?>(ConfigurationResult(@params));
            case LspMethods.RegisterCapability:
            case LspMethods.UnregisterCapability:
            case LspMethods.WorkDoneProgressCreate:
            case LspMethods.ShowMessageRequest:
                return Task.FromResult<JsonElement?>(NullResult);
            case LspMethods.ApplyEdit:
                return Task.FromResult<JsonElement?>(NotApplied);
            default:
                throw new JsonRpcException(JsonRpcErrorCodes.MethodNotFound, $"Method not found: {method}");
        }
    }

    /// <summary>One <c>null</c> per requested item: LoomEditor has no configuration to give.</summary>
    private static JsonElement ConfigurationResult(JsonElement? @params)
    {
        int count = @params is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty("items", out JsonElement items)
            && items.ValueKind == JsonValueKind.Array
            ? items.GetArrayLength()
            : 0;
        return JsonElement.Parse("[" + string.Join(",", Enumerable.Repeat("null", count)) + "]");
    }
}
