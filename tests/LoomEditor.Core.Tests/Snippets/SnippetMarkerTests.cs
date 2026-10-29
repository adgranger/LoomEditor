using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco has no separate tests for these members (snippetParser.test.ts covers them through the parser, P10-21): all new.
public class SnippetMarkerTests
{
    [Fact]
    public void TextMarker_Escape_EscapesBackslashDollarBrace() =>
        Assert.Equal("a\\\\b\\$c\\}d{e", TextMarker.Escape("a\\b$c}d{e"));

    [Fact]
    public void TextMarker_ToTextmateString_Escapes() =>
        Assert.Equal("\\$1 \\}", new TextMarker("$1 }").ToTextmateString());

    [Fact]
    public void Placeholder_CompareByIndex_FinalTabstopLast()
    {
        var zero = new Placeholder(0);
        var one = new Placeholder(1);
        var two = new Placeholder(2);

        Assert.Equal(1, Placeholder.CompareByIndex(zero, one));
        Assert.Equal(-1, Placeholder.CompareByIndex(one, zero));
        Assert.Equal(-1, Placeholder.CompareByIndex(one, two));
        Assert.Equal(1, Placeholder.CompareByIndex(two, one));
        Assert.Equal(0, Placeholder.CompareByIndex(one, new Placeholder(1)));
        Assert.Equal(0, Placeholder.CompareByIndex(zero, new Placeholder(0)));
    }

    [Fact]
    public void Placeholder_ToTextmateString_CoversEveryForm()
    {
        Assert.Equal("$1", new Placeholder(1).ToTextmateString());

        var withDefault = new Placeholder(2);
        withDefault.AppendChild(new TextMarker("a}b"));
        Assert.Equal("${2:a\\}b}", withDefault.ToTextmateString());

        var choice = new Choice();
        choice.AppendChild(new TextMarker("x"));
        choice.AppendChild(new TextMarker("y"));
        var withChoice = new Placeholder(3);
        withChoice.AppendChild(choice);
        Assert.Equal("${3|x,y|}", withChoice.ToTextmateString());
    }

    [Fact]
    public void Placeholder_Clone_IsDeepAndEqual()
    {
        var original = new Placeholder(1);
        var inner = new Placeholder(2);
        inner.AppendChild(new TextMarker("deep"));
        original.AppendChild(new TextMarker("a"));
        original.AppendChild(inner);

        var clone = (Placeholder)original.Clone();

        Assert.Equal(original.ToTextmateString(), clone.ToTextmateString());
        Assert.NotSame(original.Children[1], clone.Children[1]);
        Assert.Same(clone, clone.Children[1].Parent);
        ((TextMarker)clone.Children[1].Children[0]).Value = "changed";
        Assert.Equal("deep", original.Children[1].Children[0].ToString());
    }

    [Fact]
    public void Choice_ToString_FirstOption()
    {
        var choice = new Choice();
        choice.AppendChild(new TextMarker("first"));
        choice.AppendChild(new TextMarker("second"));

        Assert.Equal("first", choice.ToString());
        Assert.Equal(5, choice.Length);
    }

    [Fact]
    public void Choice_ToTextmateString_JoinsWithComma()
    {
        var choice = new Choice();
        choice.AppendChild(new TextMarker("a,b"));
        choice.AppendChild(new TextMarker("c|d"));
        choice.AppendChild(new TextMarker("e\\f"));

        Assert.Equal("a\\,b,c\\|d,e\\\\f", choice.ToTextmateString());
    }

    [Fact]
    public void Choice_AppendChild_IgnoresNonText()
    {
        var choice = new Choice();

        choice.AppendChild(new Placeholder(1));

        Assert.Empty(choice.Options);
    }

    [Fact]
    public void Marker_AppendChild_MergesConsecutiveText()
    {
        var parent = new Placeholder(1);
        parent.AppendChild(new TextMarker("a"));
        parent.AppendChild(new TextMarker("b"));

        Assert.Single(parent.Children);
        Assert.Equal("ab", parent.ToString());
    }

    [Fact]
    public void Marker_Replace_ReparentsChildren()
    {
        var root = new Placeholder(1);
        var target = new Placeholder(2);
        root.AppendChild(new TextMarker("a"));
        root.AppendChild(target);
        var first = new Placeholder(3);
        var nested = new TextMarker("n");
        first.AppendChild(nested);
        var second = new TextMarker("z");

        SnippetMarker.Replace(target, [first, second]);

        Assert.Equal(3, root.Children.Count);
        Assert.Same(first, root.Children[1]);
        Assert.Same(root, first.Parent);
        Assert.Same(root, second.Parent);
        Assert.Same(first, nested.Parent);
    }

    [Fact]
    public void Marker_Snippet_ReturnsRoot()
    {
        var root = new TextmateSnippet();
        var middle = new Placeholder(2);
        var leaf = new TextMarker("x");
        root.AppendChild(middle);
        middle.AppendChild(leaf);

        Assert.Same(root, leaf.Snippet);
        Assert.Same(root, root.Snippet);
        Assert.Same(leaf, root.RightMostDescendant);
        Assert.Null(new TextMarker("loose").Snippet);
    }

    [Fact]
    public void Marker_Ids_AreUnique()
    {
        var markers = new SnippetMarker[] { new TextMarker("a"), new Placeholder(1), new Choice(), new TextMarker("a").Clone() };

        Assert.Equal(markers.Length, markers.Select(marker => marker.Id).Distinct().Count());
    }
}
