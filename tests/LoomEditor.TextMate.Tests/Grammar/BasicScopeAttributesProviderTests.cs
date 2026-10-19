using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tests.Grammar;

public class BasicScopeAttributesProviderTests
{
    private static BasicScopeAttributesProvider Provider(params (string Scope, int Language)[] embedded)
        => new(7, embedded.ToDictionary(e => e.Scope, e => e.Language));

    [Fact]
    public void BasicScopeAttributes_EmbeddedLanguage_LongestScopeWins()
    {
        BasicScopeAttributesProvider provider = Provider(("source.css", 2), ("source.css.embedded", 3), ("text.html", 4));

        Assert.Equal(2, provider.GetBasicScopeAttributes("source.css").LanguageId);
        Assert.Equal(2, provider.GetBasicScopeAttributes("source.css.less").LanguageId);
        Assert.Equal(3, provider.GetBasicScopeAttributes("source.css.embedded").LanguageId);
        Assert.Equal(3, provider.GetBasicScopeAttributes("source.css.embedded.html").LanguageId);
        Assert.Equal(4, provider.GetBasicScopeAttributes("text.html.basic").LanguageId);
    }

    [Fact]
    public void BasicScopeAttributes_EmbeddedLanguage_RequiresWholeScopeSegments()
    {
        BasicScopeAttributesProvider provider = Provider(("source.css", 2));

        Assert.Equal(0, provider.GetBasicScopeAttributes("source.cssx").LanguageId);
        Assert.Equal(0, provider.GetBasicScopeAttributes("xsource.css").LanguageId);
        Assert.Equal(0, provider.GetBasicScopeAttributes("punctuation.definition.tag.html").LanguageId);
        Assert.Equal(0, Provider().GetBasicScopeAttributes("source.css").LanguageId);
    }

    [Fact]
    public void BasicScopeAttributes_EmbeddedLanguage_EscapesScopeNames()
    {
        BasicScopeAttributesProvider provider = Provider(("source.c++", 5));

        Assert.Equal(5, provider.GetBasicScopeAttributes("source.c++").LanguageId);
        Assert.Equal(0, provider.GetBasicScopeAttributes("source.cc").LanguageId);
    }

    [Theory]
    [InlineData("comment.line.double-slash", (int)OptionalStandardTokenType.Comment)]
    [InlineData("string.quoted.double", (int)OptionalStandardTokenType.String)]
    [InlineData("constant.character.escape.regex", (int)OptionalStandardTokenType.RegEx)]
    [InlineData("meta.embedded.block.html", (int)OptionalStandardTokenType.Other)]
    [InlineData("keyword.control", (int)OptionalStandardTokenType.NotSet)]
    [InlineData("commentary", (int)OptionalStandardTokenType.NotSet)]
    [InlineData("meta.embeddedx", (int)OptionalStandardTokenType.NotSet)]
    public void BasicScopeAttributes_StandardTokenType_FromScopeName(string scope, int expected)
    {
        Assert.Equal((OptionalStandardTokenType)expected, Provider().GetBasicScopeAttributes(scope).TokenType);
    }

    [Fact]
    public void BasicScopeAttributes_NullScopeAndDefaults()
    {
        BasicScopeAttributesProvider provider = Provider();

        Assert.Equal(new BasicScopeAttributes(7, OptionalStandardTokenType.NotSet), provider.GetDefaultAttributes());
        Assert.Equal(new BasicScopeAttributes(0, OptionalStandardTokenType.Other), provider.GetBasicScopeAttributes(null));
        Assert.Same(provider.GetBasicScopeAttributes("string.quoted"), provider.GetBasicScopeAttributes("string.quoted"));
    }
}
