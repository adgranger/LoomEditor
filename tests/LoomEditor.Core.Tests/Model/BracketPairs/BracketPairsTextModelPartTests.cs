using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// The tests of the model part that getBracketPairsInRange.test.ts (GetBracketPairsInRangeTests) does not cover.
public class BracketPairsTextModelPartTests
{
    private const string LanguageId = TestLanguageModels.DefaultLanguageId;

    private static TestLanguageModel Create(string text, params CharacterPair[] brackets) =>
        TestLanguageModels.Create(text, LanguageId, new LanguageConfiguration { Brackets = brackets });

    private sealed record BracketPairInfoView(string Opening, string? Closing);

    private static BracketPairInfoView[] Pairs(TestLanguageModel test) =>
        [
            .. test.Model.BracketPairs
                .GetBracketPairsInRange(test.Model.FullModelRange)
                .ToArray()
                .Select(p => new BracketPairInfoView(p.OpeningBracketRange.ToString(), p.ClosingBracketRange?.ToString())),
        ];

    [Fact]
    public void BracketPairs_FirstRequest_BuildsTree_AndRaisesChanged()
    {
        var test = Create("{ }", new CharacterPair("{", "}"));
        var model = test.Model;
        var changed = 0;
        model.BracketPairs.Changed += (_, _) => changed++;

        model.SetValue("{ { } }");
        Assert.Equal(0, changed);

        var pairs = Pairs(test);

        Assert.Equal(1, changed);
        Assert.Equal(
            [new BracketPairInfoView("[1,1 -> 1,2]", "[1,7 -> 1,8]"), new BracketPairInfoView("[1,3 -> 1,4]", "[1,5 -> 1,6]")],
            pairs);

        Pairs(test);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void BracketPairs_LanguageChanged_RebuildsTree()
    {
        var test = TestLanguageModels.Create("{ ( ) }", LanguageId, new LanguageConfiguration { Brackets = [new("{", "}")] }, null, "other");
        test.Configurations.Register("other", new LanguageConfiguration { Brackets = [new("(", ")")] });
        var model = test.Model;
        var changed = 0;
        model.BracketPairs.Changed += (_, _) => changed++;
        Assert.Single(Pairs(test));
        Assert.Equal(1, changed);

        model.Tokenization.SetLanguageId("other");

        Assert.Equal(2, changed);
        Assert.Equal([new BracketPairInfoView("[1,3 -> 1,4]", "[1,5 -> 1,6]")], Pairs(test));
    }

    [Fact]
    public void BracketPairs_ConfigurationOfOtherLanguage_DoesNotRebuild()
    {
        var test = Create("{ }", new CharacterPair("{", "}"));
        var model = test.Model;
        Pairs(test);
        var changed = 0;
        model.BracketPairs.Changed += (_, _) => changed++;

        test.Configurations.Register("someOtherLanguage", new LanguageConfiguration { Brackets = [new("(", ")")] });

        Assert.Equal(0, changed);

        test.Configurations.Register(LanguageId, new LanguageConfiguration { Brackets = [new("{", "}"), new("[", "]")] }, priority: 1);

        Assert.Equal(1, changed);
    }

    [Fact]
    public void MatchBracket_InsideBracket_ReturnsOpenThenClose()
    {
        var model = Create("a { b { } } c", new CharacterPair("{", "}")).Model;
        (Range, Range)? outer = (new Range(1, 3, 1, 4), new Range(1, 11, 1, 12));
        (Range, Range)? inner = (new Range(1, 7, 1, 8), new Range(1, 9, 1, 10));

        Assert.Equal(outer, model.BracketPairs.MatchBracket(new Position(1, 3)));
        Assert.Equal(outer, model.BracketPairs.MatchBracket(new Position(1, 4)));
        Assert.Equal(outer, model.BracketPairs.MatchBracket(new Position(1, 12)));
        Assert.Equal(inner, model.BracketPairs.MatchBracket(new Position(1, 7)));
        Assert.Equal(inner, model.BracketPairs.MatchBracket(new Position(1, 10)));
        Assert.Null(model.BracketPairs.MatchBracket(new Position(1, 1)));
        Assert.Null(model.BracketPairs.MatchBracket(new Position(1, 6)));
    }

    [Fact]
    public void FindEnclosingBrackets_ReturnsInnermostClosedPair()
    {
        var model = Create("{ [ a ] ( ) } (", new CharacterPair("{", "}"), new CharacterPair("[", "]"), new CharacterPair("(", ")")).Model;
        (Range, Range)? bracket = (new Range(1, 3, 1, 4), new Range(1, 7, 1, 8));
        (Range, Range)? brace = (new Range(1, 1, 1, 2), new Range(1, 13, 1, 14));

        Assert.Equal(bracket, model.BracketPairs.FindEnclosingBrackets(new Position(1, 5)));
        Assert.Equal(brace, model.BracketPairs.FindEnclosingBrackets(new Position(1, 8)));
        Assert.Equal(brace, model.BracketPairs.FindEnclosingBrackets(new Position(1, 2)));
        Assert.Null(model.BracketPairs.FindEnclosingBrackets(new Position(1, 1)));
        Assert.Null(model.BracketPairs.FindEnclosingBrackets(new Position(1, 16)));
    }

    [Fact]
    public void FindMatchingBracketUp_ClosingBracket_ReturnsTheOpeningBracketOfTheEnclosingPair()
    {
        var model = Create("{ [ a ] }", new CharacterPair("{", "}"), new CharacterPair("[", "]")).Model;

        Assert.Equal(new Range(1, 3, 1, 4), model.BracketPairs.FindMatchingBracketUp("]", new Position(1, 6)));
        Assert.Equal(new Range(1, 1, 1, 2), model.BracketPairs.FindMatchingBracketUp("}", new Position(1, 6)));
        Assert.Null(model.BracketPairs.FindMatchingBracketUp(")", new Position(1, 6)));
    }

    [Fact]
    public void FindPrevBracket_And_FindNextBracket_UseTheTree()
    {
        var model = Create("a { b } c", new CharacterPair("{", "}")).Model;

        Assert.Equal(new Range(1, 3, 1, 4), model.BracketPairs.FindPrevBracket(new Position(1, 6))?.Range);
        Assert.Equal(new Range(1, 7, 1, 8), model.BracketPairs.FindNextBracket(new Position(1, 6))?.Range);
        Assert.Null(model.BracketPairs.FindPrevBracket(new Position(1, 3)));
        Assert.Null(model.BracketPairs.FindNextBracket(new Position(1, 8)));
    }

    [Fact]
    public void BracketPairs_TextEdited_TreeFollowsTheText()
    {
        var test = Create("{ }", new CharacterPair("{", "}"));
        Assert.Single(Pairs(test));

        test.Model.ApplyEdits([new TextEdit(new Range(1, 2, 1, 2), " { }")]);

        Assert.Equal(
            [new BracketPairInfoView("[1,1 -> 1,2]", "[1,7 -> 1,8]"), new BracketPairInfoView("[1,3 -> 1,4]", "[1,5 -> 1,6]")],
            Pairs(test));
    }

    [Fact]
    public void BracketPairs_OptionsChanged_RebuildsTree()
    {
        var model = Create("{\n\ta\n\t\tb\n\t\t}", new CharacterPair("{", "}")).Model;
        Assert.Equal([4], model.BracketPairs.GetBracketPairsInRangeWithMinIndentation(model.FullModelRange).ToArray().Select(p => p.MinVisibleColumnIndentation));
        var changed = 0;
        model.BracketPairs.Changed += (_, _) => changed++;

        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));

