using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// Monaco: suite 'Bracket Pair Colorizer - getBracketPairsInRange' (getBracketPairsInRange.test.ts).
public class GetBracketPairsInRangeTests
{
    private const string LanguageId = TestLanguageModels.DefaultLanguageId;

    private sealed record PairJson(int Level, string Range, string OpenRange, string? CloseRange);

    private sealed record BracketJson(int Level, int LevelEqualBracketType, string Range);

    private static TestLanguageModel CreateModel(
        string text,
        string languageId,
        LanguageConfiguration configuration,
        Func<LanguageIdCodec, ITokenizationSupport>? createSupport = null,
        params string[] otherLanguages) =>
        TestLanguageModels.Create(text, languageId, configuration, createSupport, otherLanguages);

    // Monaco: createTextModelWithColorizedBracketPairs
    private static TextModel CreateTextModelWithColorizedBracketPairs(string text)
    {
        return CreateModel(
            text,
            LanguageId,
            new LanguageConfiguration
            {
                Brackets = [new("<", ">")],
                ColorizedBracketPairs = [new("{", "}"), new("[", "]"), new("(", ")")],
            },
            codec =>
            {
                var document = new TokenizedDocument([new TokenInfo(text, codec.EncodeLanguageId(LanguageId), StandardTokenType.Other, true)]);
                return document.GetTokenizationSupport();
            }).Model;
    }

    // Monaco: bracketPairToJSON
    private static PairJson ToJson(BracketPairInfo pair) =>
        new(pair.NestingLevel, pair.OpeningBracketRange.ToString(), pair.OpeningBracketRange.ToString(), pair.ClosingBracketRange?.ToString());

    private static BracketJson ToJson(BracketInfo bracket) =>
        new(bracket.NestingLevel, bracket.NestingLevelOfEqualBracketType, bracket.Range.ToString());

    private static PairJson Pair(int level, string range, string closeRange) => new(level, range, range, closeRange);

    private static BracketJson Bracket(int level, int levelEqualBracketType, string range) => new(level, levelEqualBracketType, range);

    // Monaco: 'Basic 1'
    [Fact]
    public void BasicOne()
    {
        var doc = new AnnotatedDocument("{ ( [] ¹ ) [ ² { } ] () } []");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);
        model.Tokenization.GetLineTokens(1).GetLanguageId(0);

        var pairs = model.BracketPairs.GetBracketPairsInRange(doc.GetRange(1, 2)).Map(ToJson).ToArray();

