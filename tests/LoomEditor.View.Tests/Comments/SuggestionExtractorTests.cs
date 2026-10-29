using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class SuggestionExtractorTests
{
    [Fact]
    public void Extract_TwoSuggestionsAndOneCsharpFence_ReturnsTwoWithIndexes()
    {
        var r = SuggestionExtractor.Extract("a\n```suggestion\nx\n```\n```csharp\ny\n```\n```suggestion\nz\n```");
        Assert.Equal(2, r.Count);
        Assert.Equal((0, "x"), (r[0].Index, r[0].Text));
        Assert.Equal((1, "z"), (r[1].Index, r[1].Text));
    }

    [Fact]
    public void Extract_Empty_ReturnsEmpty() => Assert.Empty(SuggestionExtractor.Extract(""));

    [Fact]
    public void Extract_NullBody_ReturnsEmpty() => Assert.Empty(SuggestionExtractor.Extract(null));

    [Fact]
    public void Extract_EmptyBlock_IsDeletion()
    {
        var b = Assert.Single(SuggestionExtractor.Extract("```suggestion\n```"));
        Assert.True(b.IsDeletion);
        Assert.Empty(b.Lines);
    }

    [Fact]
    public void Extract_MultiLineBlock_SplitsLines()
    {
        var b = Assert.Single(SuggestionExtractor.Extract("```suggestion\na\nb\n```"));
        Assert.Equal(["a", "b"], b.Lines);
    }

    [Fact]
    public void Extract_InsideQuote_Ignored() =>
        Assert.Empty(SuggestionExtractor.Extract("> ```suggestion\n> x\n> ```"));

    [Fact]
    public void Extract_WrongCase_Ignored() =>
        Assert.Empty(SuggestionExtractor.Extract("```Suggestion\nx\n```"));

    [Fact]
    public void Extract_TildeFence_Works() =>
        Assert.Single(SuggestionExtractor.Extract("~~~suggestion\nx\n~~~"));
}
