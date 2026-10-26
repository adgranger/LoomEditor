// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/completionModel.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using static LoomEditor.View.Tests.Suggest.TestHelpers.TestSuggestItems;

namespace LoomEditor.View.Tests.Suggest;

[Collection("Fuzzy")] // Fuzzy uses shared static tables: not thread-safe
public class CompletionModelTests
{
    private static readonly SuggestModelOptions Defaults = SuggestModelOptions.Default;

    private CompletionModel _model = new(
        [CreateSuggestItem("foo", 3), CreateSuggestItem("Foo", 3), CreateSuggestItem("foo", 2)],
        1 + ColumnShift,
        new CompletionLineContext("foo", 0),
        Defaults);

    private static CompletionModel Model(SuggestItem[] items, int column, string leading, int delta, SuggestModelOptions? options = null)
        => new(items, column + ColumnShift, new CompletionLineContext(leading, delta), options ?? Defaults);

    private static string[] Labels(CompletionModel model) => [.. model.Items.Select(x => x.Completion.Label)];

    // Monaco: 'filtering - cached'
    [Fact]
    public void Filtering_Cached()
    {
        IReadOnlyList<SuggestItem> itemsNow = _model.Items;
        IReadOnlyList<SuggestItem> itemsThen = _model.Items;
        Assert.Same(itemsNow, itemsThen);

        // still the same context
        _model.LineContext = new CompletionLineContext("foo", 0);
        itemsThen = _model.Items;
        Assert.Same(itemsNow, itemsThen);

        // different context, refilter
        _model.LineContext = new CompletionLineContext("foo1", 1);
        itemsThen = _model.Items;
        Assert.NotSame(itemsNow, itemsThen);
    }

    // Monaco: 'complete/incomplete'
    [Fact]
    public void Complete_Incomplete()
    {
        Assert.Empty(_model.GetIncompleteProvider());

        var incompleteModel = Model([CreateSuggestItem("foo", 3, incomplete: true), CreateSuggestItem("foo", 2)], 1, "foo", 0);
        Assert.Single(incompleteModel.GetIncompleteProvider());
    }

    // Monaco: 'Fuzzy matching of snippets stopped working with inline snippet suggestions #49895'
    [Fact]
    public void Issue49895_FuzzyMatchingOfSnippets()
    {
        var at = new Position(1, 2);
        var model = Model(
            [
                CreateSuggestItem("foobar1", 1, position: at),
                CreateSuggestItem("foobar2", 1, position: at),
                CreateSuggestItem("foobar3", 1, position: at),
                CreateSuggestItem("foobar4", 1, position: at),
                CreateSuggestItem("foobar5", 1, position: at),
                CreateSuggestItem("foofoo1", 1, incomplete: true, position: at),
            ],
            2,
            "f",
            0);
        Assert.Single(model.GetIncompleteProvider());
        Assert.Equal(6, model.Items.Count);
    }

    // Monaco: 'proper current word when length=0, #16380'
    [Fact]
    public void Issue16380_ProperCurrentWordWhenLengthIsZero()
    {
        _model = Model(
            [
                CreateSuggestItem("    </div", 4),
                CreateSuggestItem("a", 0),
                CreateSuggestItem("p", 0),
                CreateSuggestItem("    </tag", 4),
                CreateSuggestItem("    XYZ", 4),
            ],
            1,
            "   <",
            0);

        Assert.Equal(["    </div", "    </tag", "a", "p"], Labels(_model));
    }

