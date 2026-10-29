using System.Text.Json;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Protocol;

public class RoundTripTests
{
    private static LspPosition P(int l, int c) => new() { Line = l, Character = c };

    private static LspRange R(int l1, int c1, int l2, int c2) => new() { Start = P(l1, c1), End = P(l2, c2) };

    [Fact]
    public void InitializeParams_Serializes_RequiredNulls()
    {
        string json = JsonSerializer.Serialize(new InitializeParams { Capabilities = new ClientCapabilities() }, LspJsonContext.Default.InitializeParams);

        JsonAssert.Equal("""{"processId":null,"rootUri":null,"capabilities":{}}""", json);
    }

    [Fact]
    public void InitializeParams_Full()
    {
        InitializeParams p = new()
        {
            ProcessId = 42,
            ClientInfo = new ClientInfoDto { Name = "LoomEditor", Version = "1.0" },
            RootUri = "file:///r",
            Capabilities = LspClientCapabilities.Create(),
        };

        string json = JsonSerializer.Serialize(p, LspJsonContext.Default.InitializeParams);

        JsonAssert.Equal("""
            {"processId":42,"clientInfo":{"name":"LoomEditor","version":"1.0"},"rootUri":"file:///r","capabilities":{
            "general":{"positionEncodings":["utf-16"]},
            "textDocument":{
              "synchronization":{"dynamicRegistration":false,"willSave":false,"willSaveWaitUntil":false,"didSave":false},
              "completion":{"dynamicRegistration":false,"contextSupport":true,
                "completionItem":{"snippetSupport":true,"documentationFormat":["markdown","plaintext"],"deprecatedSupport":true,"preselectSupport":true,
                  "tagSupport":{"valueSet":[1]},"insertReplaceSupport":true,
                  "resolveSupport":{"properties":["documentation","detail","additionalTextEdits"]},"labelDetailsSupport":true,"commitCharactersSupport":true},
                "completionItemKind":{"valueSet":[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25]}},
              "hover":{"dynamicRegistration":false,"contentFormat":["markdown","plaintext"]},
              "definition":{"dynamicRegistration":false,"linkSupport":true},
              "publishDiagnostics":{"relatedInformation":false,"tagSupport":{"valueSet":[1,2]},"versionSupport":true,"codeDescriptionSupport":false,"dataSupport":false}}}}
            """, json);
    }

    [Fact]
    public void InitializedParams_WritesEmptyObject()
    {
        Assert.Equal("{}", JsonSerializer.Serialize(new InitializedParams(), LspJsonContext.Default.InitializedParams));
    }

    [Fact]
    public void DidOpen_Serializes()
    {
        DidOpenTextDocumentParams p = new() { TextDocument = new TextDocumentItem { Uri = "file:///a", LanguageId = "csharp", Version = 1, Text = "x" } };

        JsonAssert.Equal("""{"textDocument":{"uri":"file:///a","languageId":"csharp","version":1,"text":"x"}}""",
            JsonSerializer.Serialize(p, LspJsonContext.Default.DidOpenTextDocumentParams));
    }

    [Fact]
    public void DidChange_Incremental_Serializes()
    {
        DidChangeTextDocumentParams p = new()
        {
            TextDocument = new VersionedTextDocumentIdentifier { Uri = "file:///a", Version = 2 },
            ContentChanges = [new TextDocumentContentChangeEvent { Range = R(0, 1, 0, 2), Text = "y" }],
        };

        JsonAssert.Equal("""{"textDocument":{"uri":"file:///a","version":2},"contentChanges":[{"range":{"start":{"line":0,"character":1},"end":{"line":0,"character":2}},"text":"y"}]}""",
            JsonSerializer.Serialize(p, LspJsonContext.Default.DidChangeTextDocumentParams));
    }

    [Fact]
    public void DidChange_Full_NoRange()
    {
        DidChangeTextDocumentParams p = new()
        {
            TextDocument = new VersionedTextDocumentIdentifier { Uri = "u", Version = 3 },
            ContentChanges = [new TextDocumentContentChangeEvent { Text = "all" }],
        };

        JsonAssert.Equal("""{"textDocument":{"uri":"u","version":3},"contentChanges":[{"text":"all"}]}""",
            JsonSerializer.Serialize(p, LspJsonContext.Default.DidChangeTextDocumentParams));
    }

