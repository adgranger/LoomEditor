using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Lsp.Editor;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspConvertHoverTests
{
    private static Hover? Convert(string json) => LspConvert.ToHover(JsonElement.Parse(json));

    [Fact]
    public void Hover_Null()
    {
        Assert.Null(LspConvert.ToHover(null));
        Assert.Null(Convert("null"));
        Assert.Null(Convert("{}"));
    }

    [Fact]
    public void Hover_MarkupContent_Markdown()
        => Assert.Equal("**b**", Convert("{\"contents\":{\"kind\":\"markdown\",\"value\":\"**b**\"}}")!.Contents.Single().Value);

    [Fact]
    public void Hover_MarkupContent_PlainText_Escaped()
        => Assert.Equal(MarkdownString.FromText("a*b").Value, Convert("{\"contents\":{\"kind\":\"plaintext\",\"value\":\"a*b\"}}")!.Contents.Single().Value);

    [Fact]
    public void Hover_MarkedStringArray_Mixed()
    {
        Hover h = Convert("{\"contents\":[\"one\",{\"kind\":\"plaintext\",\"value\":\"t*\"}]}")!;

        Assert.Equal(["one", MarkdownString.FromText("t*").Value], h.Contents.Select(c => c.Value));
    }

    [Fact]
    public void Hover_FencedCode_FromLanguageValue()
        => Assert.Equal("```cs\nint x;\n```", Convert("{\"contents\":{\"language\":\"cs\",\"value\":\"int x;\"}}")!.Contents.Single().Value);

    [Fact]
    public void Hover_EmptyContents_Null()
    {
        Assert.Null(Convert("{\"contents\":\"\"}"));
        Assert.Null(Convert("{\"contents\":[]}"));
    }

    [Fact]
    public void Hover_Range_Converted_NotValidated()
        => Assert.Equal(new Range(1, 3, 100, 201), Convert("{\"contents\":\"x\",\"range\":{\"start\":{\"line\":0,\"character\":2},\"end\":{\"line\":99,\"character\":200}}}")!.Range);

    [Fact]
    public void Hover_InvalidRange_NullRange()
        => Assert.Null(Convert("{\"contents\":\"x\",\"range\":{\"start\":1}}")!.Range);
}
