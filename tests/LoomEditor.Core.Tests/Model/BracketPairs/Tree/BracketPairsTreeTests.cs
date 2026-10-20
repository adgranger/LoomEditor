using LoomEditor.Core.Languages;
using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// The upstream tests of this class go through the model part (getBracketPairsInRange.test.ts, ported with the model part).
public class BracketPairsTreeTests
{
    public static TheoryData<int> Seeds => [.. Enumerable.Range(1, 30)];

    private static (Range Range, Range Opening, Range? Closing, int Level, int LevelOfEqualType)[] Pairs(BracketPairsTree tree, Range range)
    {
        return
        [
            .. tree.GetBracketPairsInRange(range, false).ToArray().Select(p =>
                (p.Range, p.OpeningBracketRange, p.ClosingBracketRange, p.NestingLevel, p.NestingLevelOfEqualBracketType)),
        ];
    }

    private static BracketInfo[] Brackets(BracketPairsTree tree, Range range, bool onlyColorized = false)
    {
        return tree.GetBracketsInRange(range, onlyColorized).ToArray();
    }

    [Fact]
    public void ModelWithoutTokens_UsesFastTokenizer_FindsPairs()
    {
        var test = BracketPairsTestModels.Create("{ [ ( ) ] }");
        var tree = test.CreateTree();

        var pairs = tree.GetBracketPairsInRange(test.Model.FullModelRange, false).ToArray();

        Assert.Equal(
            [new Range(1, 1, 1, 12), new Range(1, 3, 1, 10), new Range(1, 5, 1, 8)],
            pairs.Select(p => p.Range));
        Assert.Equal([0, 1, 2], pairs.Select(p => p.NestingLevel));
        Assert.Equal([0, 0, 0], pairs.Select(p => p.NestingLevelOfEqualBracketType));
        Assert.Equal([-1, -1, -1], pairs.Select(p => p.MinVisibleColumnIndentation));
        Assert.All(pairs, p => Assert.NotNull(p.ClosingBracketRange));
    }

    [Fact]
    public void GetBracketPairsInRange_IncludeMinIndentation_ComputesTheIndentationInsideThePairs()
    {
        var test = BracketPairsTestModels.Create("{\n    a\n  b\n   }");
        var tree = test.CreateTree();

        var pair = Assert.Single(tree.GetBracketPairsInRange(test.Model.FullModelRange, true).ToArray());

        Assert.Equal(2, pair.MinVisibleColumnIndentation);
    }

    [Fact]
    public void GetBracketsInRange_OnlyColorized_SkipsPairsNotColorized()
    {
        var test = BracketPairsTestModels.Create("{ ( ) }", colorizedBracketPairs: [new CharacterPair("{", "}")]);
        var tree = test.CreateTree();
        var range = test.Model.FullModelRange;

        var all = Brackets(tree, range);
        var colorized = Brackets(tree, range, onlyColorized: true);

        Assert.Equal(
            [new Range(1, 1, 1, 2), new Range(1, 3, 1, 4), new Range(1, 5, 1, 6), new Range(1, 7, 1, 8)],
            all.Select(b => b.Range));
        Assert.Equal([0, 1, 1, 0], all.Select(b => b.NestingLevel));
        Assert.Equal([new Range(1, 1, 1, 2), new Range(1, 7, 1, 8)], colorized.Select(b => b.Range));
        Assert.Equal([0, 0], colorized.Select(b => b.NestingLevel));
    }

    [Fact]
    public void GetBracketsInRange_UnclosedAndUnexpectedBrackets_AreMarkedInvalid()
    {
        var test = BracketPairsTestModels.Create("( ] [");
        var tree = test.CreateTree();

        var brackets = Brackets(tree, test.Model.FullModelRange);

        Assert.Equal([new Range(1, 1, 1, 2), new Range(1, 3, 1, 4), new Range(1, 5, 1, 6)], brackets.Select(b => b.Range));
        Assert.Equal([true, true, true], brackets.Select(b => b.IsInvalid));
    }

    [Fact]
    public void GetBracketsInRange_Window_ReturnsOnlyBracketsTouchingTheRange()
    {
        var test = BracketPairsTestModels.Create("{ } ( ) [ ]");
        var tree = test.CreateTree();

        var brackets = Brackets(tree, new Range(1, 5, 1, 8));

        Assert.Equal([new Range(1, 5, 1, 6), new Range(1, 7, 1, 8)], brackets.Select(b => b.Range));
    }

