using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Tests.TestHelpers;
using TextMateGrammar = LoomEditor.TextMate.Grammar.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

public class DiffStateStacksTests
{
    private const string Braces = """
        {
          "scopeName": "source.x",
          "patterns": [ { "begin": "\\{", "end": "\\}", "name": "block", "contentName": "block.content", "patterns": [ { "include": "$self" } ] } ]
        }
        """;

    [Fact]
    public void DiffStateStacks_ApplyDiff_RebuildsAnEqualStack()
    {
        using TextMateGrammar grammar = TestGrammars.Create(Braces);
        TokenizeLineResult line1 = grammar.TokenizeLine("{", null);
        TokenizeLineResult line2 = grammar.TokenizeLine("{ {", line1.RuleStack);
        TokenizeLineResult line3 = grammar.TokenizeLine("}", line2.RuleStack);
        TokenizeLineResult line4 = grammar.TokenizeLine("} }", line3.RuleStack);
        StateStack[] states = [StateStack.Initial, line1.RuleStack, line2.RuleStack, line3.RuleStack, line4.RuleStack];

        // "{ {" pushes two blocks, "} }" pops two.
        Assert.Equal([1, 2, 4, 3, 1], states.Select(s => s.Depth));

        foreach (StateStack from in states)
        {
            foreach (StateStack to in states)
            {
                StackDiff diff = DiffStateStacks.DiffStateStacksRefEq(from, to);
                StateStackImpl? rebuilt = DiffStateStacks.ApplyStateStackDiff(from, diff);

                Assert.True(to.Equals(rebuilt), $"depth {from.Depth} -> depth {to.Depth}");
            }
        }
    }

    [Fact]
    public void DiffStateStacksRefEq_CountsPopsAndNewFrames()
    {
        using TextMateGrammar grammar = TestGrammars.Create(Braces);
        TokenizeLineResult line1 = grammar.TokenizeLine("{", null);
        TokenizeLineResult line2 = grammar.TokenizeLine("{ {", line1.RuleStack);

        StackDiff down = DiffStateStacks.DiffStateStacksRefEq(line1.RuleStack, line2.RuleStack);
        StackDiff up = DiffStateStacks.DiffStateStacksRefEq(line2.RuleStack, line1.RuleStack);
        StackDiff same = DiffStateStacks.DiffStateStacksRefEq(line2.RuleStack, line2.RuleStack);

        Assert.Equal(0, down.Pops);
        Assert.Equal(2, down.NewFrames.Count);
        Assert.Equal(2, up.Pops);
        Assert.Empty(up.NewFrames);
        Assert.Equal(0, same.Pops);
        Assert.Empty(same.NewFrames);
    }
}
