using System.Text.Json;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.FakeServer;

public class FakeServerFeatureTests
{
    private static async Task<FakeServerHost> OpenAsync(string text, FakeServerOptions? options = null)
    {
        FakeServerHost host = FakeServerHost.StartInProcess(options);
        await host.InitializeAsync();
        host.Open(text);
        await host.BarrierAsync();
        return host;
    }

    private static string CompletionParams(int line, int character, string? trigger = null)
    {
        string position = FakeServerHost.At(line, character);
        string context = trigger is null ? """{"triggerKind":1}""" : $$"""{"triggerKind":2,"triggerCharacter":"{{trigger}}"}""";
        return position[..^1] + $",\"context\":{context}}}";
    }

    private static async Task<JsonElement[]> CompletionItemsAsync(FakeServerHost host, int line, int character, string? trigger = null)
    {
        JsonElement result = (await host.RequestAsync(LspMethods.Completion, CompletionParams(line, character, trigger)))!.Value;
        return [.. result.GetProperty("items").EnumerateArray()];
    }

    private static string Summary(JsonElement diagnostic)
    {
        JsonElement range = diagnostic.GetProperty("range");
        string code = diagnostic.TryGetProperty("code", out JsonElement c) ? c.GetRawText() : "";
        string tags = diagnostic.TryGetProperty("tags", out JsonElement t) ? t.GetRawText() : "";
        return $"{diagnostic.GetProperty("severity").GetInt32()}|{code}|{tags}|{range.GetProperty("start").GetProperty("character").GetInt32()}-{range.GetProperty("end").GetProperty("character").GetInt32()}";
    }

