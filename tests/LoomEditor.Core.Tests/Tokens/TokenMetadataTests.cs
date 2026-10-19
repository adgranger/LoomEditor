using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class TokenMetadataTests
{
    private static uint Build(int languageId, StandardTokenType tokenType, bool balanced, FontStyle fontStyle, int foreground, int background) =>
        ((uint)languageId << MetadataConsts.LanguageIdOffset)
        | ((uint)tokenType << MetadataConsts.TokenTypeOffset)
        | ((balanced ? 1u : 0u) << MetadataConsts.BalancedBracketsOffset)
        | ((uint)fontStyle << MetadataConsts.FontStyleOffset)
        | ((uint)foreground << MetadataConsts.ForegroundOffset)
        | ((uint)background << MetadataConsts.BackgroundOffset);

    [Fact]
    public void TokenMetadata_Getters_ReadEachField()
    {
        uint metadata = Build(
            200, StandardTokenType.RegEx, true, FontStyle.Underline | FontStyle.Bold, 511, 255);

        Assert.Equal(200, TokenMetadata.GetLanguageId(metadata));
        Assert.Equal(StandardTokenType.RegEx, TokenMetadata.GetTokenType(metadata));
        Assert.True(TokenMetadata.ContainsBalancedBrackets(metadata));
        Assert.Equal(FontStyle.Underline | FontStyle.Bold, TokenMetadata.GetFontStyle(metadata));
        Assert.Equal(511, TokenMetadata.GetForeground(metadata));
        Assert.Equal(255, TokenMetadata.GetBackground(metadata));
    }

    [Fact]
    public void TokenMetadata_Getters_FieldsDoNotOverlap()
    {
        uint metadata = Build(1, StandardTokenType.Comment, false, FontStyle.None, 3, 2);

        Assert.Equal(1, TokenMetadata.GetLanguageId(metadata));
        Assert.Equal(StandardTokenType.Comment, TokenMetadata.GetTokenType(metadata));
        Assert.False(TokenMetadata.ContainsBalancedBrackets(metadata));
        Assert.Equal(FontStyle.None, TokenMetadata.GetFontStyle(metadata));
        Assert.Equal(3, TokenMetadata.GetForeground(metadata));
        Assert.Equal(2, TokenMetadata.GetBackground(metadata));
    }

    [Fact]
    public void MetadataConsts_MasksAndOffsetsAgree()
    {
        Assert.Equal(MetadataConsts.FontStyleMask, MetadataConsts.ItalicMask | MetadataConsts.BoldMask | MetadataConsts.UnderlineMask | MetadataConsts.StrikethroughMask);
        Assert.Equal((uint)FontStyle.Italic << MetadataConsts.FontStyleOffset, MetadataConsts.ItalicMask);
        Assert.Equal((uint)FontStyle.Bold << MetadataConsts.FontStyleOffset, MetadataConsts.BoldMask);
        Assert.Equal((uint)FontStyle.Underline << MetadataConsts.FontStyleOffset, MetadataConsts.UnderlineMask);
        Assert.Equal((uint)FontStyle.Strikethrough << MetadataConsts.FontStyleOffset, MetadataConsts.StrikethroughMask);
        Assert.Equal(uint.MaxValue, MetadataConsts.LanguageIdMask | MetadataConsts.TokenTypeMask | MetadataConsts.BalancedBracketsMask
            | MetadataConsts.FontStyleMask | MetadataConsts.ForegroundMask | MetadataConsts.BackgroundMask);
    }

    [Theory]
    [InlineData((int)FontStyle.None, false, false, false, false)]
    [InlineData((int)FontStyle.Italic, true, false, false, false)]
    [InlineData((int)FontStyle.Bold, false, true, false, false)]
    [InlineData((int)FontStyle.Underline, false, false, true, false)]
    [InlineData((int)FontStyle.Strikethrough, false, false, false, true)]
    [InlineData((int)(FontStyle.Italic | FontStyle.Strikethrough), true, false, false, true)]
    public void TokenMetadata_GetPresentation_FontStyles(int fontStyle, bool italic, bool bold, bool underline, bool strikethrough)
    {
        uint metadata = Build(1, StandardTokenType.Other, false, (FontStyle)fontStyle, 7, 2);

        TokenPresentation presentation = TokenMetadata.GetPresentationFromMetadata(metadata);

        Assert.Equal(new TokenPresentation(7, italic, bold, underline, strikethrough), presentation);
    }

    [Fact]
    public void NullTokenizeEncoded_OneTokenWithDefaultMetadata()
    {
        EncodedTokenizationResult result = NullTokenize.NullTokenizeEncoded(5, null);

        Assert.Equal(2, result.Tokens.Length);
        Assert.Equal(0u, result.Tokens[0]);
        uint metadata = result.Tokens[1];
        Assert.Equal(5, TokenMetadata.GetLanguageId(metadata));
        Assert.Equal(StandardTokenType.Other, TokenMetadata.GetTokenType(metadata));
        Assert.Equal(FontStyle.None, TokenMetadata.GetFontStyle(metadata));
        Assert.Equal(ColorId.DefaultForeground, TokenMetadata.GetForeground(metadata));
        Assert.Equal(ColorId.DefaultBackground, TokenMetadata.GetBackground(metadata));
        Assert.Same(NullState.Instance, result.EndState);
    }

    [Fact]
    public void NullTokenizeEncoded_KeepsTheGivenState()
    {
        IState state = NullState.Instance.Clone();
        var other = new OtherState();

        Assert.Same(state, NullState.Instance);
        Assert.True(NullState.Instance.Equals(NullState.Instance));
        Assert.False(NullState.Instance.Equals(other));
        Assert.Same(other, NullTokenize.NullTokenizeEncoded(1, other).EndState);
    }

    private sealed class OtherState : IState
    {
        public IState Clone() => this;

        public bool Equals(IState? other) => ReferenceEquals(this, other);
    }
}
