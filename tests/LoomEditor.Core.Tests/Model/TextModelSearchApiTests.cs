using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

public class TextModelSearchApiTests
{
    private static readonly string Lf = ((char)10).ToString();

    private static string Lines(params string[] lines) => string.Join(Lf, lines);

    // A model whose text is spread over several pieces (edits split and add pieces).
    private static TextModel ChunkedModel()
    {
        var model = new TextModel(Lines("foo bar", "baz foo", "qux"));
        model.ApplyEdits([new TextEdit(new Range(1, 4, 1, 4), " foo"), new TextEdit(new Range(3, 4, 3, 4), Lf + "foo foo")]);
        return model;
    }

    [Fact]
    public void FindMatches_OverlappingScopes_AreMergedAndSearchedOnce()
    {
        var model = new TextModel("foo foo foo");

        var result = model.FindMatches("foo", [new Range(1, 1, 1, 8), new Range(1, 5, 1, 12)], false, false, null, false);

        Assert.Equal([new Range(1, 1, 1, 4), new Range(1, 5, 1, 8), new Range(1, 9, 1, 12)], result.Select(m => m.Range));
    }

    [Fact]
    public void FindMatches_UnsortedScopes_ResultsInDocumentOrder()
    {
        var model = new TextModel(Lines("foo", "bar", "foo"));

        var result = model.FindMatches("foo", [new Range(3, 1, 3, 4), new Range(1, 1, 1, 4)], false, false, null, false);

        Assert.Equal([new Range(1, 1, 1, 4), new Range(3, 1, 3, 4)], result.Select(m => m.Range));
    }

    [Fact]
    public void FindMatches_PlainText_UsesPieceTreeAndEqualsTextModelSearch()
    {
        var model = ChunkedModel();

        var viaModel = model.FindMatches("foo", true, false, false, null, false);
        var viaSearch = TextModelSearch.FindMatches(model, new SearchParams("foo", false, false, null), model.FullModelRange, false, 999);

        Assert.NotEmpty(viaModel);
        Assert.Equal(viaSearch.Select(m => m.Range), viaModel.Select(m => m.Range));
    }

    [Fact]
    public void FindMatches_DefaultLimit_Is999()
    {
        var model = new TextModel(string.Concat(Enumerable.Repeat("a", 1500)));

        Assert.Equal(999, model.FindMatches("a", true, false, true, null, false).Count);
        Assert.Equal(5, model.FindMatches("a", true, false, true, null, false, 5).Count);
    }

    [Fact]
    public void FindNextMatch_PastLastMatch_WrapsToTop()
    {
        var model = new TextModel(Lines("foo", "bar"));

        var plain = model.FindNextMatch("foo", new Position(1, 4), false, false, null, false);
        var regex = model.FindNextMatch("fo+", new Position(1, 4), true, false, null, false);

        Assert.Equal(new Range(1, 1, 1, 4), plain?.Range);
        Assert.Equal(new Range(1, 1, 1, 4), regex?.Range);
    }

    [Fact]
    public void FindPreviousMatch_BeforeFirstMatch_WrapsToBottom()
    {
        var model = new TextModel(Lines("foo", "bar foo"));

        var match = model.FindPreviousMatch("foo", new Position(1, 1), false, false, null, false);

        Assert.Equal(new Range(2, 5, 2, 8), match?.Range);
    }
}
