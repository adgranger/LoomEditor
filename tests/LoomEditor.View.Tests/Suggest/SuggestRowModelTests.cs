using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestRowModelTests
{
    private static SuggestItem Make(CompletionItem completion)
    {
        completion.Range = CompletionRange.FromRange(new Range(1, 1, 1, 3));
        return new SuggestItem(new Position(1, 3), completion, new CompletionList([completion]), new FakeCompletionProvider());
    }

    private static SuggestItem Make(string label = "foo") => Make(new CompletionItem(label, label, CompletionItemKind.Method));

    [Fact]
    public void Create_NoWord_NoMatches() => Assert.Empty(SuggestRowModel.Create(Make(), false).Matches);

    [Fact]
    public void Create_WithWord_HasMatches()
    {
        SuggestItem item = Make();
        item.Score = Fuzzy.Score("fo", "fo", 0, "foo", "foo", 0)!;
        var matches = SuggestRowModel.Create(item, false).Matches;
        Assert.Equal([new FilterMatch(0, 2)], matches);
    }

    [Fact]
    public void Create_Deprecated()
    {
        SuggestItem item = Make(new CompletionItem("foo", "foo") { Tags = [CompletionItemTag.Deprecated] });
        Assert.True(SuggestRowModel.Create(item, false).IsDeprecated);
        Assert.False(SuggestRowModel.Create(Make(), false).IsDeprecated);
    }

    [Fact]
    public void Create_Selected()
    {
        Assert.True(SuggestRowModel.Create(Make(), true).IsSelected);
        Assert.False(SuggestRowModel.Create(Make(), false).IsSelected);
    }

    [Fact]
    public void Create_LabelDetails_Copied()
    {
        SuggestItem item = Make(new CompletionItem("foo", "foo", CompletionItemKind.Method) { LabelDetail = "(x)", LabelDescription = "Lib" });
        SuggestRowModel row = SuggestRowModel.Create(item, false);
        Assert.Equal("foo", row.Label);
        Assert.Equal("(x)", row.LabelDetail);
        Assert.Equal("Lib", row.LabelDescription);
        Assert.Equal("Me", row.BadgeGlyph);
        Assert.Equal("Method", row.KindLabel);
    }

    [Fact]
    public void DetailText_DetailAndDocumentation()
        => Assert.Equal("detail\n\ndoc text", SuggestRowModel.DetailText(Make(new CompletionItem("a", "a") { Detail = "detail", Documentation = new MarkdownString("doc text") })));

    [Fact]
    public void DetailText_OnlyDetail()
        => Assert.Equal("detail", SuggestRowModel.DetailText(Make(new CompletionItem("a", "a") { Detail = "detail" })));

    [Fact]
    public void DetailText_Empty() => Assert.Equal("", SuggestRowModel.DetailText(Make()));

    [Fact]
    public void DetailText_Markdown_PlainText()
        => Assert.Equal("bold code", SuggestRowModel.DetailText(Make(new CompletionItem("a", "a") { Documentation = new MarkdownString("**bold** `code`") })));

    [Fact]
    public void DetailText_CutsAt400()
    {
        string text = SuggestRowModel.DetailText(Make(new CompletionItem("a", "a") { Documentation = new MarkdownString(new string('x', 1000)) }));
        Assert.Equal(new string('x', 400) + "…", text);
    }
}
