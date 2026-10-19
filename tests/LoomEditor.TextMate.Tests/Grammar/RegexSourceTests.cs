using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Tests.Grammar;

public class RegexSourceTests
{
    [Fact]
    public void RegexSource_ReplaceCaptures_DowncaseAndUpcase()
    {
        const string CaptureSource = "<Div.Foo>";
        OnigCaptureIndex[] captures = [new(0, 9), new(1, 4), new(4, 8)];

        Assert.Equal("div|DIV|Div|Foo", RegexSource.ReplaceCaptures("${1:/downcase}|${1:/upcase}|$1|$2", CaptureSource, captures));
    }

    [Fact]
    public void RegexSource_ReplaceCaptures_MissingCaptureKeepsTheReference()
    {
        Assert.Equal("a$7b", RegexSource.ReplaceCaptures("a$7b", "xyz", [new(0, 3)]));
    }

    [Fact]
    public void RegexSource_ReplaceCaptures_UnmatchedGroupIsEmpty()
    {
        Assert.Equal("[]", RegexSource.ReplaceCaptures("[$1]", "xyz", [new(0, 3), new(-1, -1)]));
    }

    [Theory]
    [InlineData("\\$1", true)]
    [InlineData("${12:/upcase}", true)]
    [InlineData("${1:/capitalize}", false)]
    [InlineData("$x", false)]
    [InlineData("$٣", false)]
    [InlineData(null, false)]
    public void RegexSource_HasCaptures_IsAsciiDigitsOnly(string? source, bool expected)
    {
        Assert.Equal(expected, RegexSource.HasCaptures(source));
    }

    [Theory]
    [InlineData("#fff", true)]
    [InlineData("#ffff", true)]
    [InlineData("#A0b1C2", true)]
    [InlineData("#A0b1C2d3", true)]
    [InlineData("#ff", false)]
    [InlineData("#fffff", false)]
    [InlineData("#ggg", false)]
    [InlineData("fff", false)]
    [InlineData("#fff\n", false)]
    public void IsValidHexColor_AcceptsThreeFourSixEightDigits(string hex, bool expected)
    {
        Assert.Equal(expected, Strings.IsValidHexColor(hex));
    }

    [Theory]
    [InlineData("a/b/c.json", "c.json")]
    [InlineData("a\\b\\c.json", "c.json")]
    [InlineData("a/b/", "b")]
    [InlineData("c.json", "c.json")]
    public void Basename_ReturnsTheLastSegment(string path, string expected)
    {
        Assert.Equal(expected, Strings.Basename(path));
    }

    [Fact]
    public void Strcmp_IsOrdinal()
    {
        Assert.Equal(-1, Strings.Strcmp("B", "a"));
        Assert.Equal(1, Strings.Strcmp("b", "a"));
        Assert.Equal(0, Strings.Strcmp("a", "a"));
        Assert.Equal(-1, Strings.StrArrCmp(null, ["a"]));
        Assert.Equal(1, Strings.StrArrCmp(["a", "b"], ["a"]));
        Assert.Equal(-1, Strings.StrArrCmp(["a", "a"], ["a", "b"]));
    }

    [Fact]
    public void EscapeRegExpCharacters_EscapesSpecialCharacters()
    {
        Assert.Equal(@"a\.b\ \(c\)\-\#", Strings.EscapeRegExpCharacters("a.b (c)-#"));
    }

    [Fact]
    public void CachedFn_CallsTheFunctionOncePerKey()
    {
        int calls = 0;
        var cached = new CachedFn<string, int>(key =>
        {
            calls++;
            return key.Length;
        });

        Assert.Equal(3, cached.Get("abc"));
        Assert.Equal(3, cached.Get("abc"));
        Assert.Equal(1, calls);
    }
}
