using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Grammar;

public class StateStackTests
{
    private static readonly NoThemeScopeContext Grammar = new();

    private static AttributedScopeStack Scopes(params string[] names) => Grammar.Scopes(names);

    private static StateStackImpl PushRule(StateStackImpl stack, int ruleId, string? endRule = null, params string[] scopes)
    {
        AttributedScopeStack names = Scopes(scopes);
        return stack.Push(new RuleId(ruleId), 0, -1, false, endRule, names, names);
    }

    [Fact]
    public void Initial_IsTheNullStateWithDepthOne()
    {
        StateStack initial = StateStack.Initial;

        Assert.Same(StateStackImpl.Null, initial);
        Assert.Equal(1, initial.Depth);
        Assert.Same(initial, initial.Clone());
        Assert.True(initial.Equals(StateStackImpl.Null));
        Assert.False(initial.Equals(null));
    }

    [Fact]
    public void Push_ThenPop_ReturnsEqualStack()
    {
        StateStackImpl root = PushRule(StateStackImpl.Null, 1);
        StateStackImpl pushed = PushRule(root, 2, null, "string");

        Assert.Equal(root.Depth + 1, pushed.Depth);
        Assert.Same(root, pushed.Pop());
        Assert.True(pushed.Pop()!.Equals(root));
        Assert.Null(StateStackImpl.Null.Pop());
    }

    [Fact]
    public void Equals_StructurallyEqualStacks_AreEqual()
    {
        StateStackImpl a = PushRule(PushRule(StateStackImpl.Null, 1), 2, "end", "string");
        StateStackImpl b = PushRule(PushRule(StateStackImpl.Null, 1), 2, "end", "string");

        Assert.NotSame(a, b);
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
        Assert.False(a.Equals(a.Parent));
        // enterPos / anchorPos are not part of the equality.
        Assert.True(a.Equals(a.Parent!.Push(new RuleId(2), 9, 9, false, "end", a.NameScopesList, a.ContentNameScopesList)));
    }

    [Fact]
    public void Equals_DifferentEndRule_AreNotEqual()
    {
        StateStackImpl a = PushRule(StateStackImpl.Null, 2, "end");
        StateStackImpl b = PushRule(StateStackImpl.Null, 2, "other");

        Assert.False(a.Equals(b));
        Assert.True(a.Equals(b.WithEndRule("end")));
        Assert.Same(a, a.WithEndRule("end"));
    }

    [Fact]
    public void Equals_DifferentRuleOrContentScopes_AreNotEqual()
    {
        StateStackImpl a = PushRule(StateStackImpl.Null, 2, null, "string");

        Assert.False(a.Equals(PushRule(StateStackImpl.Null, 3, null, "string")));
        Assert.False(a.Equals(PushRule(StateStackImpl.Null, 2, null, "comment")));
    }

    [Fact]
    public void SafePop_OnRoot_ReturnsRoot()
    {
        StateStackImpl pushed = PushRule(StateStackImpl.Null, 5);

        Assert.Same(StateStackImpl.Null, StateStackImpl.Null.SafePop());
        Assert.Same(StateStackImpl.Null, pushed.SafePop());
    }

    [Fact]
    public void Reset_ForgetsEnterAndAnchorPositions_AndHasSameRuleAsUsesEnterPos()
    {
        StateStackImpl a = StateStackImpl.Null.Push(new RuleId(1), 3, 4, false, null, null, null);
        StateStackImpl b = a.Push(new RuleId(2), 3, 4, false, null, null, null);

        Assert.Equal(3, b.GetEnterPos());
        Assert.Equal(4, b.GetAnchorPos());
        Assert.True(b.HasSameRuleAs(a.Push(new RuleId(1), 3, 0, false, null, null, null)));
        Assert.False(b.HasSameRuleAs(a.Push(new RuleId(1), 7, 0, false, null, null, null)));

        b.Reset();

        Assert.Equal(-1, b.GetEnterPos());
        Assert.Equal(-1, b.GetAnchorPos());
        Assert.Equal(-1, a.GetEnterPos());
    }

    [Fact]
    public void WithContentNameScopesList_ReplacesOnlyWhenDifferent()
    {
        AttributedScopeStack names = Scopes("meta");
        StateStackImpl root = PushRule(StateStackImpl.Null, 1);
        StateStackImpl state = root.Push(new RuleId(2), 1, 2, true, "end", names, names);
        AttributedScopeStack content = names.PushAttributed("string", Grammar);

        StateStackImpl changed = state.WithContentNameScopesList(content);

        Assert.Same(state, state.WithContentNameScopesList(names));
        Assert.Same(content, changed.ContentNameScopesList);
        Assert.Same(names, changed.NameScopesList);
        Assert.Same(root, changed.Parent);
        Assert.True(changed.BeginRuleCapturedEol);
        Assert.Equal("end", changed.EndRule);
        Assert.Equal(1, changed.GetEnterPos());
        Assert.Equal(2, changed.GetAnchorPos());
    }

    [Fact]
    public void ToStateStackFrame_PushFrame_RoundTrips()
    {
        AttributedScopeStack parentNames = Scopes("meta.a");
        StateStackImpl parent = StateStackImpl.Null.Push(new RuleId(1), 0, 0, false, null, parentNames, parentNames);
        AttributedScopeStack names = parentNames.PushAttributed("meta.b", Grammar);
        AttributedScopeStack content = names.PushAttributed("string quoted", Grammar);
        StateStackImpl state = parent.Push(new RuleId(7), 0, 0, true, "\\)", names, content);

        StateStackFrame frame = state.ToStateStackFrame();
        StateStackImpl rebuilt = StateStackImpl.PushFrame(parent, frame);

        Assert.Equal(7, frame.RuleId);
        Assert.True(frame.BeginRuleCapturedEol);
        Assert.Equal("\\)", frame.EndRule);
        Assert.Equal(["meta.b"], Assert.Single(frame.NameScopesList).ScopeNames);
        Assert.Equal(2, frame.ContentNameScopesList.Count);
        Assert.True(state.Equals(rebuilt));
        Assert.Equal(-1, rebuilt.GetEnterPos());
        Assert.Equal(-1, rebuilt.GetAnchorPos());
        Assert.Equal(state.ToString(), rebuilt.ToString());
    }

    [Fact]
    public void ToString_ListsRulesOutermostFirst()
    {
        StateStackImpl state = PushRule(PushRule(StateStackImpl.Null, 1), 2, null, "string");

        Assert.Equal("[(0, , ),(1, source.x, source.x),(2, source.x string, source.x string)]", state.ToString());
    }
}