    [Fact]
    public void GetBracketsInRange_CallbackReturnsFalse_StopsTheWalk()
    {
        var test = BracketPairsTestModels.Create("{ } ( ) [ ]");
        var tree = test.CreateTree();
        var seen = 0;

        tree.GetBracketsInRange(test.Model.FullModelRange, false).Iterate(_ => ++seen < 2);

        Assert.Equal(2, seen);
    }

    [Fact]
    public void GetFirstBracketBefore_And_After_SkipUnexpectedClosingBrackets()
    {
        var test = BracketPairsTestModels.Create("x ) ( y");
        var tree = test.CreateTree();

        var after = tree.GetFirstBracketAfter(new Position(1, 3))!.Value;
        var beforeOpening = tree.GetFirstBracketBefore(new Position(1, 8))!.Value;
        var beforeUnexpected = tree.GetFirstBracketBefore(new Position(1, 4));
        var afterOpening = tree.GetFirstBracketAfter(new Position(1, 6));

        Assert.Equal(new Range(1, 5, 1, 6), after.Range);
        Assert.Equal("(", after.BracketInfo.BracketText);
        Assert.Equal(new Range(1, 5, 1, 6), beforeOpening.Range);
        Assert.Null(beforeUnexpected);
        Assert.Null(afterOpening);
    }

    [Fact]
    public void GetFirstBracketBefore_And_After_ReturnTheClosestBracket()
    {
        var test = BracketPairsTestModels.Create("{ a [ b ] c }");
        var tree = test.CreateTree();

        var before = tree.GetFirstBracketBefore(new Position(1, 12))!.Value;
        var after = tree.GetFirstBracketAfter(new Position(1, 4))!.Value;

        Assert.Equal(new Range(1, 9, 1, 10), before.Range);
        Assert.Equal(new Range(1, 5, 1, 6), after.Range);
    }