    [Fact]
    public void DidClose()
    {
        JsonAssert.Equal("""{"textDocument":{"uri":"u"}}""",
            JsonSerializer.Serialize(new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = "u" } }, LspJsonContext.Default.DidCloseTextDocumentParams));
    }

    [Fact]
    public void CompletionParams_WithAndWithoutContext()
    {
        CompletionParams p = new() { TextDocument = new TextDocumentIdentifier { Uri = "u" }, Position = P(1, 2) };
        JsonAssert.Equal("""{"textDocument":{"uri":"u"},"position":{"line":1,"character":2}}""", JsonSerializer.Serialize(p, LspJsonContext.Default.CompletionParams));

        p.Context = new CompletionContextDto { TriggerKind = LspCompletionTriggerKind.TriggerCharacter, TriggerCharacter = "." };
        JsonAssert.Equal("""{"textDocument":{"uri":"u"},"position":{"line":1,"character":2},"context":{"triggerKind":2,"triggerCharacter":"."}}""",
            JsonSerializer.Serialize(p, LspJsonContext.Default.CompletionParams));
    }

    [Fact]
    public void PositionParams()
    {
        TextDocumentPositionParams p = new() { TextDocument = new TextDocumentIdentifier { Uri = "u" }, Position = P(3, 4) };

        JsonAssert.Equal("""{"textDocument":{"uri":"u"},"position":{"line":3,"character":4}}""",
            JsonSerializer.Serialize(p, LspJsonContext.Default.TextDocumentPositionParams));
    }

    [Fact]
    public void CancelParams_IntegerId_RoundTrips()
    {
        CancelParams? back = JsonSerializer.Deserialize("""{"id":5}""", LspJsonContext.Default.CancelParams);

        Assert.Equal(5, back!.Id.GetInt64());
        JsonAssert.Equal("""{"id":5}""", JsonSerializer.Serialize(back, LspJsonContext.Default.CancelParams));
    }

    [Fact]
    public void CancelParams_StringId()
    {
        CancelParams? back = JsonSerializer.Deserialize("""{"id":"abc"}""", LspJsonContext.Default.CancelParams);

        Assert.Equal("abc", back!.Id.GetString());
        JsonAssert.Equal("""{"id":"abc"}""", JsonSerializer.Serialize(back, LspJsonContext.Default.CancelParams));
    }

    [Fact]
    public void CompletionItem_Data_RoundTripsUnchanged()
    {
        const string Data = """{"a":[1,2.5,[3,{"b":null}]],"n":1.0,"e":1e3,"u":"héllo 😀","t":true}""";
        LspCompletionItem? item = JsonSerializer.Deserialize($$"""{"label":"x","data":{{Data}}}""", LspJsonContext.Default.LspCompletionItem);

        string json = JsonSerializer.Serialize(item, LspJsonContext.Default.LspCompletionItem);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonAssert.Equal(Data, doc.RootElement.GetProperty("data").GetRawText());
    }

    [Fact]
    public void CompletionItem_UnknownMembers_Ignored()
    {
        LspCompletionItem? item = JsonSerializer.Deserialize("""{"label":"x","future":{"a":1},"kind":3}""", LspJsonContext.Default.LspCompletionItem);

        Assert.Equal("x", item!.Label);
        Assert.Equal(3, item.Kind);
    }

    [Fact]
    public void Diagnostic_CodeNumberAndString()
    {
        const string Range = """{"range":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}},"message":"m",""";
        LspDiagnostic? n = JsonSerializer.Deserialize(Range + """ "code":12}""", LspJsonContext.Default.LspDiagnostic);
        LspDiagnostic? s = JsonSerializer.Deserialize(Range + """ "code":"CS0103"}""", LspJsonContext.Default.LspDiagnostic);

        Assert.Equal(12, n!.Code!.Value.GetInt32());
        Assert.Equal("CS0103", s!.Code!.Value.GetString());
    }
}
