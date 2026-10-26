using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.Protocol;

public class ServerPayloadTests
{
    private const string Rng = """{"start":{"line":1,"character":2},"end":{"line":1,"character":5}}""";

    [Fact]
    public void InitializeResult_TypicalServer()
    {
        InitializeResult? r = JsonSerializer.Deserialize("""
            {"capabilities":{"textDocumentSync":2,"completionProvider":{"triggerCharacters":["."],"resolveProvider":true},
            "hoverProvider":true,"definitionProvider":true,"renameProvider":{"prepareProvider":true},"foo":1},
            "serverInfo":{"name":"csharp-ls","version":"0.1"}}
            """, LspJsonContext.Default.InitializeResult);

        ServerCapabilities c = r!.Capabilities;
        Assert.Equal("csharp-ls", r.ServerInfo!.Name);
        Assert.Equal(2, c.TextDocumentSync!.Value.GetInt32());
        Assert.Equal(["."], c.CompletionProvider!.TriggerCharacters!);
        Assert.True(c.CompletionProvider.ResolveProvider);
        Assert.True(LspUnions.IsEnabled(c.HoverProvider));
        Assert.True(LspUnions.IsEnabled(c.DefinitionProvider));
    }

    [Fact]
    public void InitializeResult_OptionObjects()
    {
        InitializeResult? r = JsonSerializer.Deserialize("""{"capabilities":{"textDocumentSync":{"openClose":true,"change":1},"hoverProvider":{}}}""", LspJsonContext.Default.InitializeResult);

        Assert.Equal((true, 1), LspUnions.ParseTextDocumentSync(r!.Capabilities.TextDocumentSync));
        Assert.True(LspUnions.IsEnabled(r.Capabilities.HoverProvider));
        Assert.False(LspUnions.IsEnabled(r.Capabilities.DefinitionProvider));
    }

    [Fact]
    public void PublishDiagnostics_WithVersion()
    {
        PublishDiagnosticsParams? p = JsonSerializer.Deserialize($$"""{"uri":"file:///a","version":7,"diagnostics":[{"range":{{Rng}},"severity":1,"source":"cs","message":"bad"}]}""", LspJsonContext.Default.PublishDiagnosticsParams);

        Assert.Equal(7, p!.Version);
        Assert.Equal("file:///a", p.Uri);
        LspDiagnostic d = Assert.Single(p.Diagnostics);
        Assert.Equal(1, d.Severity);
        Assert.Equal("cs", d.Source);
        Assert.Equal("bad", d.Message);
        Assert.Equal(2, d.Range.Start.Character);
        Assert.Equal(5, d.Range.End.Character);
    }

    [Fact]
    public void PublishDiagnostics_WithoutVersion()
    {
        PublishDiagnosticsParams? p = JsonSerializer.Deserialize("""{"uri":"u","diagnostics":[]}""", LspJsonContext.Default.PublishDiagnosticsParams);

        Assert.Null(p!.Version);
        Assert.Empty(p.Diagnostics);
    }

    [Fact]
    public void PublishDiagnostics_CodeAsNumber()
    {
        PublishDiagnosticsParams? p = JsonSerializer.Deserialize($$"""{"uri":"u","diagnostics":[{"range":{{Rng}},"code":161,"message":"m"}]}""", LspJsonContext.Default.PublishDiagnosticsParams);

        Assert.Equal(161, p!.Diagnostics[0].Code!.Value.GetInt32());
        Assert.Null(p.Diagnostics[0].Severity);
    }

    [Fact]
    public void PublishDiagnostics_Tags()
    {
        PublishDiagnosticsParams? p = JsonSerializer.Deserialize($$"""{"uri":"u","diagnostics":[{"range":{{Rng}},"message":"m","tags":[1,2]}]}""", LspJsonContext.Default.PublishDiagnosticsParams);

        Assert.Equal([LspDiagnosticTag.Unnecessary, LspDiagnosticTag.Deprecated], p!.Diagnostics[0].Tags!);
    }