    [Fact]
    public async Task Diagnostics_AllWordKinds()
    {
        await using FakeServerHost host = await OpenAsync("ERROR WARN INFO HINT UNUSED OLD");

        JsonElement published = await host.WaitForDiagnosticsAsync();
        JsonElement[] diagnostics = [.. published.GetProperty("diagnostics").EnumerateArray()];

        Assert.Equal(1, published.GetProperty("version").GetInt32());
        Assert.Equal(
            ["1|\"FAKE001\"||0-5", "2|2||6-10", "3|||11-15", "4|||16-20", "4||[1]|21-27", "4||[2]|28-31"],
            diagnostics.Select(Summary).ToArray());
        Assert.All(diagnostics, d => Assert.Equal("fake", d.GetProperty("source").GetString()));
        Assert.Equal("Found ERROR", diagnostics[0].GetProperty("message").GetString());
        Assert.Equal("Found OLD", diagnostics[5].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Diagnostics_RepublishedOnChange()
    {
        await using FakeServerHost host = await OpenAsync("fine");
        Assert.Equal(0, (await host.WaitForDiagnosticsAsync()).GetProperty("diagnostics").GetArrayLength());

        await host.ChangeAsync(2, FakeServerHost.Full("ERROR"));
        JsonElement second = await host.WaitForDiagnosticsAsync();

        Assert.Equal(1, second.GetProperty("diagnostics").GetArrayLength());
        Assert.Equal(2, second.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Diagnostics_WordBoundaries()
    {
        await using FakeServerHost host = await OpenAsync("ERRORS xERROR error");

        Assert.Equal(0, (await host.WaitForDiagnosticsAsync()).GetProperty("diagnostics").GetArrayLength());
    }

    [Fact]
    public async Task Completion_Words_DistinctSortedExcludingTheWordAtTheCursor()
    {
        await using FakeServerHost host = await OpenAsync("banana Apple banana cherry x ch");

        JsonElement[] items = await CompletionItemsAsync(host, 0, 31);

        Assert.Equal(["Apple", "banana", "cherry", "if", "else", "for", "class"], items.Select(i => i.GetProperty("label").GetString()).ToArray());
        JsonElement apple = items[0];
        Assert.Equal(1, apple.GetProperty("kind").GetInt32());
        Assert.Equal("Apple", apple.GetProperty("insertText").GetString());
        Assert.Equal("Apple", apple.GetProperty("sortText").GetString());
        Assert.False(apple.TryGetProperty("textEdit", out _));
        Assert.False(apple.TryGetProperty("filterText", out _));
    }

    [Fact]
    public async Task Completion_Keywords_AndPreselect()
    {
        await using FakeServerHost host = await OpenAsync("");

        JsonElement[] items = await CompletionItemsAsync(host, 0, 0);

        Assert.Equal(["if", "else", "for", "class"], items.Select(i => i.GetProperty("label").GetString()).ToArray());
        Assert.All(items, i => Assert.Equal(14, i.GetProperty("kind").GetInt32()));
        Assert.Equal([true, false, false, false], items.Select(i => i.TryGetProperty("preselect", out JsonElement p) && p.GetBoolean()).ToArray());
    }

    [Fact]
    public async Task Completion_TriggerDot_MemberItemsFirst_WithInsertReplaceEdit()
    {
        await using FakeServerHost host = await OpenAsync("obj.le");

        JsonElement[] items = await CompletionItemsAsync(host, 0, 4, ".");

        Assert.Equal(["Length", "Count", "ToString", "Equals"], items.Take(4).Select(i => i.GetProperty("label").GetString()).ToArray());
        Assert.Equal([10, 10, 2, 2], items.Take(4).Select(i => i.GetProperty("kind").GetInt32()).ToArray());
        Assert.False(items[0].TryGetProperty("commitCharacters", out _));
        Assert.Equal(["("], items[2].GetProperty("commitCharacters").EnumerateArray().Select(c => c.GetString()).ToArray());
        JsonElement edit = items[2].GetProperty("textEdit");
        Assert.Equal("ToString", edit.GetProperty("newText").GetString());
        Assert.Equal(4, edit.GetProperty("insert").GetProperty("start").GetProperty("character").GetInt32());
        Assert.Equal(4, edit.GetProperty("insert").GetProperty("end").GetProperty("character").GetInt32());
        Assert.Equal(6, edit.GetProperty("replace").GetProperty("end").GetProperty("character").GetInt32());
        Assert.Contains(items, i => i.GetProperty("label").GetString() == "obj");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completion_Incomplete_Flag(bool incomplete)
    {
        await using FakeServerHost host = await OpenAsync("abc", new FakeServerOptions { Incomplete = incomplete });

        JsonElement result = (await host.RequestAsync(LspMethods.Completion, CompletionParams(0, 0)))!.Value;

        Assert.Equal(incomplete, result.GetProperty("isIncomplete").GetBoolean());
    }

    [Fact]
    public async Task Completion_ItemData_Present()
    {
        await using FakeServerHost host = await OpenAsync("abc");

        JsonElement[] items = await CompletionItemsAsync(host, 0, 0);

        Assert.All(items, i => Assert.Equal($$"""{"label":"{{i.GetProperty("label").GetString()}}","n":1}""", i.GetProperty("data").GetRawText()));
    }

    [Fact]
    public async Task Resolve_AddsDetailAndDocumentation()
    {
        await using FakeServerHost host = await OpenAsync("");

        JsonElement item = (await host.RequestAsync(LspMethods.CompletionResolve, """{"label":"foo","data":{"label":"foo","n":1}}"""))!.Value;

        Assert.Equal("detail of foo", item.GetProperty("detail").GetString());
        Assert.Equal("markdown", item.GetProperty("documentation").GetProperty("kind").GetString());
        Assert.Equal("**foo** documentation", item.GetProperty("documentation").GetProperty("value").GetString());
        Assert.False(item.TryGetProperty("additionalTextEdits", out _));
    }

    [Fact]
    public async Task Resolve_Length_AddsAdditionalTextEdit()
    {
        await using FakeServerHost host = await OpenAsync("");

        JsonElement item = (await host.RequestAsync(LspMethods.CompletionResolve, """{"label":"Length"}"""))!.Value;

        JsonElement edit = item.GetProperty("additionalTextEdits")[0];
        Assert.Equal("// resolved\n", edit.GetProperty("newText").GetString());
        Assert.Equal(0, edit.GetProperty("range").GetProperty("end").GetProperty("line").GetInt32());
    }

    [Fact]
    public async Task Resolve_DataUnchanged()
    {
        await using FakeServerHost host = await OpenAsync("");

        JsonElement item = (await host.RequestAsync(LspMethods.CompletionResolve, """{"label":"foo","data":{"k":[1,"two",{"x":null}]}}"""))!.Value;

        Assert.Equal("""{"k":[1,"two",{"x":null}]}""", item.GetProperty("data").GetRawText());
    }

    [Fact]
    public async Task Hover_Word()
    {
        await using FakeServerHost host = await OpenAsync("hello world");

        JsonElement hover = (await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 2)))!.Value;

        Assert.Equal("markdown", hover.GetProperty("contents").GetProperty("kind").GetString());
        Assert.Equal("```fake\nhello\n```\nHover for **hello**", hover.GetProperty("contents").GetProperty("value").GetString());
        Assert.Equal(0, hover.GetProperty("range").GetProperty("start").GetProperty("character").GetInt32());
        Assert.Equal(5, hover.GetProperty("range").GetProperty("end").GetProperty("character").GetInt32());
    }

    [Fact]
    public async Task Hover_NoWord_Null()
    {
        await using FakeServerHost host = await OpenAsync("a  b");

        JsonElement? hover = await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 2));

        Assert.Null(hover);
    }

    [Fact]
    public async Task Hover_PositionAtWordEnd_StillInside()
    {
        await using FakeServerHost host = await OpenAsync("hello world");

        JsonElement hover = (await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 5)))!.Value;

