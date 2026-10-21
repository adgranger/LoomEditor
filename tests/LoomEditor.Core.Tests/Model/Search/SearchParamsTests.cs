using System.Text.RegularExpressions;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.Search;

public class SearchParamsTests
{
    private const RegexOptions Base = RegexOptions.CultureInvariant;
    private const RegexOptions Ci = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    private const RegexOptions CiMulti = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline;

    // Monaco: assertParseSearchResult (the regex flags g and u map to nothing, i to IgnoreCase, m to Multiline)
    private static void AssertParse(string searchString, bool isRegex, bool matchCase, string? wordSeparators,
        string? expectedSource, RegexOptions expectedOptions, string? expectedSimple)
    {
        var actual = new SearchParams(searchString, isRegex, matchCase, wordSeparators).ParseSearchRequest();

        if (expectedSource is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expectedSource, actual.Regex.ToString());
        Assert.Equal(expectedOptions, actual.Regex.Options);
        Assert.Equal(expectedSimple, actual.SimpleSearch);
        Assert.Equal(wordSeparators is not null, actual.WordSeparators is not null);
    }

    // Monaco: 'parseSearchRequest invalid'
    [Fact]
    public void ParseSearchRequest_Invalid_ReturnsNull()
    {
        AssertParse("", true, true, WordHelper.UsualWordSeparators, null, Base, null);
        AssertParse("(", true, false, null, null, Base, null);
    }

    // Monaco: 'parseSearchRequest non regex'
    [Fact]
    public void ParseSearchRequest_NonRegex_BuildsExpectedData()
    {
        var ws = WordHelper.UsualWordSeparators;
        AssertParse("foo", false, false, null, "foo", Ci, null);
        AssertParse("foo", false, false, ws, "foo", Ci, null);
        AssertParse("foo", false, true, null, "foo", Base, "foo");
        AssertParse("foo", false, true, ws, "foo", Base, "foo");
        AssertParse(@"foo\n", false, false, null, @"foo\\n", Ci, null);
        AssertParse(@"foo\\n", false, false, null, @"foo\\\\n", Ci, null);
        AssertParse(@"foo\r", false, false, null, @"foo\\r", Ci, null);
        AssertParse(@"foo\\r", false, false, null, @"foo\\\\r", Ci, null);
    }

    // Monaco: 'parseSearchRequest regex'
    [Fact]
    public void ParseSearchRequest_Regex_BuildsExpectedData()
    {
        var ws = WordHelper.UsualWordSeparators;
        AssertParse("foo", true, false, null, "foo", Ci, null);
        AssertParse("foo", true, false, ws, "foo", Ci, null);
        AssertParse("foo", true, true, null, "foo", Base, null);
        AssertParse("foo", true, true, ws, "foo", Base, null);
        AssertParse(@"foo\n", true, false, null, @"foo\n", CiMulti, null);
        AssertParse(@"foo\\n", true, false, null, @"foo\\n", Ci, null);
        AssertParse(@"foo\r", true, false, null, @"foo\r", CiMulti, null);
        AssertParse(@"foo\\r", true, false, null, @"foo\\r", Ci, null);
    }

    // Monaco: 'isMultilineRegexSource'
    [Fact]
    public void IsMultilineRegexSource_Sources_DetectsLineBreaks()
    {
        Assert.False(SearchParams.IsMultilineRegexSource("foo"));
        Assert.False(SearchParams.IsMultilineRegexSource(""));
        Assert.False(SearchParams.IsMultilineRegexSource(@"foo\sbar"));
        Assert.False(SearchParams.IsMultilineRegexSource(@"\\notnewline"));

        Assert.True(SearchParams.IsMultilineRegexSource(@"foo\nbar"));
        Assert.True(SearchParams.IsMultilineRegexSource(@"foo\nbar\s"));
        Assert.True(SearchParams.IsMultilineRegexSource(@"foo\r\n"));
        Assert.True(SearchParams.IsMultilineRegexSource(@"\n"));
        Assert.True(SearchParams.IsMultilineRegexSource(@"foo\W"));
        Assert.True(SearchParams.IsMultilineRegexSource("foo" + (char)10));
        Assert.True(SearchParams.IsMultilineRegexSource("foo" + (char)13 + (char)10));
    }

    // Monaco: 'isMultilineRegexSource correctly identifies multiline patterns'
    [Fact]
    public void IsMultilineRegexSource_RealPatterns_IdentifiesMultiline()
    {
        string[] singleLine = [@"MARK:\s*(?<label>.*)$", "^// Header$", @"\s*[-=]+\s*"];
        string[] multiLine =
        [
            @"^// =+\n^// (?<label>[^\n]+?)\n^// =+$",
            @"header\r\nfooter",
            @"start\r|\nend",
            "top" + (char)10 + "middle" + (char)13 + (char)10 + "bottom",
        ];

        foreach (var pattern in singleLine)
        {
            Assert.False(SearchParams.IsMultilineRegexSource(pattern), pattern);
        }

        foreach (var pattern in multiLine)
        {
            Assert.True(SearchParams.IsMultilineRegexSource(pattern), pattern);
        }
    }

    [Fact]
    public void CreateRegExp_InvalidPattern_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => Strings.CreateRegExp("(", true, true, false));
        Assert.Throws<ArgumentException>(() => Strings.CreateRegExp("", false, true, false));
    }

    [Fact]
    public void EscapeRegExpCharacters_KeepsSpacesAndHash()
    {
        Assert.Equal("a b#c", Strings.EscapeRegExpCharacters("a b#c"));
        Assert.Equal(@"\(a\)\[\]\{\}", Strings.EscapeRegExpCharacters("(a)[]{}"));
    }
}
