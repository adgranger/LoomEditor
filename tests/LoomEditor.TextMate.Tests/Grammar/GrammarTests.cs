using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

public class GrammarTests
{
    private const string Simple = """
        {
          "scopeName": "source.x",
          "patterns": [ { "match": "a", "name": "letter.a" } ],
          "repository": { "r": { "match": "b", "name": "letter.b" } }
        }
        """;

    [Fact]
    public void CreateGrammar_InitializesSelfAndBase()
    {
        using TextMateGrammar grammar = TestGrammars.Create(Simple);

        RawRepository repository = grammar.RootGrammar.Repository!;
        RawRule self = repository["$self"];

        Assert.Equal("source.x", self.Name);
        Assert.Same(grammar.RootGrammar.Patterns, self.Patterns);
        Assert.Same(self, repository["$base"]);
        Assert.True(repository.TryGetValue("r", out _));
    }

    [Fact]
    public void CreateGrammar_DoesNotMutateTheGivenRawGrammar()
    {
        RawGrammar raw = TestGrammars.Parse("""{ "scopeName": "source.y", "patterns": [ { "match": "a" } ] }""");

        using TextMateGrammar grammar = TextMateGrammar.Create(
            "source.y", raw, 1, null, null, null, new FakeGrammarRepository(), OnigTestLib.GetOniguruma());

        Assert.Null(raw.Repository);
        Assert.NotSame(raw.Patterns, grammar.RootGrammar.Patterns);
        Assert.NotNull(grammar.RootGrammar.Repository);
    }

    [Fact]
    public void RegisterRule_AssignsSequentialIds_AndGetRuleFindsThem()
    {
        using TextMateGrammar grammar = TestGrammars.Create(Simple);

        RuleId first = RuleFactory.GetCompiledRuleId(grammar.RootGrammar.Repository!["$self"], grammar, grammar.RootGrammar.Repository!);

        Assert.Equal(1, first.Value);
        Assert.IsType<IncludeOnlyRule>(grammar.GetRule(first));
        Assert.Null(grammar.GetRule(new RuleId(999)));
        Assert.Null(grammar.GetRule(new RuleId(0)));
    }

    [Fact]
    public void GetInjections_CollectsGrammarAndRepositoryInjections_InPriorityOrder()
    {
        const string Root = """
            {
              "scopeName": "source.x",
              "patterns": [],
              "injections": {
                "source.x string": { "match": "a", "name": "inj.default" },
                "L:source.x comment": { "match": "b", "name": "inj.left" }
              }
            }
            """;
        const string Injected = """
            { "scopeName": "source.inj", "injectionSelector": "R:source.x", "patterns": [ { "match": "c", "name": "inj.right" } ] }
            """;
        FakeGrammarRepository repository = new FakeGrammarRepository().Add(Injected).Inject("source.x", "source.inj");
        using TextMateGrammar grammar = TestGrammars.Create(Root, repository);

        List<Injection> injections = grammar.GetInjections();

        Assert.Equal(["L:source.x comment", "source.x string", "R:source.x"], injections.Select(i => i.DebugSelector));
        Assert.Equal([-1, 0, 1], injections.Select(i => i.Priority));
        Assert.True(injections[1].Matcher(["source.x", "string.quoted"]));
        Assert.False(injections[1].Matcher(["source.x", "comment"]));
        Assert.Equal("source.inj", injections[2].Grammar.ScopeName);
        Assert.Equal(3, injections.Select(i => i.RuleId.Value).Distinct().Count());
        Assert.Same(injections, grammar.GetInjections());
    }

    [Fact]
    public void GetInjections_IgnoresMissingGrammarsAndGrammarsWithoutSelector()
    {
        FakeGrammarRepository repository = new FakeGrammarRepository()
            .Add("""{ "scopeName": "source.noselector", "patterns": [] }""")
            .Inject("source.x", "source.missing")
            .Inject("source.x", "source.noselector");
        using TextMateGrammar grammar = TestGrammars.Create(Simple, repository);

        Assert.Empty(grammar.GetInjections());
    }

    [Fact]
    public void GetExternalGrammar_LoadsFromRepositoryOnce()
    {
        FakeGrammarRepository repository = new FakeGrammarRepository().Add("""{ "scopeName": "source.ext", "patterns": [] }""");
        using TextMateGrammar grammar = TestGrammars.Create(Simple, repository);

        RawGrammar? first = grammar.GetExternalGrammar("source.ext");
        RawGrammar? second = grammar.GetExternalGrammar("source.ext", grammar.RootGrammar.Repository);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, repository.LookupCounts["source.ext"]);
        Assert.NotNull(first.Repository!["$self"]);
        Assert.Null(grammar.GetExternalGrammar("source.unknown"));
    }

    [Fact]
    public void GetExternalGrammar_UsesTheBaseOfTheIncludingRepository()
    {
        FakeGrammarRepository repository = new FakeGrammarRepository().Add("""{ "scopeName": "source.ext", "patterns": [] }""");
        using TextMateGrammar grammar = TestGrammars.Create(Simple, repository);

        RawGrammar? external = grammar.GetExternalGrammar("source.ext", grammar.RootGrammar.Repository);

        Assert.Same(grammar.RootGrammar.Repository!["$base"], external!.Repository!["$base"]);
        Assert.NotSame(external.Repository["$self"], external.Repository["$base"]);
    }

    [Fact]
    public void GetMetadataForScope_UsesEmbeddedLanguages()
    {
        using TextMateGrammar grammar = TestGrammars.Create(
            Simple,
            embeddedLanguages: new Dictionary<string, int> { ["source.css"] = 4 });

        Assert.Equal(4, grammar.GetMetadataForScope("source.css.embedded").LanguageId);
        Assert.Equal(0, grammar.GetMetadataForScope("source.js").LanguageId);
    }
}
