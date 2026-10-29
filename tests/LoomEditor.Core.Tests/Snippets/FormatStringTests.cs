using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco tests FormatString through the parser (ported in P10-21): all tests here are new.
public class FormatStringTests
{
    private static string Resolve(string shorthand, string value) => new FormatString(1, shorthand).Resolve(value);

    [Fact]
    public void FormatString_Upcase() => Assert.Equal("FOO", Resolve("upcase", "foo"));

    [Fact]
    public void FormatString_Downcase() => Assert.Equal("foo", Resolve("downcase", "FOO"));

    [Fact]
    public void FormatString_Capitalize() => Assert.Equal("Foo bar", Resolve("capitalize", "foo bar"));

    [Fact]
    public void FormatString_Pascalcase() => Assert.Equal("FooBarBaz", Resolve("pascalcase", "foo-bar baz"));

    [Fact]
    public void FormatString_Camelcase() => Assert.Equal("fooBarBaz", Resolve("camelcase", "foo-bar baz"));

    [Fact]
    public void FormatString_Kebabcase()
    {
        Assert.Equal("foo-bar-baz", Resolve("kebabcase", "FooBar baz"));
        Assert.Equal("http-server", Resolve("kebabcase", "HTTPServer"));
        Assert.Equal("foo-bar", Resolve("kebabcase", "__foo_bar__"));
    }

    [Fact]
    public void FormatString_Snakecase() => Assert.Equal("foo_bar_baz_x", Resolve("snakecase", "fooBar baz-x"));

    [Fact]
    public void FormatString_Shorthand_EmptyValue_IsEmpty() => Assert.Equal(string.Empty, Resolve("upcase", string.Empty));

    [Fact]
    public void FormatString_IfValue_WhenGroupMatched() => Assert.Equal("yes", new FormatString(1, null, "yes", "no").Resolve("x"));

    [Fact]
    public void FormatString_ElseValue_WhenGroupEmpty() => Assert.Equal("no", new FormatString(1, null, "yes", "no").Resolve(string.Empty));

    [Fact]
    public void FormatString_PlainGroup()
    {
        Assert.Equal("x", new FormatString(1).Resolve("x"));
        Assert.Equal(string.Empty, new FormatString(1).Resolve(null));
    }

    [Fact]
    public void FormatString_ToTextmateString_AllForms()
    {
        Assert.Equal("${1}", new FormatString(1).ToTextmateString());
        Assert.Equal("${1:/upcase}", new FormatString(1, "upcase").ToTextmateString());
        Assert.Equal("${1:?a:b}", new FormatString(1, null, "a", "b").ToTextmateString());
        Assert.Equal("${1:+a}", new FormatString(1, null, "a").ToTextmateString());
        Assert.Equal("${1:-b}", new FormatString(1, null, null, "b").ToTextmateString());
    }

    [Fact]
    public void FormatString_Clone_KeepsEverything()
    {
        var clone = (FormatString)new FormatString(2, "camelcase", "a", "b").Clone();

        Assert.Equal((2, "camelcase", "a", "b"), (clone.Index, clone.ShorthandName, clone.IfValue, clone.ElseValue));
    }
}
