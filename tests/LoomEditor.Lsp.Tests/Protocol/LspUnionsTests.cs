using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.Protocol;

public class LspUnionsTests
{
    private static JsonElement J(string json) => JsonElement.Parse(json);

    private const string Rng = """{"start":{"line":1,"character":2},"end":{"line":3,"character":4}}""";

    [Fact]
    public void ParseTextDocumentSync_Number() => Assert.Equal((true, 2), LspUnions.ParseTextDocumentSync(J("2")));

    [Fact]
    public void ParseTextDocumentSync_Object()
    {
        Assert.Equal((true, 1), LspUnions.ParseTextDocumentSync(J("""{"openClose":true,"change":1}""")));
        Assert.Equal((false, 0), LspUnions.ParseTextDocumentSync(J("{}")));
    }

    [Fact]
    public void ParseTextDocumentSync_Null()
    {
        Assert.Equal((false, 0), LspUnions.ParseTextDocumentSync(null));
        Assert.Equal((false, 0), LspUnions.ParseTextDocumentSync(J("null")));
    }

    [Fact]
    public void ParseTextDocumentSync_Garbage()
    {
        Assert.Equal((false, 0), LspUnions.ParseTextDocumentSync(J("\"x\"")));
        Assert.Equal((false, 0), LspUnions.ParseTextDocumentSync(J("""{"openClose":"yes","change":"no"}""")));
    }

    [Fact]
    public void IsEnabled_TrueObjectFalseAbsent()
    {
        Assert.True(LspUnions.IsEnabled(J("true")));
        Assert.True(LspUnions.IsEnabled(J("{}")));
        Assert.False(LspUnions.IsEnabled(J("false")));
        Assert.False(LspUnions.IsEnabled(null));
        Assert.False(LspUnions.IsEnabled(J("1")));
    }

    [Fact]
    public void ParseCompletionResult_Array()
    {
        IReadOnlyList<LspCompletionItem> items = LspUnions.ParseCompletionResult(J("""[{"label":"a"},{"label":"b"}]"""), out bool inc);

        Assert.Equal(["a", "b"], items.Select(i => i.Label));
        Assert.False(inc);
    }

    [Fact]
    public void ParseCompletionResult_List()
    {
        IReadOnlyList<LspCompletionItem> items = LspUnions.ParseCompletionResult(J("""{"isIncomplete":false,"items":[{"label":"a"}]}"""), out bool inc);

        Assert.Single(items);
        Assert.False(inc);
    }

    [Fact]
    public void ParseCompletionResult_ListIncomplete()
    {
        LspUnions.ParseCompletionResult(J("""{"isIncomplete":true,"items":[]}"""), out bool inc);

        Assert.True(inc);
    }

    [Fact]
    public void ParseCompletionResult_Null()
    {
        Assert.Empty(LspUnions.ParseCompletionResult(null, out bool a));
        Assert.Empty(LspUnions.ParseCompletionResult(J("null"), out bool b));
        Assert.False(a || b);
    }

    [Fact]
    public void ParseCompletionResult_MalformedItemSkipped()
    {
        IReadOnlyList<LspCompletionItem> items = LspUnions.ParseCompletionResult(J("""[{"label":"a"},{"kind":"x"},5,{"label":"c"}]"""), out _);

        Assert.Equal(["a", "c"], items.Select(i => i.Label));
    }

    [Fact]
    public void ParseHoverContents_String()
    {
        Assert.Equal(new HoverContentPart("hi", true), Assert.Single(LspUnions.ParseHoverContents(J("\"hi\""))));
    }

    [Fact]
    public void ParseHoverContents_MarkedStringObject()
    {
        HoverContentPart p = Assert.Single(LspUnions.ParseHoverContents(J("""{"language":"csharp","value":"int x;"}""")));

        Assert.Equal("```csharp" + (char)10 + "int x;" + (char)10 + "```", p.Value);
        Assert.True(p.IsMarkdown);
    }

    [Fact]
    public void ParseHoverContents_MarkupContentMarkdown()
    {
        Assert.Equal(new HoverContentPart("**b**", true), Assert.Single(LspUnions.ParseHoverContents(J("""{"kind":"markdown","value":"**b**"}"""))));
    }

    [Fact]
    public void ParseHoverContents_MarkupContentPlainText()
    {
        Assert.Equal(new HoverContentPart("t", false), Assert.Single(LspUnions.ParseHoverContents(J("""{"kind":"plaintext","value":"t"}"""))));
    }

    [Fact]
    public void ParseHoverContents_Array()
    {
        IReadOnlyList<HoverContentPart> parts = LspUnions.ParseHoverContents(J("""["a",{"kind":"plaintext","value":"b"},{"language":"x","value":"c"}]"""));

        Assert.Equal(3, parts.Count);
        Assert.Equal("a", parts[0].Value);
        Assert.False(parts[1].IsMarkdown);
    }

