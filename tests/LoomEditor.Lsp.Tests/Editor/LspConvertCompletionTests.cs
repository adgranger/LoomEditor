using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspConvertCompletionTests
{
    private const string Rng = "{\"start\":{\"line\":0,\"character\":1},\"end\":{\"line\":0,\"character\":3}}";

    private static LspCompletionItem Lsp(string json) => JsonSerializer.Deserialize(json, LspJsonContext.Default.LspCompletionItem)!;

    private static CompletionItem Item(string json) => LspConvert.ToCompletionItem(Lsp(json));

    private static JsonElement Json(string json) => JsonElement.Parse(json);

    public static TheoryData<int, CompletionItemKind> KindData => new()
    {
        { 1, CompletionItemKind.Text }, { 2, CompletionItemKind.Method }, { 3, CompletionItemKind.Function },
        { 4, CompletionItemKind.Constructor }, { 5, CompletionItemKind.Field }, { 6, CompletionItemKind.Variable },
        { 7, CompletionItemKind.Class }, { 8, CompletionItemKind.Interface }, { 9, CompletionItemKind.Module },
        { 10, CompletionItemKind.Property }, { 11, CompletionItemKind.Unit }, { 12, CompletionItemKind.Value },
        { 13, CompletionItemKind.Enum }, { 14, CompletionItemKind.Keyword }, { 15, CompletionItemKind.Snippet },
        { 16, CompletionItemKind.Color }, { 17, CompletionItemKind.File }, { 18, CompletionItemKind.Reference },
        { 19, CompletionItemKind.Folder }, { 20, CompletionItemKind.EnumMember }, { 21, CompletionItemKind.Constant },
        { 22, CompletionItemKind.Struct }, { 23, CompletionItemKind.Event }, { 24, CompletionItemKind.Operator },
        { 25, CompletionItemKind.TypeParameter },
    };

    [Theory]
    [MemberData(nameof(KindData))]
    public void Kind_AllLspKinds(int lsp, CompletionItemKind expected) => Assert.Equal(expected, LspConvert.ToCompletionItemKind(lsp));

    [Fact]
    public void Kind_NullAndUnknown_Text()
    {
        Assert.Equal(CompletionItemKind.Text, LspConvert.ToCompletionItemKind(null));
        Assert.Equal(CompletionItemKind.Text, LspConvert.ToCompletionItemKind(0));
        Assert.Equal(CompletionItemKind.Text, LspConvert.ToCompletionItemKind(26));
    }

    [Fact]
    public void Item_Basic()
    {
        CompletionItem i = Item("{\"label\":\"foo\"}");

        Assert.Equal(("foo", "foo", null), (i.Label, i.InsertText, i.Range));
    }

    [Fact]
    public void Item_InsertText() => Assert.Equal("bar", Item("{\"label\":\"foo\",\"insertText\":\"bar\"}").InsertText);

    [Fact]
    public void Item_TextEdit_SingleRange()
    {
        CompletionItem i = Item("{\"label\":\"foo\",\"insertText\":\"ignored\",\"textEdit\":{\"range\":" + Rng + ",\"newText\":\"fooX\"}}");

        Assert.Equal("fooX", i.InsertText);
        Assert.Equal(new CompletionRange(new Range(1, 2, 1, 4), new Range(1, 2, 1, 4)), i.Range);
    }

    [Fact]
    public void Item_TextEdit_InsertReplace()
    {
        CompletionItem i = Item("{\"label\":\"f\",\"textEdit\":{\"insert\":" + Rng + ",\"replace\":{\"start\":{\"line\":0,\"character\":1},\"end\":{\"line\":0,\"character\":6}},\"newText\":\"n\"}}");

        Assert.Equal(new CompletionRange(new Range(1, 2, 1, 4), new Range(1, 2, 1, 7)), i.Range);
    }

    [Fact]
    public void Item_TextEdit_RangeNotValidated()
    {
        CompletionItem i = Item("{\"label\":\"f\",\"textEdit\":{\"range\":{\"start\":{\"line\":500,\"character\":900},\"end\":{\"line\":500,\"character\":901}},\"newText\":\"n\"}}");

        Assert.Equal(new Range(501, 901, 501, 902), i.Range!.Value.Insert);
    }

    [Fact]
    public void ToCompletionItem_SnippetFormat_SetsInsertAsSnippetAndKeepsRawText()
    {
        CompletionItem item = Item("{\"label\":\"f\",\"insertText\":\"f(${1:x})$0\",\"insertTextFormat\":2}");

        Assert.Equal("f(${1:x})$0", item.InsertText);
        Assert.Equal(InsertTextRules.InsertAsSnippet, item.InsertTextRules);
        Assert.Equal(InsertTextRules.None, Item("{\"label\":\"f\",\"insertText\":\"f\",\"insertTextFormat\":1}").InsertTextRules);
    }

    [Fact]
    public void Item_Tags_DeprecatedByTagOrFlag()
    {
        Assert.Equal([CompletionItemTag.Deprecated], Item("{\"label\":\"f\",\"tags\":[1]}").Tags);
        Assert.Equal([CompletionItemTag.Deprecated], Item("{\"label\":\"f\",\"deprecated\":true}").Tags);
        Assert.Null(Item("{\"label\":\"f\"}").Tags);
    }

    [Fact]
    public void Item_Documentation_Markdown()
        => Assert.Equal("**d**", Item("{\"label\":\"f\",\"documentation\":{\"kind\":\"markdown\",\"value\":\"**d**\"}}").Documentation!.Value);

    [Fact]
    public void Item_Documentation_PlainText_Escaped()
        => Assert.Equal(MarkdownString.FromText("a*b").Value, Item("{\"label\":\"f\",\"documentation\":\"a*b\"}").Documentation!.Value);

    [Fact]
    public void Item_Documentation_Empty_Null()
    {
        Assert.Null(Item("{\"label\":\"f\",\"documentation\":\"\"}").Documentation);
        Assert.Null(Item("{\"label\":\"f\"}").Documentation);
    }

    [Fact]
    public void Item_CommitCharacters_FirstCharOfEach()
    {
        Assert.Equal(['.', '('], Item("{\"label\":\"f\",\"commitCharacters\":[\".\",\"\",\"(x\"]}").CommitCharacters);
        Assert.Null(Item("{\"label\":\"f\",\"commitCharacters\":[]}").CommitCharacters);
    }

    [Fact]
    public void Item_AdditionalTextEdits_Converted()
    {
        CompletionItem i = Item("{\"label\":\"f\",\"additionalTextEdits\":[{\"range\":" + Rng + ",\"newText\":\"using X;\"}]}");

        Assert.Equal([new TextEdit(new Range(1, 2, 1, 4), "using X;")], i.AdditionalTextEdits);
        Assert.Null(Item("{\"label\":\"f\",\"additionalTextEdits\":[]}").AdditionalTextEdits);
    }

    [Fact]
    public void Item_LabelDetails()
    {
        CompletionItem i = Item("{\"label\":\"f\",\"labelDetails\":{\"detail\":\"(int)\",\"description\":\"Ns\"}}");

        Assert.Equal(("(int)", "Ns"), (i.LabelDetail, i.LabelDescription));
    }

    [Fact]
    public void Item_Preselect_SortText_FilterText_Detail()
    {
        CompletionItem i = Item("{\"label\":\"f\",\"preselect\":true,\"sortText\":\"s\",\"filterText\":\"ft\",\"detail\":\"d\"}");

        Assert.Equal((true, "s", "ft", "d"), (i.Preselect, i.SortText, i.FilterText, i.Detail));
    }

    [Fact]
    public void Item_ProviderData_IsTheRawItem()
    {
        LspCompletionItem raw = Lsp("{\"label\":\"f\",\"data\":{\"id\":1}}");

        Assert.Same(raw, LspConvert.ToCompletionItem(raw).ProviderData);
    }

    [Fact]
    public void List_Array_NotIncomplete()
    {
        CompletionList l = LspConvert.ToCompletionList(Json("[{\"label\":\"a\"},{\"label\":\"b\"}]"));

        Assert.Equal((2, false), (l.Suggestions.Count, l.Incomplete));
    }

    [Fact]
    public void List_Object_Incomplete()
    {
        CompletionList l = LspConvert.ToCompletionList(Json("{\"isIncomplete\":true,\"items\":[{\"label\":\"a\"}]}"));

        Assert.Equal((1, true), (l.Suggestions.Count, l.Incomplete));
    }

    [Fact]
    public void List_Null_Empty() => Assert.Empty(LspConvert.ToCompletionList(null).Suggestions);

    [Fact]
    public async Task NoModelParameters_ConversionsRunOffTheUiThread()
    {
        JsonElement list = Json("[{\"label\":\"a\"}]");
        JsonElement hover = Json("{\"contents\":\"x\"}");
        JsonElement loc = Json("{\"uri\":\"u\",\"range\":" + Rng + "}");

        (int, bool, int) result = await Task.Run(() =>
            (LspConvert.ToCompletionList(list).Suggestions.Count, LspConvert.ToHover(hover) is not null, LspConvert.ToLocationLinks(loc).Count));

        Assert.Equal((1, true, 1), result);
    }

    [Fact]
    public void ApplyResolved_CarriesResolvedMembers()
    {
        LspCompletionItem resolved = Lsp("{\"label\":\"f\",\"detail\":\"resolved\",\"documentation\":\"doc\"}");

        CompletionItem i = LspConvert.ApplyResolved(resolved);

        Assert.Equal(("resolved", true), (i.Detail, i.Documentation is not null));
        Assert.Same(resolved, i.ProviderData);
    }

    [Fact]
    public void TriggerKind_Mapping()
    {
        Assert.Equal(1, LspConvert.ToLspTriggerKind(CompletionTriggerKind.Invoke));
        Assert.Equal(2, LspConvert.ToLspTriggerKind(CompletionTriggerKind.TriggerCharacter));
        Assert.Equal(3, LspConvert.ToLspTriggerKind(CompletionTriggerKind.TriggerForIncompleteCompletions));
    }

    [Fact]
    public void Context_Mapping()
    {
        CompletionContextDto c = LspConvert.ToLspContext(new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."));

        Assert.Equal((2, "."), (c.TriggerKind, c.TriggerCharacter));
    }
}
