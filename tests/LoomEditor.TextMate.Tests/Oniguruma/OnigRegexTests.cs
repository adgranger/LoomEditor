using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Tests.Oniguruma;

public class OnigRegexTests
{
    [Fact]
    public void Compile_InvalidPattern_ThrowsWithOnigurumaMessage()
    {
        OnigException exception = Assert.Throws<OnigException>(() => new OnigRegex("a(b"));

        Assert.Equal("end pattern with unmatched parenthesis", exception.Message);
    }

    [Fact]
    public void Search_FromPosition_ReturnsUtf16Offsets()
    {
        using var regex = new OnigRegex("ab");
        const string Subject = "\U0001F600 ab ab";

        OnigMatch? match = regex.Search(Subject, 4, OnigSearchOptions.None);

        Assert.NotNull(match);
        Assert.Equal([new OnigCaptureIndex(6, 8)], match.CaptureIndices);
        Assert.Equal(2, match.CaptureIndices[0].Length);
    }

    [Fact]
    public void Search_NoMatch_ReturnsNull()
    {
        using var regex = new OnigRegex("ab");

        Assert.Null(regex.Search("ab xy", 1, OnigSearchOptions.None));
    }

    [Fact]
    public void Search_UnmatchedGroup_ReturnsMinusOne()
    {
        using var regex = new OnigRegex("(a)|(b)");

        OnigMatch? match = regex.Search("xb", 0, OnigSearchOptions.None);

        Assert.NotNull(match);
        Assert.Equal([new OnigCaptureIndex(1, 2), new OnigCaptureIndex(-1, -1), new OnigCaptureIndex(1, 2)], match.CaptureIndices);
    }

    [Theory]
    [InlineData(@"\h+", "zz1aFg", 2, 5)]
    [InlineData("[[:alpha:]]+", "12abc3", 2, 5)]
    [InlineData("a++", "baaa", 1, 4)]
    [InlineData(@"(?<n>a|\(\g<n>\))", "x((a))", 1, 6)]
    public void Search_OnigurumaOnlySyntax_Works(string pattern, string subject, int start, int end)
    {
        using var regex = new OnigRegex(pattern);

        OnigMatch? match = regex.Search(subject, 0, OnigSearchOptions.None);

        Assert.NotNull(match);
        Assert.Equal(new OnigCaptureIndex(start, end), match.CaptureIndices[0]);
    }

    [Fact]
    public void Search_PossessiveQuantifier_DoesNotBacktrack()
    {
        using var regex = new OnigRegex("a++a");

        Assert.Null(regex.Search("aaaa", 0, OnigSearchOptions.None));
    }

    [Fact]
    public void Search_NotBeginPosition_SkipsAMatchAtTheStart()
    {
        using var regex = new OnigRegex(@"\Ga");

        Assert.NotNull(regex.Search("aa", 1, OnigSearchOptions.None));
        Assert.Null(regex.Search("aa", 1, OnigSearchOptions.NotBeginPosition));
    }

    [Theory]
    [InlineData(@"\Gabc", true)]
    [InlineData(@"(?:x|\G)y", true)]
    [InlineData("abc", false)]
    [InlineData(@"abc\", false)]
    public void HasGAnchor_DetectsBackslashG(string pattern, bool expected)
    {
        Assert.Equal(expected, OnigRegex.ContainsGAnchor(pattern));
    }

    [Fact]
    public void HasGAnchor_IsRememberedByTheRegex()
    {
        using var regex = new OnigRegex(@"\Gabc");

        Assert.True(regex.HasGAnchor);
    }

    [Fact]
    public void Version_Is6910()
    {
        Assert.Equal("6.9.10", new OnigurumaEngine().Version);
    }
}