    [Fact]
    public void ParseHoverContents_EmptyDropped()
    {
        Assert.Empty(LspUnions.ParseHoverContents(J("""["",{"kind":"markdown","value":""}]""")));
    }

    [Fact]
    public void ParseHoverContents_Garbage()
    {
        Assert.Empty(LspUnions.ParseHoverContents(J("5")));
        Assert.Empty(LspUnions.ParseHoverContents(J("null")));
        Assert.Empty(LspUnions.ParseHoverContents(J("""{"value":1}""")));
    }

    [Fact]
    public void ParseDocumentation_String_Markup_Null()
    {
        Assert.Equal("s", LspUnions.ParseDocumentation(J("\"s\""), out bool a));
        Assert.False(a);
        Assert.Equal("m", LspUnions.ParseDocumentation(J("""{"kind":"markdown","value":"m"}"""), out bool b));
        Assert.True(b);
        Assert.Null(LspUnions.ParseDocumentation(null, out _));
        Assert.Null(LspUnions.ParseDocumentation(J("3"), out _));
    }

    [Fact]
    public void ParseTextEdit_Range()
    {
        (LspRange? ins, LspRange? rep, string? text) = LspUnions.ParseTextEdit(J($$"""{"range":{{Rng}},"newText":"n"}"""));

        Assert.Equal("n", text);
        Assert.Same(ins, rep);
        Assert.Equal(3, ins!.End.Line);
    }

    [Fact]
    public void ParseTextEdit_InsertReplace()
    {
        (LspRange? ins, LspRange? rep, string? text) = LspUnions.ParseTextEdit(J("""{"newText":"n","insert":RNG,"replace":{"start":{"line":0,"character":0},"end":{"line":3,"character":9}}}""".Replace("RNG", Rng)));

        Assert.Equal("n", text);
        Assert.Equal(4, ins!.End.Character);
        Assert.Equal(9, rep!.End.Character);
    }

    [Fact]
    public void ParseTextEdit_Null()
    {
        Assert.Equal((null, null, null), LspUnions.ParseTextEdit(null));
        Assert.Equal((null, null, null), LspUnions.ParseTextEdit(J("""{"newText":"n"}""")));
    }

    [Fact]
    public void ParseDefinitionResult_Location()
    {
        LspLocationLink l = Assert.Single(LspUnions.ParseDefinitionResult(J($$"""{"uri":"file:///a","range":{{Rng}} }""")));

        Assert.Equal("file:///a", l.TargetUri);
        Assert.Same(l.TargetRange, l.TargetSelectionRange);
        Assert.Null(l.OriginSelectionRange);
    }

    [Fact]
    public void ParseDefinitionResult_LocationArray()
    {
        IReadOnlyList<LspLocationLink> links = LspUnions.ParseDefinitionResult(J($$"""[{"uri":"a","range":{{Rng}} },{"uri":"b","range":{{Rng}} }]"""));

        Assert.Equal(["a", "b"], links.Select(l => l.TargetUri));
    }

    [Fact]
    public void ParseDefinitionResult_LocationLinkArray()
    {
        LspLocationLink l = Assert.Single(LspUnions.ParseDefinitionResult(J("""[{"originSelectionRange":RNG,"targetUri":"t","targetRange":RNG,"targetSelectionRange":{"start":{"line":0,"character":0},"end":{"line":0,"character":1}}}]""".Replace("RNG", Rng))));

        Assert.Equal("t", l.TargetUri);
        Assert.NotNull(l.OriginSelectionRange);
        Assert.Equal(1, l.TargetSelectionRange.End.Character);
    }

    [Fact]
    public void ParseDefinitionResult_Mixed()
    {
        IReadOnlyList<LspLocationLink> links = LspUnions.ParseDefinitionResult(J($$"""[{"uri":"a","range":{{Rng}} },{"targetUri":"b","targetRange":{{Rng}},"targetSelectionRange":{{Rng}} }]"""));

        Assert.Equal(["a", "b"], links.Select(l => l.TargetUri));
    }

    [Fact]
    public void ParseDefinitionResult_Null()
    {
        Assert.Empty(LspUnions.ParseDefinitionResult(null));
        Assert.Empty(LspUnions.ParseDefinitionResult(J("null")));
    }

    [Fact]
    public void ParseDefinitionResult_MalformedSkipped()
    {
        IReadOnlyList<LspLocationLink> links = LspUnions.ParseDefinitionResult(J($$"""[5,{"uri":"x"},{"targetUri":"y"},{"uri":"ok","range":{{Rng}} }]"""));

        Assert.Equal("ok", Assert.Single(links).TargetUri);
    }
}