    // Monaco: 'keep snippet sorting with prefix: top, #25495'
    [Fact]
    public void Issue25495_SnippetSortingTop()
    {
        _model = Model(
            [
                CreateSuggestItem("Snippet1", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("tnippet2", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("semver", 1, CompletionItemKind.Property),
            ],
            1,
            "s",
            0,
            new SuggestModelOptions(Snippets: SnippetSuggestions.Top));

        Assert.Equal(2, _model.Items.Count);
        SuggestItem a = _model.Items[0];
        SuggestItem b = _model.Items[1];
        Assert.Equal("Snippet1", a.Completion.Label);
        Assert.Equal("semver", b.Completion.Label);
        Assert.True(a.Score[0] < b.Score[0]); // snippet really promoted
    }

    // Monaco: 'keep snippet sorting with prefix: bottom, #25495'
    [Fact]
    public void Issue25495_SnippetSortingBottom()
    {
        _model = Model(
            [
                CreateSuggestItem("snippet1", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("tnippet2", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("Semver", 1, CompletionItemKind.Property),
            ],
            1,
            "s",
            0,
            new SuggestModelOptions(Snippets: SnippetSuggestions.Bottom));

        Assert.Equal(2, _model.Items.Count);
        SuggestItem a = _model.Items[0];
        SuggestItem b = _model.Items[1];
        Assert.Equal("Semver", a.Completion.Label);
        Assert.Equal("snippet1", b.Completion.Label);
        Assert.True(a.Score[0] < b.Score[0]); // snippet really demoted
    }

    // Monaco: 'keep snippet sorting with prefix: inline, #25495'
    [Fact]
    public void Issue25495_SnippetSortingInline()
    {
        _model = Model(
            [
                CreateSuggestItem("snippet1", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("tnippet2", 1, CompletionItemKind.Snippet),
                CreateSuggestItem("Semver", 1),
            ],
            1,
            "s",
            0,
            new SuggestModelOptions(Snippets: SnippetSuggestions.Inline));

        Assert.Equal(2, _model.Items.Count);
        SuggestItem a = _model.Items[0];
        SuggestItem b = _model.Items[1];
        Assert.Equal("snippet1", a.Completion.Label);
        Assert.Equal("Semver", b.Completion.Label);
        Assert.True(a.Score[0] > b.Score[0]); // snippet really demoted
    }

    // Monaco: 'filterText seems ignored in autocompletion, #26874'
    [Fact]
    public void Issue26874_FilterTextIgnored()
    {
        SuggestItem item1 = CreateSuggestItem("Map - java.util", 1, filterText: "Map");
        SuggestItem item2 = CreateSuggestItem("Map - java.util", 1);

        _model = Model([item1, item2], 1, "M", 0);
        Assert.Equal(2, _model.Items.Count);

        _model.LineContext = new CompletionLineContext("Map ", 3);
        Assert.Single(_model.Items);
    }

    // Monaco: 'Vscode 1.12 no longer obeys 'sortText' in completion items (from language server), #26096'
    [Fact]
    public void Issue26096_SortText()
    {
        SuggestItem item1 = CreateSuggestItem("<- groups", 2, CompletionItemKind.Property, false, new Position(1, 3), "00002", "  groups");
        SuggestItem item2 = CreateSuggestItem("source", 0, CompletionItemKind.Property, false, new Position(1, 3), "00001", "source");
        SuggestItem[] items = [.. new[] { item1, item2 }.OrderBy(x => x, SuggestItemComparer.Default)];

        _model = Model(items, 3, "  ", 0);

        Assert.Equal(["source", "<- groups"], Labels(_model));
    }

    // Monaco: 'Completion item sorting broken when using label details #153026'
    [Fact]
    public void Issue153026_LabelDetailsSorting()
    {
        SuggestItem itemZzz = CreateSuggestItem("ZZZ", 0, CompletionItemKind.Operator, labelDetail: "x");
        SuggestItem itemAaa = CreateSuggestItem("AAA", 0, CompletionItemKind.Operator, labelDetail: "x");
        SuggestItem itemIii = CreateSuggestItem("III", 0, CompletionItemKind.Operator);

        SuggestItem[] actual = [.. new[] { itemZzz, itemAaa, itemIii }.OrderBy(x => x, SuggestItemComparer.Default)];

        Assert.Equal([itemAaa, itemIii, itemZzz], actual);
    }

    // Monaco: 'Score only filtered items when typing more, score all when typing less'
    [Fact]
    public void ScoreOnlyFilteredItemsWhenTypingMore_ScoreAllWhenTypingLess()
    {
        _model = Model(
            [
                CreateSuggestItem("console", 0),
                CreateSuggestItem("co_new", 0),
                CreateSuggestItem("bar", 0),
                CreateSuggestItem("car", 0),
                CreateSuggestItem("foo", 0),
            ],
            1,
            string.Empty,
            0);
        Assert.Equal(5, _model.Items.Count);

        // narrow down once
        _model.LineContext = new CompletionLineContext("c", 1);
        Assert.Equal(3, _model.Items.Count);

        // query gets longer, narrow down the narrow-down'ed-set from before
        _model.LineContext = new CompletionLineContext("cn", 2);
        Assert.Equal(2, _model.Items.Count);

        // query gets shorter, refilter everything
        _model.LineContext = new CompletionLineContext(string.Empty, 0);
        Assert.Equal(5, _model.Items.Count);
    }

    // Monaco: 'Have more relaxed suggest matching algorithm #15419'
    [Fact]
    public void Issue15419_RelaxedMatching()
    {
        _model = Model(
            [
                CreateSuggestItem("result", 0),
                CreateSuggestItem("replyToUser", 0),
                CreateSuggestItem("randomLolut", 0),
                CreateSuggestItem("car", 0),
                CreateSuggestItem("foo", 0),
            ],
            1,
            string.Empty,
            0);

        // query gets longer, narrow down the narrow-down'ed-set from before
        _model.LineContext = new CompletionLineContext("rlut", 4);
        Assert.Equal(["result", "replyToUser", "randomLolut"], Labels(_model)); // best with `rult`, `rltu`, `rlut`
    }

    // Monaco: 'Emmet suggestion not appearing at the top of the list in jsx files, #39518'
    [Fact]
    public void Issue39518_EmmetSuggestionAtTheTop()
    {
        _model = Model(
            [
                CreateSuggestItem("from", 0),
                CreateSuggestItem("form", 0),
                CreateSuggestItem("form:get", 0),
                CreateSuggestItem("testForeignMeasure", 0),
                CreateSuggestItem("fooRoom", 0),
            ],
            1,
            string.Empty,
            0);

        _model.LineContext = new CompletionLineContext("form", 4);
        Assert.Equal(5, _model.Items.Count);
        Assert.Equal(["form", "form:get", "from"], Labels(_model).Take(3));
    }

    [Fact]
    public void LineContext_Unchanged_KeepsCache()
    {
        IReadOnlyList<SuggestItem> before = _model.Items;
        _model.LineContext = new CompletionLineContext("foo", 0);
        Assert.Same(before, _model.Items);
    }

    [Fact]
    public void LineContext_Longer_RefiltersOnlyFilteredItems()
    {
        var calls = 0;
        FuzzyScorer counting = (p, lp, pp, w, lw, wp, o) =>
        {
            calls++;
            return Fuzzy.Score(p, lp, pp, w, lw, wp, o);
        };
        var model = new CompletionModel(
            [
                CreateSuggestItem("console", 0),
                CreateSuggestItem("co_new", 0),
                CreateSuggestItem("car", 0),
                CreateSuggestItem("bar", 0),
                CreateSuggestItem("foo", 0),
            ],
            1 + ColumnShift,
            new CompletionLineContext("c", 1),
            Defaults,
            null,
            counting);

        Assert.Equal(3, model.Items.Count);
        Assert.Equal(5, calls);

        calls = 0;
        model.LineContext = new CompletionLineContext("co", 2);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal(3, calls); // only the 3 survivors are scored again
    }

    [Fact]
    public void LineContext_Shorter_RefiltersAll()
    {
        var calls = 0;
        FuzzyScorer counting = (p, lp, pp, w, lw, wp, o) =>
        {
            calls++;
            return Fuzzy.Score(p, lp, pp, w, lw, wp, o);
        };
        var model = new CompletionModel(
            [CreateSuggestItem("console", 0), CreateSuggestItem("car", 0), CreateSuggestItem("foo", 0)],
            1 + ColumnShift,
            new CompletionLineContext("co", 2),
            Defaults,
            null,
            counting);
        Assert.Single(model.Items);

        calls = 0;
        model.LineContext = new CompletionLineContext("c", 1);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal(3, calls);
    }

    [Fact]
    public void InvalidItem_Skipped()
    {
        SuggestItem invalid = CreateSuggestItem(string.Empty, 0);
        Assert.True(invalid.IsInvalid);
        var model = Model([invalid, CreateSuggestItem("ok", 0)], 1, string.Empty, 0);
        Assert.Equal(["ok"], Labels(model));
    }

    [Fact]
    public void GetItemsByProvider_GroupsByProvider()
    {
        var byProvider = _model.GetItemsByProvider();
        Assert.Equal(3, byProvider.Count); // each helper item has its own provider
        Assert.All(byProvider.Values, v => Assert.Single(v));
    }

    [Fact]
    public void GetIncompleteProvider_OnlyIncomplete()
    {
        SuggestItem incomplete = CreateSuggestItem("a", 0, incomplete: true);
        var model = Model([incomplete, CreateSuggestItem("b", 0)], 1, string.Empty, 0);
        Assert.Equal([incomplete.Provider], model.GetIncompleteProvider());
    }

    [Fact]
    public void FilterText_UsedForMatching_LabelForHighlights()
    {
        SuggestItem item = CreateSuggestItem("Map - java.util", 1, filterText: "Map");
        var model = Model([item], 1, "M", 0);

        Assert.Single(model.Items);
        Assert.True(item.Score.Length > 2);
    }

    [Fact]
    public void GracefulFilter_Off_NoTypoMatch()
    {
        SuggestItem[] Items() => [CreateSuggestItem("result", 0)];
        var graceful = Model(Items(), 1, "rlut", 4);
        var strict = Model(Items(), 1, "rlut", 4, new SuggestModelOptions(FilterGraceful: false));

        Assert.Single(graceful.Items);
        Assert.Empty(strict.Items);
    }

    [Fact]
    public void OverLongSource_UsesPlainScorer()
    {
        FuzzyScorer plain = Fuzzy.Score;
        FuzzyScorer graceful = Fuzzy.ScoreGracefulAggressive;

        Assert.Equal(graceful, CompletionModel.ChooseScorer(Defaults, 2000));
        Assert.Equal(plain, CompletionModel.ChooseScorer(Defaults, 2001));
        Assert.Equal(plain, CompletionModel.ChooseScorer(new SuggestModelOptions(FilterGraceful: false), 1));
    }

    [Fact]
    public void WordLongerThanLine_DoesNotThrow()
    {
        var model = Model([CreateSuggestItem("foo", 10, position: new Position(1, 11))], 11, "fo", 5);
        _ = model.Items;
        model.LineContext = new CompletionLineContext("fo", -3);
        _ = model.Items;
    }
}
