using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

// P8-28 writes the server loop. --selftest proves that the source-generated envelope works under Native AOT.
if (args is ["--selftest"])
{
    byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
        new JsonRpcMessage { Id = JsonElement.Parse("1"), Method = "selftest" }, LspJsonContext.Default.JsonRpcMessage);
    JsonRpcMessage? back = JsonSerializer.Deserialize(bytes, LspJsonContext.Default.JsonRpcMessage);
    if (back?.Method == "selftest" && back.Id?.GetInt64() == 1)
    {
        Console.Out.WriteLine("ok");
    }
}

return 0;
