using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

// Monaco tests Variable through the parser (ported in P10-21): all tests here are new.
public class VariableTests
{
    private sealed class FixedResolver(string? value) : IVariableResolver
    {
        public string? Resolve(Variable variable) => value;
    }

    private static Variable WithDefault(string name, string defaultValue)
    {
        var variable = new Variable(name);
        variable.AppendChild(new TextMarker(defaultValue));
        return variable;
    }

    [Fact]
    public void Variable_Resolve_Known_ReplacesChildren()
    {
        Variable variable = WithDefault("A", "default");

        Assert.True(variable.Resolve(new FixedResolver("value")));

        Assert.Equal("value", variable.ToString());
        Assert.Single(variable.Children);
    }

    [Fact]
    public void Variable_Resolve_Unknown_KeepsDefaultChildren()
    {
        Variable variable = WithDefault("A", "default");

        Assert.False(variable.Resolve(new FixedResolver(null)));

        Assert.Equal("default", variable.ToString());
    }

    [Fact]
    public void Variable_Resolve_AppliesTransform()
    {
        var variable = new Variable("A");
        var transform = new Transform();
        transform.SetRegexp("b", string.Empty);
        transform.AppendChild(new TextMarker("X"));
        variable.Transform = transform;

        Assert.True(variable.Resolve(new FixedResolver("abc")));

        Assert.Equal("aXc", variable.ToString());
    }

    [Fact]
    public void Variable_ToTextmateString_WithAndWithoutDefault()
    {
        Assert.Equal("${A}", new Variable("A").ToTextmateString());
        Assert.Equal("${A:def\\}}", WithDefault("A", "def}").ToTextmateString());
    }

    [Fact]
    public void Variable_Clone_IsDeep()
    {
        Variable original = WithDefault("A", "d");
        original.Transform = new Transform();

        var clone = (Variable)original.Clone();

        Assert.Equal(original.ToTextmateString(), clone.ToTextmateString());
        Assert.NotSame(original.Transform, clone.Transform);
        Assert.NotSame(original.Children[0], clone.Children[0]);
    }
}