        Assert.Equal(1, changed);
        Assert.Equal([8], model.BracketPairs.GetBracketPairsInRangeWithMinIndentation(model.FullModelRange).ToArray().Select(p => p.MinVisibleColumnIndentation));
    }

    [Fact]
    public void FindMatchingBracketUp_Legacy_FindsOpeningBracket()
    {
        var model = Create("{\n  (\n  )\n}", new CharacterPair("{", "}"), new CharacterPair("(", ")")).Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;

        Assert.Equal(new Range(1, 1, 1, 2), model.BracketPairs.FindMatchingBracketUp("}", new Position(4, 1)));
        Assert.Null(model.BracketPairs.FindMatchingBracketUp("]", new Position(4, 1)));
    }

    [Fact]
    public void FindMatchingBracketUp_Tree_FindsOpeningBracket()
    {
        var model = Create("{\n  (\n  )\n}", new CharacterPair("{", "}"), new CharacterPair("(", ")")).Model;

        Assert.Equal(new Range(1, 1, 1, 2), model.BracketPairs.FindMatchingBracketUp("}", new Position(4, 1)));
        Assert.Null(model.BracketPairs.FindMatchingBracketUp("]", new Position(4, 1)));
    }

    [Fact]
    public void MatchBracket_Legacy_MaxDurationZero_ReturnsNull()
    {
        string text = "(\n" + string.Concat(Enumerable.Repeat("x\n", 2000)) + ")";
        var model = Create(text, new CharacterPair("(", ")")).Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;
        var lastLine = model.LineCount;
        Assert.Equal(2002, lastLine);

        // The search is canceled at its 100th call, which is on the 100th line.
        Assert.Null(model.BracketPairs.MatchBracket(new Position(lastLine, 1), TimeSpan.Zero));
        Assert.Null(model.BracketPairs.MatchBracket(new Position(1, 1), TimeSpan.Zero));
        Assert.Null(model.BracketPairs.FindMatchingBracketUp(")", new Position(lastLine, 1), TimeSpan.Zero));

        // The legacy search answers the bracket at the position first.
        (Range, Range)? pair = (new Range(1, 1, 1, 2), new Range(lastLine, 1, lastLine, 2));
        Assert.Equal(((Range, Range)?)(pair.Value.Item2, pair.Value.Item1), model.BracketPairs.MatchBracket(new Position(lastLine, 1)));
        Assert.Equal(pair, model.BracketPairs.MatchBracket(new Position(1, 1)));
        Assert.Equal(pair, model.BracketPairs.MatchBracket(new Position(1, 1), TimeSpan.FromMinutes(1)));
        Assert.Equal(new Range(1, 1, 1, 2), model.BracketPairs.FindMatchingBracketUp(")", new Position(lastLine, 1)));
    }

    [Fact]
    public void MatchBracket_Legacy_BracketInComment_IsIgnored()
    {
        const string text = "( /* ) */ )";
        var test = TestLanguageModels.Create(
            text,
            LanguageId,
            new LanguageConfiguration { Brackets = [new("(", ")")] },
            codec =>
            {
                uint Metadata(StandardTokenType type) =>
                    ((uint)codec.EncodeLanguageId(LanguageId) << MetadataConsts.LanguageIdOffset)
                    | ((uint)type << MetadataConsts.TokenTypeOffset);
                return new LineTokensSupport(new Dictionary<string, uint[]>
                {
                    [text] = [0, Metadata(StandardTokenType.Other), 2, Metadata(StandardTokenType.Comment), 9, Metadata(StandardTokenType.Other)],
                });
            });
        var model = test.Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;
        model.Tokenization.ForceTokenization(1);

        (Range, Range)? pair = (new Range(1, 1, 1, 2), new Range(1, 11, 1, 12));
        Assert.Equal(pair, model.BracketPairs.MatchBracket(new Position(1, 1)));
        Assert.Equal(((Range, Range)?)(pair.Value.Item2, pair.Value.Item1), model.BracketPairs.MatchBracket(new Position(1, 11)));
        Assert.Null(model.BracketPairs.MatchBracket(new Position(1, 6)));
    }

    [Fact]
    public void MaxSupportedDocumentLength_Exceeded_TreeIsDropped_AndChangedRaised()
    {
        var test = Create("{ [ ] }", new CharacterPair("{", "}"), new CharacterPair("[", "]"));
        var model = test.Model;
        var part = model.BracketPairs;
        Assert.Equal(2, Pairs(test).Length);
        var changed = 0;
        part.Changed += (_, _) => changed++;

        part.MaxSupportedDocumentLength = 3;

        Assert.Empty(Pairs(test));
        Assert.Equal(1, changed);
        Assert.Empty(part.GetBracketsInRange(model.FullModelRange).ToArray());
        Assert.Null(part.FindPrevBracket(new Position(1, 5)));
        Assert.Equal(((Range, Range)?)(new Range(1, 1, 1, 2), new Range(1, 7, 1, 8)), part.MatchBracket(new Position(1, 1)));
        Assert.Equal(1, changed);

        part.MaxSupportedDocumentLength = 50_000 * 100;

        Assert.Equal(2, Pairs(test).Length);
        Assert.Equal(2, changed);
    }
}
