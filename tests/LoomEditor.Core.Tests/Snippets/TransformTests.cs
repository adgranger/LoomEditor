using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco tests these classes through the parser (ported in P10-21): all tests here are new.
public class TransformTests
{
    private static Transform Create(string source, string flags, params SnippetMarker[] format)
    {
        var transform = new Transform();
        transform.SetRegexp(source, flags);
        foreach (SnippetMarker marker in format)
        {
            transform.AppendChild(marker);
        }

        return transform;
    }

    [Fact]
    public void Transform_Resolve_ReplacesFirstMatchWithoutG() =>
        Assert.Equal("a-bX", Create("X", string.Empty, new TextMarker("-")).Resolve("aXbX"));

    [Fact]
    public void Transform_Resolve_ReplacesAllWithG() =>
        Assert.Equal("a-b-", Create("X", "g", new TextMarker("-")).Resolve("aXbX"));

    [Fact]
    public void Transform_Resolve_IgnoreCase() =>
        Assert.Equal("A-", Create("x", "i", new TextMarker("-")).Resolve("AX"));

    [Fact]
    public void Transform_Resolve_UsesTheCaptureGroups() =>
        Assert.Equal("b-a", Create("(a)-(b)", string.Empty, new FormatString(2), new TextMarker("-"), new FormatString(1)).Resolve("a-b"));

    [Fact]
    public void Transform_Resolve_NoMatch_ElseBranch() =>
        Assert.Equal("none", Create("\\d", string.Empty, new FormatString(1, null, null, "none")).Resolve("abc"));

    [Fact]
    public void Transform_Resolve_NoMatch_NoElse_ReturnsInput() =>
        Assert.Equal("abc", Create("\\d", string.Empty, new FormatString(1, "upcase")).Resolve("abc"));

    [Fact]
    public void Transform_ToTextmateString_RoundTripsSourceAndFlags() =>
        Assert.Equal("/(a)/${1:/upcase}-/gi", Create("(a)", "gi", new FormatString(1, "upcase"), new TextMarker("-")).ToTextmateString());

    [Fact]
    public void Transform_Clone_Independent()
    {
        Transform original = Create("a", "g", new TextMarker("-"));

        var clone = (Transform)original.Clone();
        clone.SetRegexp("b", "i");

        Assert.Equal("/a/-/g", original.ToTextmateString());
        Assert.Equal("/b/-/i", clone.ToTextmateString());
        Assert.NotSame(original.Children[0], ((Transform)original.Clone()).Children[0]);
    }

    [Fact]
    public void Transform_ToString_IsEmpty() => Assert.Equal(string.Empty, Create("a", string.Empty, new TextMarker("-")).ToString());
}
