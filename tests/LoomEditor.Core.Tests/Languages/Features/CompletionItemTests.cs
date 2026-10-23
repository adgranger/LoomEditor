using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Languages.Features;

public class CompletionItemTests
{
    [Fact]
    public void Constructor_NullLabel_Throws() => Assert.Throws<ArgumentNullException>(() => new CompletionItem(null!, "x"));

    [Fact]
    public void Constructor_Defaults()
    {
        var item = new CompletionItem("l", "i");
        Assert.Equal(CompletionItemKind.Text, item.Kind);
        Assert.Null(item.Range);
        Assert.False(item.Preselect);
    }

    // Monaco: CompletionItem resolve (Object.assign of the resolved item)
    [Fact]
    public void MergeFrom_CopiesOnlyResolvableNonNullMembers()
    {
        var item = new CompletionItem("label", "insert") { SortText = "s", Detail = "old" };
        var edits = new[] { TextEdit.Insert(new Position(1, 1), "x") };
        var resolved = new CompletionItem("other", "other-insert")
        {
            Detail = "new",
            Documentation = new MarkdownString("doc"),
            AdditionalTextEdits = edits,
            SortText = "other-sort",
        };

        item.MergeFrom(resolved);

        Assert.Equal("new", item.Detail);
        Assert.Equal("doc", item.Documentation!.Value);
        Assert.Same(edits, item.AdditionalTextEdits);
        Assert.Equal("label", item.Label);
        Assert.Equal("insert", item.InsertText);
        Assert.Equal("s", item.SortText);

        item.MergeFrom(new CompletionItem("x", "y"));
        Assert.Equal("new", item.Detail);
    }

    [Fact]
    public void MergeFrom_Null_Throws() => Assert.Throws<ArgumentNullException>(() => new CompletionItem("a", "b").MergeFrom(null!));

    [Fact]
    public void CompletionRange_FromRange_SameRange()
    {
        var range = new Range(1, 1, 1, 4);
        var single = CompletionRange.FromRange(range);
        Assert.Equal(range, single.Insert);
        Assert.Equal(range, single.Replace);
    }

    [Fact]
    public void Location_ToLink_NoOptionalRanges()
    {
        var range = new Range(1, 1, 2, 2);
        LocationLink link = new Location("file:///a", range).ToLink();
        Assert.Equal("file:///a", link.Uri);
        Assert.Equal(range, link.Range);
        Assert.Null(link.OriginSelectionRange);
        Assert.Null(link.TargetSelectionRange);
    }

    // Monaco: CompletionItemKind
    [Fact]
    public void CompletionItemKind_Order()
    {
        Assert.Equal(0, (int)CompletionItemKind.Method);
        Assert.Equal(16, (int)CompletionItemKind.EnumMember);
        Assert.Equal(28, (int)CompletionItemKind.Snippet);
    }
}