    [Fact]
    public void ContentChanged_ThenQuery_ReparsesIncrementally()
    {
        var test = BracketPairsTestModels.Create("{ }");
        var tree = test.CreateTreeFollowingContent();
        Assert.Single(tree.GetBracketPairsInRange(test.Model.FullModelRange, false).ToArray());

        test.Model.ApplyEdits([TextEdit.Insert(new Position(1, 3), "( ) ")]);
        test.Model.ApplyEdits([TextEdit.Insert(new Position(1, 5), "[ ] ")]);

        Assert.Equal("{ ( [ ] ) }", test.Model.GetValue());
        var pairs = Pairs(tree, test.Model.FullModelRange);
        Assert.Equal(
            [new Range(1, 1, 1, 12), new Range(1, 3, 1, 10), new Range(1, 5, 1, 8)],
            pairs.Select(p => p.Range));
        Assert.Equal([0, 1, 2], pairs.Select(p => p.Level));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void IncrementalReparse_EqualsFreshTree(int seed)
    {
        var random = new Random(seed);
        const string Alphabet = "{}[]() ab\n";
        var test = BracketPairsTestModels.Create(RandomText(random, Alphabet, 12));
        var model = test.Model;
        var tree = test.CreateTreeFollowingContent();

        for (var i = 0; i < 20; i++)
        {
            var message = $"seed={seed} edit={i} text={model.GetValue().ReplaceLineEndings("\\n")}";
            var length = model.GetValueLength();
            var a = random.Next(0, length + 1);
            var b = random.Next(0, length + 1);
            var start = model.GetPositionAt(Math.Min(a, b));
            var end = model.GetPositionAt(Math.Max(a, b));
            model.ApplyEdits([new TextEdit(new Range(start.LineNumber, start.Column, end.LineNumber, end.Column), RandomText(random, Alphabet, random.Next(0, 5)))]);

            var fresh = BracketPairsTestModels.Create(model.GetValue()).CreateTree();
            var range = model.FullModelRange;
            message = $"{message} -> {model.GetValue().ReplaceLineEndings("\\n")}";

            Assert.True(Brackets(fresh, range).SequenceEqual(Brackets(tree, range)), message);
            Assert.True(Pairs(fresh, range).SequenceEqual(Pairs(tree, range)), message);
        }
    }

    [Fact]
    public void TokensChanged_FiresChanged_WhenNoInitialTree()
    {
        var test = BracketPairsTestModels.Create("{\n}", withTokenization: true);
        test.TokenizeAll();
        var tree = test.CreateTree();
        var fired = 0;
        tree.Changed += (_, _) => fired++;

        tree.HandleDidChangeTokens(new ModelTokensChangedEventArgs([new ChangedLineRange(1, 2)]));

        Assert.Equal(1, fired);
        Assert.Single(tree.GetBracketPairsInRange(test.Model.FullModelRange, false).ToArray());
    }

    [Fact]
    public void TokensChanged_WithInitialTree_DoesNotFireChanged()
    {
        var test = BracketPairsTestModels.Create("{\n}");
        var tree = test.CreateTree();
        var fired = 0;
        tree.Changed += (_, _) => fired++;

        tree.HandleDidChangeTokens(new ModelTokensChangedEventArgs([new ChangedLineRange(1, 2)]));

        Assert.Equal(0, fired);
    }

    [Fact]
    public void BackgroundTokenizationCompleted_DropsInitialTree_AndFiresChanged()
    {
        var test = BracketPairsTestModels.Create("{\n}\n(\n)", withTokenization: true);
        test.Tokenize(1);
        var tree = test.CreateTree();
        var fired = 0;
        tree.Changed += (_, _) => fired++;

        tree.HandleDidChangeBackgroundTokenizationState();
        Assert.Equal(0, fired);

        test.TokenizeAll();
        tree.HandleDidChangeBackgroundTokenizationState();
        Assert.Equal(1, fired);

        tree.HandleDidChangeBackgroundTokenizationState();
        Assert.Equal(1, fired);
        Assert.Equal(2, tree.GetBracketPairsInRange(test.Model.FullModelRange, false).ToArray().Length);
    }

    [Fact]
    public void Constructor_TokenizationCompleted_BuildsTheTreeWithTokens()
    {
        var test = BracketPairsTestModels.Create("{ ( ) }", withTokenization: true);
        test.TokenizeAll();

        var tree = test.CreateTree();

        Assert.Equal(2, tree.GetBracketPairsInRange(test.Model.FullModelRange, false).ToArray().Length);
        Assert.True(tree.DidLanguageChange(BracketPairsTestModels.LanguageId));
    }

    [Fact]
    public void HasUnmatchedClosingBracketAfter_TokensNotAccurate_ReturnsFalse()
    {
        var test = BracketPairsTestModels.Create("x }\ny\nz", withTokenization: true);
        test.Tokenize(1);
        var tree = test.CreateTree();

        Assert.False(tree.HasUnmatchedClosingBracketAfter(new Position(1, 1), test.Opening("{")));
    }

    [Fact]
    public void HasUnmatchedClosingBracketAfter_AccurateTokens_FindsTheOrphanAfterThePosition()
    {
        var test = BracketPairsTestModels.Create("x }", withTokenization: true);
        test.TokenizeAll();
        var tree = test.CreateTree();

        Assert.True(tree.HasUnmatchedClosingBracketAfter(new Position(1, 1), test.Opening("{")));
        Assert.True(tree.HasUnmatchedClosingBracketAfter(new Position(1, 3), test.Opening("{")));
        Assert.False(tree.HasUnmatchedClosingBracketAfter(new Position(1, 4), test.Opening("{")));
        Assert.False(tree.HasUnmatchedClosingBracketAfter(new Position(1, 1), test.Opening("(")));
    }

    [Fact]
    public void HasUnmatchedClosingBracketAfter_MatchedPair_ReturnsFalse()
    {
        var test = BracketPairsTestModels.Create("{ }", withTokenization: true);
        test.TokenizeAll();
        var tree = test.CreateTree();

        Assert.False(tree.HasUnmatchedClosingBracketAfter(new Position(1, 1), test.Opening("{")));
    }

    private static string RandomText(Random random, string alphabet, int length)
    {
        return string.Create(length, (random, alphabet), static (span, state) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = state.alphabet[state.random.Next(state.alphabet.Length)];
            }
        });
    }
}
