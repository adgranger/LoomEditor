using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco tests these classes through the parser (ported in P10-21): all tests here are new.
public class TextmateSnippetTests
{
    private sealed class MapResolver(Dictionary<string, string> values) : IVariableResolver
    {
        public string? Resolve(Variable variable) => values.GetValueOrDefault(variable.Name);
    }

    // a${2:bc${1:d}}$0
    private static (TextmateSnippet Snippet, Placeholder Two, Placeholder One, Placeholder Zero) Create()
    {
        var snippet = new TextmateSnippet();
        var two = new Placeholder(2);
        var one = new Placeholder(1);
        var zero = new Placeholder(0);
        one.AppendChild(new TextMarker("d"));
        two.AppendChild(new TextMarker("bc"));
        two.AppendChild(one);
        snippet.AppendChild(new TextMarker("a"));
        snippet.AppendChild(two);
        snippet.AppendChild(zero);
        return (snippet, two, one, zero);
    }

    [Fact]
    public void TextmateSnippet_Placeholders_SortedWithFinalLast()
    {
        var (snippet, two, one, zero) = Create();

        Assert.Equal([two, one, zero], snippet.Placeholders);
        Assert.Same(two, snippet.PlaceholderInfo.Last);
        Assert.Equal([one, two, zero], snippet.Placeholders.Order(Comparer<Placeholder>.Create(Placeholder.CompareByIndex)));
    }

    [Fact]
    public void TextmateSnippet_PlaceholderInfo_Last_IsTheFirstOnATie()
    {
        var snippet = new TextmateSnippet();
        var first = new Placeholder(1);
        snippet.AppendChild(first);
        snippet.AppendChild(new Placeholder(1));

        Assert.Same(first, snippet.PlaceholderInfo.Last);
    }

    [Fact]
    public void TextmateSnippet_Offset_OfNestedPlaceholder()
    {
        var (snippet, _, one, _) = Create();

        Assert.Equal(3, snippet.Offset(one));
        Assert.Equal(-1, snippet.Offset(new Placeholder(9)));
    }

    [Fact]
    public void TextmateSnippet_FullLength()
    {
        var (_, two, one, _) = Create();

        Assert.Equal(3, TextmateSnippet.FullLength(two));
        Assert.Equal(1, TextmateSnippet.FullLength(one));
    }

    [Fact]
    public void TextmateSnippet_EnclosingPlaceholders_Nested()
    {
        var (_, two, one, _) = Create();

        Assert.Equal([two], TextmateSnippet.EnclosingPlaceholders(one));
        Assert.Empty(TextmateSnippet.EnclosingPlaceholders(two));
    }

    [Fact]
    public void TextmateSnippet_Replace_InvalidatesPlaceholders()
    {
        var (snippet, _, _, zero) = Create();
        Assert.Equal(3, snippet.Placeholders.Count);

        snippet.Replace(zero, [new TextMarker("z")]);

        Assert.Equal(2, snippet.Placeholders.Count);
        Assert.Equal("a${2:bc${1:d}}z", snippet.ToTextmateString());
    }

    [Fact]
    public void TextmateSnippet_AppendChild_InvalidatesPlaceholders()
    {
        var (snippet, _, _, _) = Create();
        Assert.Equal(3, snippet.Placeholders.Count);

        snippet.AppendChild(new Placeholder(5));

        Assert.Equal(4, snippet.Placeholders.Count);
    }

    [Fact]
    public void TextmateSnippet_ResolveVariables_ReplacesKnownAndInvalidatesPlaceholders()
    {
        var snippet = new TextmateSnippet();
        var known = new Variable("NAME");
        var unknown = new Variable("OTHER");
        unknown.AppendChild(new TextMarker("default"));
        snippet.AppendChild(known);
        snippet.AppendChild(unknown);
        Assert.Empty(snippet.Placeholders);

        TextmateSnippet result = snippet.ResolveVariables(new MapResolver(new Dictionary<string, string> { ["NAME"] = "value" }));

        Assert.Same(snippet, result);
        Assert.Equal("valuedefault", snippet.ToString());
    }

    [Fact]
    public void TextmateSnippet_Clone_IsDeep()
    {
        var (snippet, _, _, _) = Create();

        var clone = (TextmateSnippet)snippet.Clone();

        Assert.Equal(snippet.ToTextmateString(), clone.ToTextmateString());
        Assert.NotSame(snippet.Children[1], clone.Children[1]);
        Assert.Same(clone, clone.Children[1].Parent);
        Assert.Same(clone, clone.Children[1].Children[1].Snippet);
        clone.Replace(clone.Children[2], []);
        Assert.Equal(3, snippet.Placeholders.Count);
    }

    [Fact]
    public void TextmateSnippet_ToTextmateString_RoundTrip() =>
        Assert.Equal("a${2:bc${1:d}}$0", Create().Snippet.ToTextmateString());

    [Fact]
    public void Walk_StopsWhenTheVisitorReturnsFalse()
    {
        var (snippet, _, _, _) = Create();
        var visited = new List<string>();

        snippet.Walk(marker =>
        {
            visited.Add(marker.GetType().Name);
            return marker is not Placeholder { Index: 1 };
        });

        Assert.Equal(["TextMarker", "Placeholder", "TextMarker", "Placeholder"], visited);
    }
}
