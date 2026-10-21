using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tests.Model.Search.TestHelpers;

namespace LoomEditor.Core.Tests.Model.Search;

public partial class TextModelSearchTests
{
    private static string Text(params string[] lines) => string.Join(Lf, lines);

    private static FindMatch? Next(TextModel model, SearchParams p, Position start, bool capture = false)
        => TextModelSearch.FindNextMatch(model, p, start, capture);

    // Monaco: 'findNextMatch without regex'
    [Fact]
    public void FindNextMatch_WithoutRegex_StepsAndWraps()
    {
        var model = new TextModel(Text("line line one", "line two", "three"));
        var p = new SearchParams("line", false, false, null);

        var actual = Next(model, p, new Position(1, 1));
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(1, 6, 1, 10));

        actual = Next(model, p, new Position(1, 3));
        SearchAssert.Match(actual, new Range(1, 6, 1, 10));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(2, 1, 2, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));
    }

    // Monaco: 'findNextMatch with beginning boundary regex'
    [Fact]
    public void FindNextMatch_BeginningBoundaryRegex_StepsAndWraps()
    {
        var model = new TextModel(Text("line one", "line two", "three"));
        var p = new SearchParams("^line", true, false, null);

        var actual = Next(model, p, new Position(1, 1));
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(2, 1, 2, 5));

        actual = Next(model, p, new Position(1, 3));
        SearchAssert.Match(actual, new Range(2, 1, 2, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));
    }

    // Monaco: 'findNextMatch with beginning boundary regex and line has repetitive beginnings'
    [Fact]
    public void FindNextMatch_BeginningBoundaryRegexRepetitiveBeginnings_StepsAndWraps()
    {
        var model = new TextModel(Text("line line one", "line two", "three"));
        var p = new SearchParams("^line", true, false, null);

        var actual = Next(model, p, new Position(1, 1));
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(2, 1, 2, 5));

        actual = Next(model, p, new Position(1, 3));
        SearchAssert.Match(actual, new Range(2, 1, 2, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(1, 1, 1, 5));
    }

    // Monaco: 'findNextMatch with beginning boundary multiline regex and line has repetitive beginnings'
    [Fact]
    public void FindNextMatch_BeginningBoundaryMultilineRegex_StepsLineByLine()
    {
        var model = new TextModel(Text("line line one", "line two", "line three", "line four"));
        var p = new SearchParams(@"^line.*\nline", true, false, null);

        var actual = Next(model, p, new Position(1, 1));
        SearchAssert.Match(actual, new Range(1, 1, 2, 5));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(3, 1, 4, 5));

        actual = Next(model, p, new Position(2, 1));
        SearchAssert.Match(actual, new Range(2, 1, 3, 5));
    }

    // Monaco: 'findNextMatch with ending boundary regex'
    [Fact]
    public void FindNextMatch_EndingBoundaryRegex_StepsAndWraps()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams("line$", true, false, null);

        var actual = Next(model, p, new Position(1, 1));
        SearchAssert.Match(actual, new Range(1, 10, 1, 14));

        actual = Next(model, p, new Position(1, 4));
        SearchAssert.Match(actual, new Range(1, 10, 1, 14));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(2, 5, 2, 9));

        actual = Next(model, p, actual!.Range.End);
        SearchAssert.Match(actual, new Range(1, 10, 1, 14));
    }

    // Monaco: 'findMatches with capturing matches'
    [Fact]
    public void FindMatches_CapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams("(l(in)e)", true, false, null);

        var actual = TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 100);

        SearchAssert.Matches(actual,
        [
            new FindMatch(new Range(1, 5, 1, 9), ["line", "line", "in"]),
            new FindMatch(new Range(1, 10, 1, 14), ["line", "line", "in"]),
            new FindMatch(new Range(2, 5, 2, 9), ["line", "line", "in"]),
        ], "capture");
    }

    // Monaco: 'findMatches multiline with capturing matches'
    [Fact]
    public void FindMatches_MultilineCapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams(@"(l(in)e)\n", true, false, null);

        var actual = TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 100);

        SearchAssert.Matches(actual,
        [
            new FindMatch(new Range(1, 10, 2, 1), ["line" + Lf, "line", "in"]),
            new FindMatch(new Range(2, 5, 3, 1), ["line" + Lf, "line", "in"]),
        ], "capture");
    }

    // Monaco: 'findNextMatch with capturing matches'
    [Fact]
    public void FindNextMatch_CapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams("(l(in)e)", true, false, null);

        SearchAssert.Match(Next(model, p, new Position(1, 1), true), new Range(1, 5, 1, 9), ["line", "line", "in"]);
    }

    // Monaco: 'findNextMatch multiline with capturing matches'
    [Fact]
    public void FindNextMatch_MultilineCapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams(@"(l(in)e)\n", true, false, null);

        SearchAssert.Match(Next(model, p, new Position(1, 1), true), new Range(1, 10, 2, 1), ["line" + Lf, "line", "in"]);
    }

    // Monaco: 'findPreviousMatch with capturing matches'
    [Fact]
    public void FindPreviousMatch_CapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams("(l(in)e)", true, false, null);

        var actual = TextModelSearch.FindPreviousMatch(model, p, new Position(1, 1), true);

        SearchAssert.Match(actual, new Range(2, 5, 2, 9), ["line", "line", "in"]);
    }

    // Monaco: 'findPreviousMatch multiline with capturing matches'
    [Fact]
    public void FindPreviousMatch_MultilineCapturingMatches_ReturnsGroups()
    {
        var model = new TextModel(Text("one line line", "two line", "three"));
        var p = new SearchParams(@"(l(in)e)\n", true, false, null);

        var actual = TextModelSearch.FindPreviousMatch(model, p, new Position(1, 1), true);

        SearchAssert.Match(actual, new Range(2, 5, 3, 1), ["line" + Lf, "line", "in"]);
    }

    private static TextModel CrLfModel() => new(string.Join(Cr + Lf, "a", "b", "c", "d", "e", "f", "g", "h", "i"));

    // Monaco: '\n matches \r\n'
    [Fact]
    public void FindMatches_LfMatchesCrLf_ReturnsModelRanges()
    {
        var model = CrLfModel();
        Assert.Equal(Cr + Lf, model.Eol);

        var p = new SearchParams(@"h\n", true, false, null);
        SearchAssert.Match(TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 1000)[0], new Range(8, 1, 9, 1), ["h" + Lf]);

        p = new SearchParams(@"g\nh\n", true, false, null);
        SearchAssert.Match(TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 1000)[0], new Range(7, 1, 9, 1), ["g" + Lf + "h" + Lf]);

        p = new SearchParams(@"\ni", true, false, null);
        SearchAssert.Match(TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 1000)[0], new Range(8, 2, 9, 2), [Lf + "i"]);
    }

    // Monaco: '\r can never be found'
    [Fact]
    public void FindMatches_CarriageReturn_IsNeverFound()
    {
        var model = CrLfModel();
        var p = new SearchParams(@"\r\n", true, false, null);

        Assert.Null(Next(model, p, new Position(1, 1), true));
        Assert.Empty(TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 1000));
    }

    // Monaco: 'issue #74715. \d* finds empty string and stops searching.'
    [Fact]
    public void FindMatches_Issue74715_DigitsStarFindsEmptyStringAndStops()
    {
        var model = new TextModel("10.243.30.10");
        var p = new SearchParams(@"\d*", true, false, null);

        var actual = TextModelSearch.FindMatches(model, p, model.FullModelRange, true, 100);

        SearchAssert.Matches(actual,
        [
            new FindMatch(new Range(1, 1, 1, 3), ["10"]),
            new FindMatch(new Range(1, 3, 1, 3), [""]),
            new FindMatch(new Range(1, 4, 1, 7), ["243"]),
            new FindMatch(new Range(1, 7, 1, 7), [""]),
            new FindMatch(new Range(1, 8, 1, 10), ["30"]),
            new FindMatch(new Range(1, 10, 1, 10), [""]),
            new FindMatch(new Range(1, 11, 1, 13), ["10"]),
        ], "issue 74715");
    }
}
