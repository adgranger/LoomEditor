using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.Search.TestHelpers;

namespace LoomEditor.Core.Tests.Model.Search;

// Monaco: suite 'TextModelSearch' (the unicode escape test is not ported: \u{0066} is JavaScript-only syntax).
public partial class TextModelSearchTests
{
    private static readonly string Lf = ((char)10).ToString();
    private static readonly string Cr = ((char)13).ToString();
    private static readonly string Usual = WordHelper.UsualWordSeparators;

    private static readonly string[] RegularText =
    [
        "This is some foo - bar text which contains foo and bar - as in Barcelona.",
        "Now it begins a word fooBar and now it is caps Foo-isn't this great?",
        "And here's a dull line with nothing interesting in it",
        "It is also interesting if it's part of a word like amazingFooBar",
        "Again nothing interesting here",
    ];

    private static string Join(params string[] lines) => string.Join(Lf, lines);

    private static string Surrogates(params int[] units) => new(units.Select(u => (char)u).ToArray());

    // Monaco: _assertFindMatches
    private static void AssertFindMatchesOnModel(TextModel model, SearchParams searchParams, FindMatch[] expected)
    {
        var actual = TextModelSearch.FindMatches(model, searchParams, model.FullModelRange, false, 1000);
        SearchAssert.Matches(actual, expected, "findMatches");

        // test `findNextMatch`
        var startPos = new Position(1, 1);
        var match = TextModelSearch.FindNextMatch(model, searchParams, startPos, false);
        Assert.Equal(expected.Length > 0, match is not null);
        if (expected.Length > 0)
        {
            SearchAssert.Match(match, expected[0].Range, null, $"findNextMatch {startPos}");
        }

        foreach (var expectedMatch in expected)
        {
            startPos = expectedMatch.Range.Start;
            match = TextModelSearch.FindNextMatch(model, searchParams, startPos, false);
            SearchAssert.Match(match, expectedMatch.Range, null, $"findNextMatch {startPos}");
        }

        // test `findPrevMatch`
        startPos = new Position(model.LineCount, model.GetLineMaxColumn(model.LineCount));
        match = TextModelSearch.FindPreviousMatch(model, searchParams, startPos, false);
        if (expected.Length > 0)
        {
            SearchAssert.Match(match, expected[^1].Range, null, $"findPrevMatch {startPos}");
        }

        foreach (var expectedMatch in expected)
        {
            startPos = expectedMatch.Range.End;
            match = TextModelSearch.FindPreviousMatch(model, searchParams, startPos, false);
            SearchAssert.Match(match, expectedMatch.Range, null, $"findPrevMatch {startPos}");
        }
    }

    // Monaco: assertFindMatches (runs on an LF and on a CRLF model)
    private static void AssertFindMatches(string text, string searchString, bool isRegex, bool matchCase, string? wordSeparators, params int[][] expected)
    {
        var expectedMatches = expected.Select(e => new FindMatch(new Range(e[0], e[1], e[2], e[3]), null)).ToArray();
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);

        AssertFindMatchesOnModel(new TextModel(text), searchParams, expectedMatches);

