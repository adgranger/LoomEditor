using LoomEditor.Core.Languages;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages;

// Monaco has no test for supports.ts: all tests are new.
public class ScopedLineTokensTests
{
    // "<b>" in html, "x=1;" in javascript, "</b>" in html.
    private static LineTokens EmbeddedLine()
    {
        var codec = new LanguageIdCodec();
        codec.Register("html");
        codec.Register("javascript");
        uint Meta(string language, StandardTokenType type = StandardTokenType.Other) =>
            ((uint)codec.EncodeLanguageId(language) << MetadataConsts.LanguageIdOffset) | ((uint)type << MetadataConsts.TokenTypeOffset);

        return LineTokens.CreateFromTextAndMetadata(
            [("<b>", Meta("html")), ("x=", Meta("javascript")), ("1;", Meta("javascript", StandardTokenType.String)), ("</b>", Meta("html"))],
            codec);
    }

    [Fact]
    public void Create_EmbeddedLanguage_ScopesToTheTokensOfThatLanguage()
    {
        ScopedLineTokens scoped = ScopedLineTokens.Create(EmbeddedLine(), 4);

        Assert.Equal("javascript", scoped.LanguageId);
        Assert.Equal(3, scoped.FirstCharOffset);
        Assert.Equal("x=1;", scoped.GetLineContent());
        Assert.Equal(4, scoped.GetLineLength());
        Assert.Equal(2, scoped.GetTokenCount());
        Assert.Equal(1, scoped.FindTokenIndexAtOffset(2));
        Assert.Equal(StandardTokenType.String, scoped.GetStandardTokenType(1));
        Assert.Equal(4, scoped.ToIViewLineTokens().GetEndOffset(1));
    }

    [Fact]
    public void GetActualLineContentBefore_IncludesTextBeforeTheScope()
    {
        ScopedLineTokens scoped = ScopedLineTokens.Create(EmbeddedLine(), 4);

        Assert.Equal("<b>x=", scoped.GetActualLineContentBefore(2));
    }

    [Fact]
    public void IgnoreBracketsInToken_OnlyOtherIsFalse()
    {
        Assert.False(ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType.Other));
        Assert.True(ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType.Comment));
        Assert.True(ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType.String));
        Assert.True(ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType.RegEx));
    }

    [Fact]
    public void CreateFakeScopedLineTokens_GivesOneTokenPerText()
    {
        ScopedLineTokens scoped = ModesTestUtils.CreateFakeScopedLineTokens(
            [new TokenText("a ", StandardTokenType.Other), new TokenText("'b'", StandardTokenType.String)]);

        Assert.Equal("a 'b'", scoped.GetLineContent());
        Assert.Equal(StandardTokenType.String, scoped.GetStandardTokenType(scoped.FindTokenIndexAtOffset(3)));
    }
}
