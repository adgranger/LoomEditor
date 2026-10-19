// Ported from microsoft/vscode-textmate: src/tests/grammar.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Parsing;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

// Not ported: 'Fonts are correctly set' and 'Fonts are correctly set 2' (grammar.test.ts 145-196): font attributes are
// not ported (ADR 0008, decision 3). The metadata tests of the same file are in EncodedTokenAttributesTests.
public class GrammarRegressionTests
{
    // vscode-textmate: test.skip('Shadowed rules are resolved correctly')
    // Skipped upstream as well: a known vscode-textmate bug, the expected value below is upstream's own ("TODO this
    // should be inner!"): a rule of an inner repository is not found when another pattern is compiled first.
    [Fact(Skip = "Skipped upstream: known vscode-textmate bug (the inner repository rule should shadow the outer one)")]
    public void ShadowedRules_AreResolvedCorrectly()
    {
        using var registry = new Registry(new RegistryOptions(OnigTestLib.GetOniguruma(), _ => null));
        var repository = new RawRepository
        {
            ["foo"] = new RawRule { Include = "#bar" },
            ["bar"] = new RawRule { Match = "bar1", Name = "outer" },
        };
        IGrammar grammar = registry.AddGrammar(new RawGrammar
        {
            ScopeName = "source.test",
            Repository = repository,
            Patterns =
            [
                new RawRule
                {
                    Patterns = [new RawRule { Include = "#foo" }],
                    Repository = new RawRepository { ["bar"] = new RawRule { Match = "bar1", Name = "inner" } },
                },

                // When you move this up, the test passes
                new RawRule { Begin = "begin", Patterns = [new RawRule { Include = "#foo" }], End = "end" },
            ],
        });

        TokenizeLineResult result = grammar.TokenizeLine("bar1", null);

        // TODO this should be inner!
        Token token = Assert.Single(result.Tokens);
        Assert.Equal(0, token.StartIndex);
        Assert.Equal(4, token.EndIndex);
        Assert.Equal(["source.test", "outer"], token.Scopes);
    }
}