        Assert.Contains("Hover for **hello**", hover.GetProperty("contents").GetProperty("value").GetString());
    }

    [Fact]
    public async Task Definition_Def_Location()
    {
        await using FakeServerHost host = await OpenAsync("def foo():\nfoo()");

        JsonElement location = (await host.RequestAsync(LspMethods.Definition, FakeServerHost.At(1, 1)))!.Value;

        Assert.Equal("file:///t.txt", location.GetProperty("uri").GetString());
        JsonElement range = location.GetProperty("range");
        Assert.Equal(0, range.GetProperty("start").GetProperty("line").GetInt32());
        Assert.Equal(4, range.GetProperty("start").GetProperty("character").GetInt32());
        Assert.Equal(7, range.GetProperty("end").GetProperty("character").GetInt32());
    }

    [Fact]
    public async Task Definition_Class_LocationLinkArray()
    {
        await using FakeServerHost host = await OpenAsync("class Foo\nnew Foo");

        JsonElement result = (await host.RequestAsync(LspMethods.Definition, FakeServerHost.At(1, 5)))!.Value;

        Assert.Equal(1, result.GetArrayLength());
        JsonElement link = result[0];
        Assert.Equal("file:///t.txt", link.GetProperty("targetUri").GetString());
        Assert.Equal("(1,4)-(1,7)", Format(link.GetProperty("originSelectionRange")));
        Assert.Equal("(0,0)-(0,9)", Format(link.GetProperty("targetRange")));
        Assert.Equal("(0,6)-(0,9)", Format(link.GetProperty("targetSelectionRange")));
    }

    [Fact]
    public async Task Definition_External_OtherUri()
    {
        await using FakeServerHost host = await OpenAsync("external");

        JsonElement location = (await host.RequestAsync(LspMethods.Definition, FakeServerHost.At(0, 2)))!.Value;

        Assert.Equal("file:///other/file.txt", location.GetProperty("uri").GetString());
        Assert.Equal("(0,0)-(0,8)", Format(location.GetProperty("range")));
    }

    [Fact]
    public async Task Definition_NotFound_Null()
    {
        await using FakeServerHost host = await OpenAsync("bar");

        JsonElement? result = await host.RequestAsync(LspMethods.Definition, FakeServerHost.At(0, 1));

        Assert.Null(result);
    }

    [Fact]
    public async Task Definition_NoWord_Null()
    {
        await using FakeServerHost host = await OpenAsync("  ");

        JsonElement? result = await host.RequestAsync(LspMethods.Definition, FakeServerHost.At(0, 1));

        Assert.Null(result);
    }

    private static string Format(JsonElement range)
    {
        JsonElement start = range.GetProperty("start");
        JsonElement end = range.GetProperty("end");
        return $"({start.GetProperty("line").GetInt32()},{start.GetProperty("character").GetInt32()})-({end.GetProperty("line").GetInt32()},{end.GetProperty("character").GetInt32()})";
    }
}
