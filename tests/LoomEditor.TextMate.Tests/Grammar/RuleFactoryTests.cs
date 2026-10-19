using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

public class RuleFactoryTests
{
    [Theory]
    [InlineData("$self", (int)IncludeReferenceKind.Self, null, null)]
    [InlineData("$base", (int)IncludeReferenceKind.Base, null, null)]
    [InlineData("#name", (int)IncludeReferenceKind.RelativeReference, null, "name")]
    [InlineData("source.js", (int)IncludeReferenceKind.TopLevelReference, "source.js", null)]
    [InlineData("source.js#name", (int)IncludeReferenceKind.TopLevelRepositoryReference, "source.js", "name")]
    public void ParseInclude_AllForms(string include, int kind, string? scopeName, string? ruleName)
    {
        IncludeReference reference = IncludeReference.Parse(include);

        Assert.Equal((IncludeReferenceKind)kind, reference.Kind);
        Assert.Equal(scopeName, (reference as TopLevelReference)?.ScopeName ?? (reference as TopLevelRepositoryReference)?.ScopeName);
        Assert.Equal(ruleName, (reference as RelativeReference)?.RuleName ?? (reference as TopLevelRepositoryReference)?.RuleName);
    }

    [Fact]
    public void GetCompiledRuleId_SameRawRule_ReturnsSameId()
    {
        var helper = new FakeHelper();
        var raw = new RawRule { Match = "a", Name = "x" };

        RuleId first = RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository());
        RuleId second = RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository());

        Assert.Equal(first, second);
        Assert.Equal(first.Value, raw.Id);
        MatchRule rule = Assert.IsType<MatchRule>(helper.GetRule(first));
        Assert.Equal("a", rule.DebugMatchRegExp);
        Assert.Equal("x", rule.GetName(null, null));
    }

    [Fact]
    public void CompilePatterns_IncludeOfRepositoryRule_IsResolved()
    {
        var helper = new FakeHelper();
        var repository = new RawRepository { ["a"] = new RawRule { Match = "x" } };
        var raw = new RawRule { Patterns = [new RawRule { Include = "#a" }, new RawRule { Match = "y" }] };

        IncludeOnlyRule rule = Assert.IsType<IncludeOnlyRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, repository)));

        Assert.Equal([new RuleId(repository["a"].Id!.Value), new RuleId(raw.Patterns[1].Id!.Value)], rule.Patterns);
        Assert.False(rule.HasMissingPatterns);
    }

    [Fact]
    public void CompilePatterns_ExternalAndSelfIncludes_AreResolved()
    {
        var external = new RawGrammar
        {
            ScopeName = "source.ext",
            Repository = new RawRepository
            {
                ["r"] = new RawRule { Match = "ext-r" },
                ["$self"] = new RawRule { Patterns = [new RawRule { Match = "ext-self" }] },
            },
        };
        var helper = new FakeHelper { ["source.ext"] = external };
        var repository = new RawRepository();
        repository["$self"] = new RawRule { Patterns = [new RawRule { Match = "self" }] };
        repository["$base"] = repository["$self"];
        var raw = new RawRule
        {
            Patterns = [new RawRule { Include = "source.ext#r" }, new RawRule { Include = "source.ext" }, new RawRule { Include = "$base" }],
        };

        IncludeOnlyRule rule = Assert.IsType<IncludeOnlyRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, repository)));

        Assert.Equal(3, rule.Patterns.Count);
        Assert.Equal(external.Repository["r"].Id, rule.Patterns[0].Value);
        Assert.Equal(external.Repository["$self"].Id, rule.Patterns[1].Value);
        Assert.Equal(repository["$self"].Id, rule.Patterns[2].Value);
    }

    [Fact]
    public void CompilePatterns_UnknownInclude_IsSkipped()
    {
        var helper = new FakeHelper();
        var raw = new RawRule
        {
            Patterns =
            [
                new RawRule { Include = "#missing" },
                new RawRule { Include = "source.unknown" },
                new RawRule { Include = "source.unknown#rule" },
                new RawRule { Include = "$self" },
                // An include-only rule whose patterns are all missing is removed too.
                new RawRule { Patterns = [new RawRule { Include = "#missing" }] },
                new RawRule { Match = "kept" },
            ],
        };

        IncludeOnlyRule rule = Assert.IsType<IncludeOnlyRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));

        Assert.Equal([new RuleId(raw.Patterns[5].Id!.Value)], rule.Patterns);
        Assert.True(rule.HasMissingPatterns);
    }

    [Fact]
    public void GetCompiledRuleId_IncludeWithoutPatterns_BecomesAPattern()
    {
        var helper = new FakeHelper();
        var repository = new RawRepository { ["a"] = new RawRule { Match = "x" } };
        var raw = new RawRule { Include = "#a", Repository = new RawRepository { ["b"] = new RawRule { Match = "y" } } };

        IncludeOnlyRule rule = Assert.IsType<IncludeOnlyRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, repository)));

        Assert.Equal([new RuleId(repository["a"].Id!.Value)], rule.Patterns);
    }

    [Fact]
    public void BeginEndRule_NoEnd_UsesFFFF()
    {
        var helper = new FakeHelper();
        var raw = new RawRule { Begin = "a", ContentName = "c" };

        BeginEndRule rule = Assert.IsType<BeginEndRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));

        Assert.Equal(char.MaxValue.ToString(), rule.DebugEndRegExp);
        Assert.Equal("a", rule.DebugBeginRegExp);
        Assert.False(rule.ApplyEndPatternLast);
        Assert.False(rule.EndHasBackReferences);
        Assert.Equal("c", rule.GetContentName("", []));
    }

    [Theory]
    [InlineData(false, "   - -1: \\)\n   - 2: x")]
    [InlineData(true, "   - 2: x\n   - -1: \\)")]
    public void BeginEndRule_ApplyEndPatternLast_PutsEndLast(bool applyEndPatternLast, string expected)
    {
        var helper = new FakeHelper();
        var raw = new RawRule { Begin = "\\(", End = "\\)", ApplyEndPatternLast = applyEndPatternLast, Patterns = [new RawRule { Match = "x" }] };

        BeginEndRule rule = Assert.IsType<BeginEndRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));
        using CompiledRule compiled = rule.Compile(helper, null);

        Assert.Equal(applyEndPatternLast, rule.ApplyEndPatternLast);
        Assert.Equal(expected, compiled.ToString());
        rule.Dispose();
    }

    [Fact]
    public void BeginEndRule_EndBackReferences_AreResolvedPerCompile()
    {
        var helper = new FakeHelper();
        var raw = new RawRule { Begin = "(['\"])", End = "\\1" };
        BeginEndRule rule = Assert.IsType<BeginEndRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));

        string end = rule.GetEndWithResolvedBackReferences("'abc'", [new OnigCaptureIndex(0, 1), new OnigCaptureIndex(0, 1)]);
        CompiledRule compiled = rule.Compile(helper, end);

        Assert.True(rule.EndHasBackReferences);
        Assert.Equal("'", end);
        Assert.Equal(RuleId.End, compiled.FindNextMatchSync("x'", 0, FindOption.None)!.RuleId);
        rule.Dispose();
    }

    [Fact]
    public void BeginWhileRule_CompileWhile_UsesTheWhileRuleId()
    {
        var helper = new FakeHelper();
        var raw = new RawRule { Begin = ">", While = "\\G\\s*>", Captures = new RawCaptures { ["0"] = new RawRule { Name = "p" } } };

        BeginWhileRule rule = Assert.IsType<BeginWhileRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));
        CompiledRule compiled = rule.CompileWhileAG(helper, null, allowA: false, allowG: true);

        Assert.Equal(RuleId.While, compiled.FindNextMatchSync(" >", 0, FindOption.None)!.RuleId);
        Assert.Equal("p", rule.BeginCaptures[0]!.GetName(null, null));
        Assert.Equal("p", rule.WhileCaptures[0]!.GetName(null, null));
        Assert.Equal("\\G\\s*>", rule.DebugWhileRegExp);
        rule.Dispose();
    }

    [Fact]
    public void CompileCaptures_HolesAreNull()
    {
        var helper = new FakeHelper();
        var raw = new RawRule
        {
            Match = "(a)(b)(c)",
            Captures = new RawCaptures
            {
                ["3"] = new RawRule { Name = "three.$3" },
                ["0"] = new RawRule { Name = "all" },
                ["x"] = new RawRule { Name = "ignored" },
                ["1"] = new RawRule { Patterns = [new RawRule { Match = "a" }] },
            },
        };

        MatchRule rule = Assert.IsType<MatchRule>(helper.GetRule(RuleFactory.GetCompiledRuleId(raw, helper, new RawRepository())));

        Assert.Equal(4, rule.Captures.Count);
        Assert.Equal("all", rule.Captures[0]!.GetName(null, null));
        Assert.NotEqual(0, rule.Captures[1]!.RetokenizeCapturedWithRuleId.Value);
        Assert.Null(rule.Captures[2]);
        Assert.Equal("three.c", rule.Captures[3]!.GetName("abc", [new(0, 3), new(0, 1), new(1, 2), new(2, 3)]));
        Assert.Equal(0, rule.Captures[3]!.RetokenizeCapturedWithRuleId.Value);
    }

    [Fact]
    public void CaptureRule_Compile_IsNotSupported()
    {
        var helper = new FakeHelper();
        CaptureRule rule = RuleFactory.CreateCaptureRule(helper, "n", null, new RuleId(0));

        Assert.Throws<NotSupportedException>(() => rule.Compile(helper, null));
        Assert.StartsWith("CaptureRule#", rule.DebugName, StringComparison.Ordinal);
    }

    // The grammar's side of the rule factory (Grammar.registerRule / getRule / getExternalGrammar).
    private sealed class FakeHelper : IRuleFactoryHelper, IRuleRegistryAndOnigLib
    {
        private readonly IOnigLib _onigLib = OnigTestLib.GetOniguruma();
        private readonly Dictionary<int, Rule> _rules = [];
        private readonly Dictionary<string, RawGrammar> _externalGrammars = [];
        private int _lastRuleId;

        public RawGrammar this[string scopeName]
        {
            set => _externalGrammars[scopeName] = value;
        }

        public Rule? GetRule(RuleId ruleId) => _rules.GetValueOrDefault(ruleId.Value);

        public T RegisterRule<T>(Func<RuleId, T> factory)
            where T : Rule
        {
            var id = new RuleId(++_lastRuleId);
            T result = factory(id);
            _rules[id.Value] = result;
            return result;
        }

        public RawGrammar? GetExternalGrammar(string scopeName, RawRepository repository) => _externalGrammars.GetValueOrDefault(scopeName);

        public IOnigScanner CreateOnigScanner(IReadOnlyList<string> sources) => _onigLib.CreateOnigScanner(sources);

        public IOnigString CreateOnigString(string str) => _onigLib.CreateOnigString(str);
    }
}
