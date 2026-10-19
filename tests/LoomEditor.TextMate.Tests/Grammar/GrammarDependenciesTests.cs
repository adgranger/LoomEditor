using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Tests.Grammar;

public class GrammarDependenciesTests
{
    private static RawGrammar Grammar(string scopeName, params RawRule[] patterns) => new()
    {
        ScopeName = scopeName,
        Patterns = [.. patterns],
        Repository = new RawRepository(),
    };

    private static ScopeDependencyProcessor Processor(string initial, params RawGrammar[] grammars)
    {
        var byScope = grammars.ToDictionary(g => g.ScopeName!, StringComparer.Ordinal);
        return new ScopeDependencyProcessor(scope => byScope.GetValueOrDefault(scope), initial);
    }

    private static string[] Keys(ScopeDependencyProcessor processor) => [.. processor.Q.Select(r => r.ToKey())];

    [Fact]
    public void ScopeDependencyProcessor_CollectsExternalScopesAndRepositoryRules()
    {
        RawGrammar a = Grammar(
            "source.a",
            new RawRule { Include = "source.b#rule" },
            new RawRule { Include = "source.c" },
            new RawRule { Include = "source.c" });
        ScopeDependencyProcessor processor = Processor("source.a", a);

        processor.ProcessQueue();

        Assert.Equal(["source.b#rule", "source.c"], Keys(processor));
        Assert.IsType<TopLevelRepositoryRuleReference>(processor.Q[0]);
        Assert.IsType<TopLevelRuleReference>(processor.Q[1]);
        Assert.Contains("source.a", processor.SeenFullScopeRequests);
        Assert.Contains("source.c", processor.SeenFullScopeRequests);
        Assert.Contains("source.b#rule", processor.SeenPartialScopeRequests);
    }

    [Fact]
    public void ScopeDependencyProcessor_SelfAndBaseAreNotExternal()
    {
        RawGrammar a = Grammar(
            "source.a",
            new RawRule { Include = "$self" },
            new RawRule { Include = "$base" },
            new RawRule { Include = "#local" },
            new RawRule { Include = "source.a" },
            new RawRule { Include = "source.a#local" });
        a.Repository!["local"] = new RawRule { Patterns = [new RawRule { Include = "source.d" }] };
        ScopeDependencyProcessor processor = Processor("source.a", a);

        processor.ProcessQueue();

        // Only the reference that is reached through the repository rule is external.
        Assert.Equal(["source.d"], Keys(processor));
    }

    [Fact]
    public void ScopeDependencyProcessor_FullRequestMakesPartialRequestRedundant()
    {
        RawGrammar a = Grammar("source.a", new RawRule { Include = "source.b" }, new RawRule { Include = "source.b#rule" });
        RawGrammar b = Grammar("source.b");
        ScopeDependencyProcessor processor = Processor("source.a", a, b);

        processor.ProcessQueue();
        Assert.Equal(["source.b"], Keys(processor));

        processor.ProcessQueue();
        Assert.Empty(processor.Q);
    }

    [Fact]
    public void ScopeDependencyProcessor_FollowsRepositoriesOfLoadedGrammarsAndInjections()
    {
        RawGrammar a = Grammar("source.a", new RawRule { Include = "source.b#rule" });
        RawGrammar b = Grammar("source.b");
        b.Repository!["rule"] = new RawRule { Include = "source.e" };
        var byScope = new[] { a, b }.ToDictionary(g => g.ScopeName!, StringComparer.Ordinal);
        var processor = new ScopeDependencyProcessor(
            scope => byScope.GetValueOrDefault(scope),
            "source.a",
            scope => scope == "source.b" ? ["source.inj"] : null);

        processor.ProcessQueue();
        Assert.Equal(["source.b#rule"], Keys(processor));

        processor.ProcessQueue();
        Assert.Equal(["source.e", "source.inj"], Keys(processor));
    }

    [Fact]
    public void ScopeDependencyProcessor_MissingInitialGrammar_Throws()
    {
        ScopeDependencyProcessor processor = Processor("source.a");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(processor.ProcessQueue);

        Assert.Equal("No grammar provided for <source.a>", ex.Message);
    }

    [Fact]
    public void ScopeDependencyProcessor_MissingOtherGrammar_IsIgnored()
    {
        RawGrammar a = Grammar("source.a", new RawRule { Include = "source.b" });
        ScopeDependencyProcessor processor = Processor("source.a", a);

        processor.ProcessQueue();
        processor.ProcessQueue();

        Assert.Empty(processor.Q);
    }
}
