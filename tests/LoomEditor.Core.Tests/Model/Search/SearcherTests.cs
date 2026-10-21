using System.Text.RegularExpressions;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.Search;

public class SearcherTests
{
    private static Searcher Create(string pattern, WordCharacterClassifier? ws = null)
        => new(ws, new Regex(pattern, RegexOptions.CultureInvariant));

    private static List<int> AllIndexes(Searcher searcher, string text)
    {
        searcher.Reset(0);
        List<int> result = [];
        Match? m;
        while ((m = searcher.Next(text)) is not null)
        {
            result.Add(m.Index);
        }

        return result;
    }

    [Fact]
    public void Searcher_EmptyMatchBeforeSurrogatePair_AdvancesByTwo()
    {
        var text = "a😀b";

        // Empty matches at 0, 1, then 3 (skips the middle of the pair), then 4 (end of text).
        Assert.Equal([0, 1, 3, 4], AllIndexes(Create("(?=)"), text));
    }

    [Fact]
    public void Searcher_MatchEndingAtTextEnd_ReturnsNullAfterwards()
    {
        // Deviation from the card name: a non-empty match always advances the index, so the "same range twice"
        // exit cannot be reached from outside; this covers the "reached the end of the line" exit instead.
        var searcher = Create("a");
        searcher.Reset(0);
        Assert.Equal(2, searcher.Next("Xxa")?.Index);

        Assert.Null(searcher.Next("Xxa"));
    }

    [Fact]
    public void Searcher_WordSeparators_SkipsMatchInsideWord()
    {
        var ws = WordCharacterClassifierCache.Get(WordHelper.UsualWordSeparators, []);

        Assert.Equal([7],AllIndexes(Create("foo", ws), "afoob, foo"));
    }

    [Fact]
    public void Searcher_LookbehindSeesTextBeforeLastIndex()
    {
        var searcher = Create("(?<=a)b");
        searcher.Reset(1);

        Assert.Equal(1, searcher.Next("ab")?.Index);
    }
}
