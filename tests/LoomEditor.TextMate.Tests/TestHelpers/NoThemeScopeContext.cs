using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Tests.TestHelpers;

/// <summary>A grammar stand-in for the scope stack tests: no theme, no embedded languages, language id 1.</summary>
internal sealed class NoThemeScopeContext : IScopeAttributesContext, IThemeProvider
{
    private readonly BasicScopeAttributesProvider _provider = new(1, null);

    public IThemeProvider ThemeProvider => this;

    public BasicScopeAttributes GetMetadataForScope(string scopeName) => _provider.GetBasicScopeAttributes(scopeName);

    public StyleAttributes? ThemeMatch(ScopeStack scopePath) => null;

    public StyleAttributes GetDefaults() => new(FontStyle.None, 0, 0);

    /// <summary>The root scope <c>source.x</c> with <paramref name="names"/> pushed on it.</summary>
    public AttributedScopeStack Scopes(params string[] names)
    {
        AttributedScopeStack stack = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, this);
        foreach (string name in names)
        {
            stack = stack.PushAttributed(name, this);
        }

        return stack;
    }
}
