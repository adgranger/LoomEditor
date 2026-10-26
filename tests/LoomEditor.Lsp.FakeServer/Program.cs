using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

// --selftest proves that the source-generated types and the union decoders work under Native AOT.
if (args is ["--selftest"])
{
    byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
        new JsonRpcMessage { Id = JsonElement.Parse("1"), Method = "selftest" }, LspJsonContext.Default.JsonRpcMessage);
    JsonRpcMessage? back = JsonSerializer.Deserialize(bytes, LspJsonContext.Default.JsonRpcMessage);
    bool ok = back?.Method == "selftest" && back.Id?.GetInt64() == 1;

    InitializeParams init = new() { ProcessId = 1, Capabilities = LspClientCapabilities.Create() };
    InitializeParams? initBack = JsonSerializer.Deserialize(
        JsonSerializer.SerializeToUtf8Bytes(init, LspJsonContext.Default.InitializeParams), LspJsonContext.Default.InitializeParams);
    ok &= initBack?.Capabilities.TextDocument?.Completion?.CompletionItem?.ResolveSupport?.Properties.Length == 3;

    InitializeResult? result = JsonSerializer.Deserialize(
        """{"capabilities":{"textDocumentSync":2,"hoverProvider":true,"completionProvider":{"triggerCharacters":["."]}}}"""u8, LspJsonContext.Default.InitializeResult);
    ok &= result is not null
        && LspUnions.ParseTextDocumentSync(result.Capabilities.TextDocumentSync) == (true, 2)
        && LspUnions.IsEnabled(result.Capabilities.HoverProvider);

    LspCompletionItem? item = JsonSerializer.Deserialize("""{"label":"x","data":{"k":[1,2.5]}}"""u8, LspJsonContext.Default.LspCompletionItem);
    LspCompletionItem? itemBack = JsonSerializer.Deserialize(
        JsonSerializer.SerializeToUtf8Bytes(item, LspJsonContext.Default.LspCompletionItem), LspJsonContext.Default.LspCompletionItem);
    ok &= itemBack?.Data?.GetRawText() == item?.Data?.GetRawText();

    ok &= LspUnions.ParseHoverContents(JsonElement.Parse("""[{"language":"cs","value":"int x;"}]""")).Count == 1;
    ok &= LspUnions.ParseDefinitionResult(JsonElement.Parse("""{"uri":"u","range":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}}}""")).Count == 1;

    if (ok)
    {
        Console.Out.WriteLine("ok");
    }

    return 0;
}

// Standard output carries protocol frames only; anything for a human goes to standard error.
await using Stream stdin = Console.OpenStandardInput();
await using Stream stdout = Console.OpenStandardOutput();
using LoomEditor.Lsp.FakeServer.FakeLanguageServer server = new(stdin, stdout, LoomEditor.Lsp.FakeServer.FakeServerOptions.Parse(args), exitProcess: true);
return await server.RunAsync(CancellationToken.None);
