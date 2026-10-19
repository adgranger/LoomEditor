using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Tests.TestHelpers;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

public class TokenizeStringMatchTests
{
    private static string GrammarJson(string pattern, string? injections = null) => $$"""
        {
          "scopeName": "source.x",
          "patterns": [ { "match": "{{pattern}}", "name": "own" } ]
          {{(injections is null ? "" : ", \"injections\": " + injections)}}
        }
        """;

    // The state at the start of a line: the root rule on top of an empty stack, like Grammar._tokenize does.
    private static StateStackImpl RootStack(TextMateGrammar grammar)
    {
        RuleId rootId = RuleFactory.GetCompiledRuleId(grammar.RootGrammar.Repository!["$self"], grammar, grammar.RootGrammar.Repository!);
        grammar.GetInjections();
        AttributedScopeStack scopes = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar);
        return new StateStackImpl(null, rootId, -1, -1, false, null, scopes, scopes);
    }

    [Fact]
    public void MatchRule_ReturnsCaptureIndicesAndRuleId()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("a+"));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("xxaab\n");

        MatchResult? result = TokenizeString.MatchRule(grammar, line, true, 0, stack, -1);

        Assert.NotNull(result);
        Assert.Equal(new OnigCaptureIndex(2, 4), Assert.Single(result.CaptureIndices));
        MatchRule rule = Assert.IsType<MatchRule>(grammar.GetRule(result.MatchedRuleId));
        Assert.Equal("own", rule.GetName(null, null));
    }

    [Fact]
    public void MatchRule_SearchesFromTheGivenPosition()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("a"));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("aba\n");

        MatchResult? result = TokenizeString.MatchRule(grammar, line, true, 1, stack, -1);

        Assert.Equal(new OnigCaptureIndex(2, 3), Assert.Single(result!.CaptureIndices));
    }

    [Fact]
    public void MatchRule_NoMatch_ReturnsNull()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("z"));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("abc\n");

        Assert.Null(TokenizeString.MatchRule(grammar, line, true, 0, stack, -1));
    }

    [Fact]
    public void MatchRuleOrInjections_NoInjections_ReturnsTheRuleMatch()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("a"));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("a\n");

        MatchResult? result = TokenizeString.MatchRuleOrInjections(grammar, line, true, 0, stack, -1);

        Assert.IsNotType<MatchInjectionsResult>(result);
        Assert.Equal(new OnigCaptureIndex(0, 1), Assert.Single(result!.CaptureIndices));
    }

    [Fact]
    public void MatchRuleOrInjections_LeftPriorityInjection_WinsAtSamePosition()
    {
        using TextMateGrammar grammar = TestGrammars.Create(
            GrammarJson("a", """{ "L:source.x": { "match": "a", "name": "left" } }"""));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("a\n");

        MatchResult? result = TokenizeString.MatchRuleOrInjections(grammar, line, true, 0, stack, -1);

        MatchInjectionsResult injection = Assert.IsType<MatchInjectionsResult>(result);
        Assert.True(injection.PriorityMatch);
        Assert.Equal(grammar.GetInjections()[0].RuleId, injection.MatchedRuleId);
    }

    [Fact]
    public void MatchRuleOrInjections_RightPriorityInjection_LosesAtSamePosition()
    {
        using TextMateGrammar grammar = TestGrammars.Create(
            GrammarJson("a", """{ "R:source.x": { "match": "a", "name": "right" }, "source.x": { "match": "a", "name": "plain" } }"""));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("a\n");

        MatchResult? result = TokenizeString.MatchRuleOrInjections(grammar, line, true, 0, stack, -1);

        Assert.IsNotType<MatchInjectionsResult>(result);
        Assert.Equal("own", grammar.GetRule(result!.MatchedRuleId)!.GetName(null, null));
    }

    [Fact]
    public void MatchRuleOrInjections_InjectionStartingEarlier_Wins_AndLaterOneLoses()
    {
        using TextMateGrammar grammar = TestGrammars.Create(
            GrammarJson("b", """{ "source.x": { "match": "a", "name": "inj" } }"""));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString earlier = grammar.CreateOnigString("ab\n");
        using IOnigString later = grammar.CreateOnigString("ba\n");

        MatchResult? wins = TokenizeString.MatchRuleOrInjections(grammar, earlier, true, 0, stack, -1);
        MatchResult? loses = TokenizeString.MatchRuleOrInjections(grammar, later, true, 0, stack, -1);

        Assert.IsType<MatchInjectionsResult>(wins);
        Assert.IsNotType<MatchInjectionsResult>(loses);
    }

    [Fact]
    public void MatchRuleOrInjections_OnlyInjectionMatches_ReturnsIt_AndSelectorMustMatchTheStack()
    {
        using TextMateGrammar grammar = TestGrammars.Create(GrammarJson("z", """
            { "source.x": { "match": "a", "name": "inj" }, "source.other": { "match": "a", "name": "other" } }
            """));
        StateStackImpl stack = RootStack(grammar);
        using IOnigString line = grammar.CreateOnigString("a\n");

        MatchResult? result = TokenizeString.MatchRuleOrInjections(grammar, line, true, 0, stack, -1);

        Assert.IsType<MatchInjectionsResult>(result);
        Assert.Equal("inj", grammar.GetRule(result.MatchedRuleId)!.GetName(null, null));
    }

    [Fact]
    public void GetFindOptions_DisablesAnchorsThatAreNotAllowed()
    {
        Assert.Equal(FindOption.None, TokenizeString.GetFindOptions(true, true));
        Assert.Equal(FindOption.NotBeginString, TokenizeString.GetFindOptions(false, true));
        Assert.Equal(FindOption.NotBeginPosition, TokenizeString.GetFindOptions(true, false));
        Assert.Equal(FindOption.NotBeginString | FindOption.NotBeginPosition, TokenizeString.GetFindOptions(false, false));
    }
}