        var model2 = new TextModel(text);
        model2.SetEol(EndOfLineSequence.CRLF);
        AssertFindMatchesOnModel(model2, searchParams, expectedMatches);
    }

    private static int[] R(int a, int b, int c, int d) => [a, b, c, d];

    // Monaco: 'Simple find'
    [Fact]
    public void FindMatches_SimpleFind_FindsAll() => AssertFindMatches(
        Join(RegularText), "foo", false, false, null, R(1, 14, 1, 17), R(1, 44, 1, 47), R(2, 22, 2, 25), R(2, 48, 2, 51), R(4, 59, 4, 62));

    // Monaco: 'Case sensitive find'
    [Fact]
    public void FindMatches_CaseSensitive_FindsExactCase() => AssertFindMatches(
        Join(RegularText), "foo", false, true, null, R(1, 14, 1, 17), R(1, 44, 1, 47), R(2, 22, 2, 25));

    // Monaco: 'Whole words find'
    [Fact]
    public void FindMatches_WholeWords_FindsWholeWords() => AssertFindMatches(
        Join(RegularText), "foo", false, false, Usual, R(1, 14, 1, 17), R(1, 44, 1, 47), R(2, 48, 2, 51));

    // Monaco: '/^/ find'
    [Fact]
    public void FindMatches_LineStartRegex_FindsEachLine() => AssertFindMatches(
        Join(RegularText), "^", true, false, null, R(1, 1, 1, 1), R(2, 1, 2, 1), R(3, 1, 3, 1), R(4, 1, 4, 1), R(5, 1, 5, 1));

    // Monaco: '/$/ find'
    [Fact]
    public void FindMatches_LineEndRegex_FindsEachLine() => AssertFindMatches(
        Join(RegularText), "$", true, false, null, R(1, 74, 1, 74), R(2, 69, 2, 69), R(3, 54, 3, 54), R(4, 65, 4, 65), R(5, 31, 5, 31));

    // Monaco: '/.*/ find'
    [Fact]
    public void FindMatches_DotStarRegex_FindsEachLine() => AssertFindMatches(
        Join(RegularText), ".*", true, false, null, R(1, 1, 1, 74), R(2, 1, 2, 69), R(3, 1, 3, 54), R(4, 1, 4, 65), R(5, 1, 5, 31));

    // Monaco: '/^$/ find'
    [Fact]
    public void FindMatches_EmptyLinesRegex_FindsEmptyLines() => AssertFindMatches(
        Join("This is some foo - bar text which contains foo and bar - as in Barcelona.", "", "And here's a dull line with nothing interesting in it", "", "Again nothing interesting here"),
        "^$", true, false, null, R(2, 1, 2, 1), R(4, 1, 4, 1));

    private static readonly string[] Text4 = ["Just some text text", "Just some text text", "some text again", "again some text"];

    // Monaco: 'multiline find 1'
    [Fact]
    public void FindMatches_Multiline1() => AssertFindMatches(Join(Text4), @"text\n", true, false, null, R(1, 16, 2, 1), R(2, 16, 3, 1));

    // Monaco: 'multiline find 2'
    [Fact]
    public void FindMatches_Multiline2() => AssertFindMatches(Join(Text4), @"text\nJust", true, false, null, R(1, 16, 2, 5));

    // Monaco: 'multiline find 3'
    [Fact]
    public void FindMatches_Multiline3() => AssertFindMatches(Join(Text4), @"\nagain", true, false, null, R(3, 16, 4, 6));

    // Monaco: 'multiline find 4'
    [Fact]
    public void FindMatches_Multiline4() => AssertFindMatches(Join(Text4), @".*\nJust.*\n", true, false, null, R(1, 1, 3, 1));

    // Monaco: 'multiline find with line beginning regex'
    [Fact]
    public void FindMatches_MultilineWithLineBeginning() => AssertFindMatches(
        Join("if", "else", "", "if", "else"), @"^if\nelse", true, false, null, R(1, 1, 2, 5), R(4, 1, 5, 5));

    // Monaco: 'matching empty lines using boundary expression'
    [Fact]
    public void FindMatches_EmptyLinesUsingBoundaryExpression() => AssertFindMatches(
        Join("if", "", "else", "  ", "if", " ", "else"), @"^\s*$\n", true, false, null, R(2, 1, 3, 1), R(4, 1, 5, 1), R(6, 1, 7, 1));

    // Monaco: 'matching lines starting with A and ending with B'
    [Fact]
    public void FindMatches_LinesStartingWithAAndEndingWithB() => AssertFindMatches(
        Join("a if b", "a", "ab", "eb"), "^a.*b$", true, false, null, R(1, 1, 1, 7), R(3, 1, 3, 3));

    // Monaco: 'multiline find with line ending regex'
    [Fact]
    public void FindMatches_MultilineWithLineEnding() => AssertFindMatches(
        Join("if", "else", "", "if", "elseif", "else"), @"if\nelse$", true, false, null, R(1, 1, 2, 5), R(5, 5, 6, 5));

    // Monaco: 'issue #4836 - ^.*$'
    [Fact]
    public void FindMatches_Issue4836_CaretDotStarDollar() => AssertFindMatches(
        Join("Just some text text", "", "some text again", "", "again some text"),
        "^.*$", true, false, null, R(1, 1, 1, 20), R(2, 1, 2, 1), R(3, 1, 3, 16), R(4, 1, 4, 1), R(5, 1, 5, 16));

    // Monaco: 'multiline find for non-regex string'
    [Fact]
    public void FindMatches_MultilineNonRegexString() => AssertFindMatches(
        Join("Just some text text", "some text text", "some text again", "again some text", "but not some"),
        "text" + Lf + "some", false, false, null, R(1, 16, 2, 5), R(2, 11, 3, 5));

    // Monaco: 'issue #3623: Match whole word does not work for not latin characters'
    [Fact]
    public void FindMatches_Issue3623_WholeWordNonLatin() => AssertFindMatches(
        Join("я", "компилятор", "обфускация", ":я-я"), "я", false, false, Usual, R(1, 1, 1, 2), R(4, 2, 4, 3), R(4, 4, 4, 5));

    // Monaco: 'issue #27459: Match whole words regression'
    [Fact]
    public void FindMatches_Issue27459_WholeWordsRegression() => AssertFindMatches(
        Join("this._register(this._textAreaInput.onKeyDown((e: IKeyboardEvent) => {", "\tthis._viewController.emitKeyDown(e);", "}));"),
        "((e: ", false, false, Usual, R(1, 45, 1, 50));

    // Monaco: 'issue #27594: Search results disappear'
    [Fact]
    public void FindMatches_Issue27594_SearchResultsDisappear() => AssertFindMatches(
        "this.server.listen(0);", "listen(", false, false, Usual, R(1, 13, 1, 20));

    // Monaco: 'issue #53415. \W should match line break.'
    [Fact]
    public void FindMatches_Issue53415_NonWordMatchesLineBreak()
    {
        AssertFindMatches(Join("text", "180702-", "180703-180704"), @"\d{6}-\W", true, false, null, R(2, 1, 3, 1));

        var just = new[] { "Just some text", "", "Just" };
        AssertFindMatches(Join(just), @"\W", true, false, null, R(1, 5, 1, 6), R(1, 10, 1, 11), R(1, 15, 2, 1), R(2, 1, 3, 1));

        // Line break doesn't affect the result as we always use \n as line break when doing search
        AssertFindMatches(string.Join(Cr + Lf, just), @"\W", true, false, null, R(1, 5, 1, 6), R(1, 10, 1, 11), R(1, 15, 2, 1), R(2, 1, 3, 1));

        AssertFindMatches(Join("Just some text", "\tJust", "Just"), @"\W", true, false, null,
            R(1, 5, 1, 6), R(1, 10, 1, 11), R(1, 15, 2, 1), R(2, 1, 2, 2), R(2, 6, 3, 1));

        // line break is seen as one non-word character
        AssertFindMatches(Join("Just  some text", "", "Just"), @"\W{2}", true, false, null, R(1, 5, 1, 7), R(1, 16, 3, 1));

        // even if it's \r\n
        AssertFindMatches(string.Join(Cr + Lf, "Just  some text", "", "Just"), @"\W{2}", true, false, null, R(1, 5, 1, 7), R(1, 16, 3, 1));
    }

    // Monaco: 'issue #100134. Zero-length matches should properly step over surrogate pairs'
    [Fact]
    public void FindMatches_Issue100134_ZeroLengthMatchesStepOverSurrogatePairs()
    {
        // 1[Laptop]1 - there should be no matches inside of [Laptop] emoji
        AssertFindMatches("1" + Surrogates(0xD83D, 0xDCBB) + "1", "()", true, false, null, R(1, 1, 1, 1), R(1, 2, 1, 2), R(1, 4, 1, 4), R(1, 5, 1, 5));

        // 1[Cat Face][ZWJ][Laptop]1 - matches between emoji and ZWJ, none inside the emoji
        AssertFindMatches("1" + Surrogates(0xD83D, 0xDC31, 0x200D, 0xD83D, 0xDCBB) + "1", "()", true, false, null,
            R(1, 1, 1, 1), R(1, 2, 1, 2), R(1, 4, 1, 4), R(1, 5, 1, 5), R(1, 7, 1, 7), R(1, 8, 1, 8));
    }
}
