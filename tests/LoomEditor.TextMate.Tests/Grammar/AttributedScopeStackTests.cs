using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;
using TextMateTheme = LoomEditor.TextMate.Theme.Theme;

namespace LoomEditor.TextMate.Tests.Grammar;

public class AttributedScopeStackTests
{
    private static readonly TextMateTheme TestTheme = TextMateTheme.CreateFromRawTheme(new RawTheme(
    [
        Setting(null, foreground: "#100000", background: "#200000"),
        Setting("string", foreground: "#300000"),
        Setting("string.quoted", fontStyle: "bold", foreground: "#400000"),
        Setting("comment", fontStyle: "italic"),
    ]));

    private sealed class FakeContext(BasicScopeAttributesProvider provider) : IScopeAttributesContext, IThemeProvider
    {
        public IThemeProvider ThemeProvider => this;

        public BasicScopeAttributes GetMetadataForScope(string scopeName) => provider.GetBasicScopeAttributes(scopeName);

        public StyleAttributes? ThemeMatch(ScopeStack scopePath) => TestTheme.Match(scopePath);

        public StyleAttributes GetDefaults() => TestTheme.GetDefaults();
    }

    private static FakeContext NewContext() => new(new BasicScopeAttributesProvider(5, null));

    private static string Foreground(uint attributes) => TestTheme.GetColorMap()[EncodedTokenAttributes.GetForeground(attributes)]!;

    [Fact]
    public void PushAttributed_MergesThemeColorsOfTheDeepestMatch()
    {
        FakeContext grammar = NewContext();
        AttributedScopeStack root = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar);

        AttributedScopeStack str = root.PushAttributed("string", grammar);
        AttributedScopeStack quoted = str.PushAttributed("string.quoted.double", grammar);
        AttributedScopeStack comment = root.PushAttributed("comment.line", grammar);

        // The root scope matches no rule (the theme defaults are applied elsewhere): the foreground stays unset.
        Assert.Equal(0, EncodedTokenAttributes.GetForeground(root.TokenAttributes));
        Assert.Equal("#300000", Foreground(str.TokenAttributes));
        Assert.Equal("#400000", Foreground(quoted.TokenAttributes));
        Assert.Equal((int)FontStyle.Bold, EncodedTokenAttributes.GetFontStyle(quoted.TokenAttributes));
        Assert.Equal(StandardTokenType.String, EncodedTokenAttributes.GetTokenType(quoted.TokenAttributes));
        // A rule that sets no foreground keeps the inherited one.
        Assert.Equal(0, EncodedTokenAttributes.GetForeground(comment.TokenAttributes));
        Assert.Equal((int)FontStyle.Italic, EncodedTokenAttributes.GetFontStyle(comment.TokenAttributes));
        Assert.Equal(StandardTokenType.Comment, EncodedTokenAttributes.GetTokenType(comment.TokenAttributes));
        Assert.Same(str, quoted.Parent);
        Assert.Equal("source.x string string.quoted.double", quoted.ToString());
    }

    [Fact]
    public void PushAttributed_ScopeWithSpaces_PushesEachSegment()
    {
        FakeContext grammar = NewContext();
        AttributedScopeStack root = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar);

        AttributedScopeStack result = root.PushAttributed("string comment", grammar);

        Assert.Equal(["source.x", "string", "comment"], result.GetScopeNames());
        Assert.Equal("comment", result.ScopeName);
        Assert.Equal("string", result.Parent!.ScopeName);
        Assert.Same(root, result.Parent.Parent);
        Assert.Same(root, root.PushAttributed(null, grammar));
    }

    [Fact]
    public void GetExtensionIfDefined_ReturnsFramesAboveBase()
    {
        FakeContext grammar = NewContext();
        AttributedScopeStack root = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar);
        AttributedScopeStack a = root.PushAttributed("meta.a", grammar);
        AttributedScopeStack top = a.PushAttributed("string quoted", grammar);

        List<AttributedScopeStackFrame>? frames = top.GetExtensionIfDefined(root);

        Assert.NotNull(frames);
        Assert.Equal(3, frames.Count);
        Assert.Equal(["meta.a"], frames[0].ScopeNames);
        Assert.Equal(a.TokenAttributes, frames[0].EncodedTokenAttributes);
        Assert.Equal(["quoted"], frames[2].ScopeNames);
        Assert.Equal(top.TokenAttributes, frames[2].EncodedTokenAttributes);
        Assert.Empty(top.GetExtensionIfDefined(top)!);
        Assert.Null(a.GetExtensionIfDefined(top));

        AttributedScopeStack? rebuilt = AttributedScopeStack.FromExtension(root, frames);
        Assert.True(AttributedScopeStack.Equals(top, rebuilt));
        Assert.Same(root, AttributedScopeStack.FromExtension(root, []));
    }

    [Fact]
    public void Equals_IsStructural()
    {
        FakeContext grammar = NewContext();
        AttributedScopeStack root = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar);
        AttributedScopeStack one = root.PushAttributed("string", grammar);
        AttributedScopeStack two = AttributedScopeStack.CreateRootAndLookUpScopeName("source.x", 0, grammar).PushAttributed("string", grammar);

        Assert.NotSame(one, two);
        Assert.True(AttributedScopeStack.Equals(one, two));
        Assert.True(one.EqualsStack(two));
        Assert.False(AttributedScopeStack.Equals(one, root));
        Assert.False(AttributedScopeStack.Equals(one, null));
        Assert.True(AttributedScopeStack.Equals(null, null));
        Assert.False(AttributedScopeStack.Equals(one, root.PushAttributed("comment", grammar)));
    }

    [Fact]
    public void BalancedBracketSelectors_UnbalancedWins_AndStarMatchesAlways()
    {
        var star = new BalancedBracketSelectors(["*"], []);
        Assert.True(star.MatchesAlways);
        Assert.False(star.MatchesNever);
        Assert.True(star.Match(["source.x", "anything"]));

        var starExcept = new BalancedBracketSelectors(["*"], ["string"]);
        Assert.False(starExcept.MatchesAlways);
        Assert.False(starExcept.Match(["source.x", "string.quoted"]));
        Assert.True(starExcept.Match(["source.x", "meta"]));

        var specific = new BalancedBracketSelectors(["meta.block", "source.y comment"], ["meta.block.skip"]);
        Assert.False(specific.MatchesAlways);
        Assert.False(specific.MatchesNever);
        Assert.True(specific.Match(["source.x", "meta.block.inner"]));
        Assert.False(specific.Match(["source.x", "meta.block.skip"]));
        Assert.True(specific.Match(["source.y", "comment.line"]));
        Assert.False(specific.Match(["source.x", "comment.line"]));

        var none = new BalancedBracketSelectors([], ["string"]);
        Assert.True(none.MatchesNever);
        Assert.False(none.Match(["string"]));
    }

    [Fact]
    public void NameMatcher_RequiresIdentifiersInOrderAsScopePrefixes()
    {
        Assert.True(NameMatcher.Matches(["a", "b"], ["a.x", "b"]));
        Assert.False(NameMatcher.Matches(["b", "a"], ["a", "b"]));
        Assert.False(NameMatcher.Matches(["a", "a"], ["a"]));
        Assert.False(NameMatcher.Matches(["a"], ["ab"]));
        Assert.False(NameMatcher.Matches(["a"], [""]));
    }

    private static RawThemeSetting Setting(string? scope, string? fontStyle = null, string? foreground = null, string? background = null) =>
        new(null, scope, null, new RawThemeSettings(fontStyle, foreground, background));
}