        Assert.Equal(
            [
                Pair(0, "[1,1 -> 1,2]", "[1,23 -> 1,24]"),
                Pair(1, "[1,3 -> 1,4]", "[1,9 -> 1,10]"),
                Pair(1, "[1,11 -> 1,12]", "[1,18 -> 1,19]"),
            ],
            pairs);
    }

    // Monaco: 'Basic 2'
    [Fact]
    public void BasicTwo()
    {
        var doc = new AnnotatedDocument("{ ( [] ¹ ²) [  { } ] () } []");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var pairs = model.BracketPairs.GetBracketPairsInRange(doc.GetRange(1, 2)).Map(ToJson).ToArray();

        Assert.Equal(
            [
                Pair(0, "[1,1 -> 1,2]", "[1,23 -> 1,24]"),
                Pair(1, "[1,3 -> 1,4]", "[1,9 -> 1,10]"),
            ],
            pairs);
    }

    // Monaco: 'Basic Empty'
    [Fact]
    public void BasicEmpty()
    {
        var doc = new AnnotatedDocument("¹ ² { ( [] ) [  { } ] () } []");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var pairs = model.BracketPairs.GetBracketPairsInRange(doc.GetRange(1, 2)).Map(ToJson).ToArray();

        Assert.Empty(pairs);
    }

    // Monaco: 'unmatched closing brackets after position'
    [Fact]
    public void UnmatchedClosingBracketsAfterPosition()
    {
        var model = CreateModel(
            "} text }",
            LanguageId,
            new LanguageConfiguration { Brackets = [new("{", "}"), new("[", "]")] }).Model;

        bool[] actual =
        [
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "{"),
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 2), "{"),
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 9), "{"),
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "["),
        ];
        Assert.Equal([true, true, false, false], actual);
    }

    // Monaco: 'unmatched closing bracket query waits for background tokenization'
    [Fact]
    public void UnmatchedClosingBracketQueryWaitsForBackgroundTokenization()
    {
        TokenizedDocument? document = null;
        var model = CreateModel(
            "\"}\"}",
            LanguageId,
            new LanguageConfiguration { Brackets = [new("{", "}")] },
            codec =>
            {
                document = new TokenizedDocument(
                [
                    new TokenInfo("\"}\"", codec.EncodeLanguageId(LanguageId), StandardTokenType.String, true),
                    new TokenInfo("}", codec.EncodeLanguageId(LanguageId), StandardTokenType.Other, true),
                ]);
                return new WithoutBackgroundTokenization(document.GetTokenizationSupport());
            }).Model;

        model.BracketPairs.GetBracketsInRange(model.FullModelRange).ToArray();
        var beforeTokenization = model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "{");
        model.Tokenization.ForceTokenization(model.LineCount);
        var afterTokenization = model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "{");
        model.SetValue("x" + document!.GetText());

        bool[] actual = [beforeTokenization, afterTokenization, model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "{")];
        Assert.Equal([false, true, false], actual);
    }

    // Monaco: 'unmatched closing brackets are language scoped'
    [Fact]
    public void UnmatchedClosingBracketsAreLanguageScoped()
    {
        const string OuterLanguageId = "outerLanguage";
        const string InnerLanguageId = "innerLanguage";

        var test = CreateModel(
            "x}",
            OuterLanguageId,
            new LanguageConfiguration { Brackets = [new("{", "}")] },
            codec =>
            {
                var document = new TokenizedDocument(
                [
                    new TokenInfo("x", codec.EncodeLanguageId(OuterLanguageId), StandardTokenType.Other, true),
                    new TokenInfo("}", codec.EncodeLanguageId(InnerLanguageId), StandardTokenType.Other, true),
                ]);
                return document.GetTokenizationSupport();
            },
            InnerLanguageId);
        test.Configurations.Register(InnerLanguageId, new LanguageConfiguration { Brackets = [new("{", "}")] });
        var model = test.Model;
        model.Tokenization.ForceTokenization(model.LineCount);
        model.BracketPairs.GetBracketsInRange(model.FullModelRange).ToArray();

        bool[] actual =
        [
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 1), "{"),
            model.BracketPairs.HasUnmatchedClosingBracketAfter(new Position(1, 2), "{"),
        ];
        Assert.Equal([false, true], actual);
    }

    // Monaco: 'Basic All'
    [Fact]
    public void BasicAll()
    {
        var doc = new AnnotatedDocument("¹ { ( [] ) [  { } ] () } [] ²");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var pairs = model.BracketPairs.GetBracketPairsInRange(doc.GetRange(1, 2)).Map(ToJson).ToArray();

        Assert.Equal(
            [
                Pair(0, "[1,2 -> 1,3]", "[1,23 -> 1,24]"),
                Pair(1, "[1,4 -> 1,5]", "[1,9 -> 1,10]"),
                Pair(2, "[1,6 -> 1,7]", "[1,7 -> 1,8]"),
                Pair(1, "[1,11 -> 1,12]", "[1,18 -> 1,19]"),
                Pair(2, "[1,14 -> 1,15]", "[1,16 -> 1,17]"),
                Pair(1, "[1,20 -> 1,21]", "[1,21 -> 1,22]"),
                Pair(0, "[1,25 -> 1,26]", "[1,26 -> 1,27]"),
            ],
            pairs);
    }

    // Monaco: 'getBracketsInRange'
    [Fact]
    public void GetBracketsInRange()
    {
        var doc = new AnnotatedDocument("¹ { [ ( [ [ (  ) ] ] ) ] } { } ²");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var brackets = model.BracketPairs.GetBracketsInRange(doc.GetRange(1, 2)).Map(ToJson).ToArray();

        Assert.Equal(
            [
                Bracket(0, 0, "[1,2 -> 1,3]"),
                Bracket(1, 0, "[1,4 -> 1,5]"),
                Bracket(2, 0, "[1,6 -> 1,7]"),
                Bracket(3, 1, "[1,8 -> 1,9]"),
                Bracket(4, 2, "[1,10 -> 1,11]"),
                Bracket(5, 1, "[1,12 -> 1,13]"),
                Bracket(5, 1, "[1,15 -> 1,16]"),
                Bracket(4, 2, "[1,17 -> 1,18]"),
                Bracket(3, 1, "[1,19 -> 1,20]"),
                Bracket(2, 0, "[1,21 -> 1,22]"),
                Bracket(1, 0, "[1,23 -> 1,24]"),
                Bracket(0, 0, "[1,25 -> 1,26]"),
                Bracket(0, 0, "[1,27 -> 1,28]"),
                Bracket(0, 0, "[1,29 -> 1,30]"),
            ],
            brackets);
    }

    // Monaco: 'Test Error Brackets'
    [Fact]
    public void TestErrorBrackets()
    {
        var doc = new AnnotatedDocument("¹ { () ] ² ");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var brackets = model.BracketPairs
            .GetBracketsInRange(doc.GetRange(1, 2))
            .Map(b => new { Level = b.NestingLevel, Range = b.Range.ToString(), b.IsInvalid })
            .ToArray();

        Assert.Equal(
            [
                new { Level = 0, Range = "[1,2 -> 1,3]", IsInvalid = true },
                new { Level = 1, Range = "[1,4 -> 1,5]", IsInvalid = false },
                new { Level = 1, Range = "[1,5 -> 1,6]", IsInvalid = false },
                new { Level = 0, Range = "[1,7 -> 1,8]", IsInvalid = true },
            ],
            brackets);
    }

    // Monaco: 'colorizedBracketsVSBrackets'
    [Fact]
    public void ColorizedBracketsVsBrackets()
    {
        var doc = new AnnotatedDocument("¹ {} [<()>] <{>} ²");
        var model = CreateTextModelWithColorizedBracketPairs(doc.Text);

        var colorized = model.BracketPairs.GetBracketsInRange(doc.GetRange(1, 2), true).Map(ToJson).ToArray();
        var all = model.BracketPairs.GetBracketsInRange(doc.GetRange(1, 2), false).Map(ToJson).ToArray();

        Assert.Equal(
            [
                Bracket(0, 0, "[1,2 -> 1,3]"),
                Bracket(0, 0, "[1,3 -> 1,4]"),
                Bracket(0, 0, "[1,5 -> 1,6]"),
                Bracket(1, 0, "[1,7 -> 1,8]"),
                Bracket(1, 0, "[1,8 -> 1,9]"),
                Bracket(0, 0, "[1,10 -> 1,11]"),
                Bracket(0, 0, "[1,13 -> 1,14]"),
                Bracket(-1, 0, "[1,15 -> 1,16]"),
            ],
            colorized);
        Assert.Equal(
            [
                Bracket(0, 0, "[1,2 -> 1,3]"),
                Bracket(0, 0, "[1,3 -> 1,4]"),
                Bracket(0, 0, "[1,5 -> 1,6]"),
                Bracket(1, 0, "[1,6 -> 1,7]"),
                Bracket(2, 0, "[1,7 -> 1,8]"),
                Bracket(2, 0, "[1,8 -> 1,9]"),
                Bracket(1, 0, "[1,9 -> 1,10]"),
                Bracket(0, 0, "[1,10 -> 1,11]"),
                Bracket(0, 0, "[1,12 -> 1,13]"),
                Bracket(1, 0, "[1,13 -> 1,14]"),
                Bracket(0, 0, "[1,14 -> 1,15]"),
                Bracket(-1, 0, "[1,15 -> 1,16]"),
            ],
            all);
    }

    // Monaco: the getBracketPairsInRange helper `createBackgroundTokenizer: () => ({ dispose: () => { }, requestTokens: () => { } })`
    private sealed class WithoutBackgroundTokenization(ITokenizationSupport inner) : ITokenizationSupport
    {
        public IState GetInitialState() => inner.GetInitialState();

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) => inner.TokenizeEncoded(line, hasEol, state);

        public IBackgroundTokenizer? CreateBackgroundTokenizer(TextModel textModel, IBackgroundTokenizationStore store) => new NoBackgroundTokenizer();
    }

    private sealed class NoBackgroundTokenizer : IBackgroundTokenizer
    {
        public void Dispose()
        {
        }

        public void RequestTokens(int startLineNumber, int endLineNumberExclusive)
        {
        }
    }

}