    [Fact]
    public void CompletionList_InsertReplaceEdit()
    {
        LspCompletionList? l = JsonSerializer.Deserialize("""{"isIncomplete":true,"items":[{"label":"foo","textEdit":{"newText":"foo","insert":RNG,"replace":RNG }}]}""".Replace("RNG", Rng), LspJsonContext.Default.LspCompletionList);

        Assert.True(l!.IsIncomplete);
        (LspRange? ins, LspRange? rep, string? text) = LspUnions.ParseTextEdit(l.Items[0].TextEdit);
        Assert.Equal("foo", text);
        Assert.Equal(2, ins!.Start.Character);
        Assert.Equal(5, rep!.End.Character);
    }

    [Fact]
    public void CompletionList_TextEdit()
    {
        LspCompletionList? l = JsonSerializer.Deserialize("""{"isIncomplete":false,"items":[{"label":"foo","kind":3,"insertTextFormat":1,"textEdit":{"range":RNG,"newText":"bar"}}]}""".Replace("RNG", Rng), LspJsonContext.Default.LspCompletionList);

        Assert.False(l!.IsIncomplete);
        Assert.Equal(LspCompletionItemKind.Function, l.Items[0].Kind);
        (LspRange? ins, LspRange? rep, string? text) = LspUnions.ParseTextEdit(l.Items[0].TextEdit);
        Assert.Equal("bar", text);
        Assert.Same(ins, rep);
    }

    [Fact]
    public void CompletionItem_LabelDetails()
    {
        LspCompletionItem? i = JsonSerializer.Deserialize("""{"label":"M","labelDetails":{"detail":"(int)","description":"System"},"sortText":"a","filterText":"m","commitCharacters":["("],"tags":[1],"preselect":true}""", LspJsonContext.Default.LspCompletionItem);

        Assert.Equal("(int)", i!.LabelDetails!.Detail);
        Assert.Equal("System", i.LabelDetails.Description);
        Assert.Equal("a", i.SortText);
        Assert.Equal("m", i.FilterText);
        Assert.Equal(["("], i.CommitCharacters!);
        Assert.Equal([1], i.Tags!);
        Assert.True(i.Preselect);
    }

    [Fact]
    public void CompletionItem_DocumentationString()
    {
        LspCompletionItem? i = JsonSerializer.Deserialize("""{"label":"x","documentation":"plain"}""", LspJsonContext.Default.LspCompletionItem);

        Assert.Equal("plain", LspUnions.ParseDocumentation(i!.Documentation, out bool md));
        Assert.False(md);
    }

    [Fact]
    public void CompletionItem_DocumentationMarkupContent()
    {
        LspCompletionItem? i = JsonSerializer.Deserialize("""{"label":"x","documentation":{"kind":"markdown","value":"**b**"}}""", LspJsonContext.Default.LspCompletionItem);

        Assert.Equal("**b**", LspUnions.ParseDocumentation(i!.Documentation, out bool md));
        Assert.True(md);
    }

    [Fact]
    public void Hover_MarkupContent()
    {
        LspHover? h = JsonSerializer.Deserialize($$"""{"contents":{"kind":"markdown","value":"text"},"range":{{Rng}} }""", LspJsonContext.Default.LspHover);

        Assert.Equal(new HoverContentPart("text", true), Assert.Single(LspUnions.ParseHoverContents(h!.Contents)));
        Assert.Equal(1, h.Range!.Start.Line);
    }

    [Fact]
    public void Hover_MarkedStringArray()
    {
        LspHover? h = JsonSerializer.Deserialize("""{"contents":["one",{"language":"csharp","value":"int x;"}]}""", LspJsonContext.Default.LspHover);

        IReadOnlyList<HoverContentPart> parts = LspUnions.ParseHoverContents(h!.Contents);
        Assert.Equal(2, parts.Count);
        Assert.Equal("one", parts[0].Value);
        Assert.Null(h.Range);
    }

    [Fact]
    public void LogMessage()
    {
        LogMessageParams? m = JsonSerializer.Deserialize("""{"type":3,"message":"hello"}""", LspJsonContext.Default.LogMessageParams);

        Assert.Equal(LspMessageType.Info, m!.Type);
        Assert.Equal("hello", m.Message);
    }
}
