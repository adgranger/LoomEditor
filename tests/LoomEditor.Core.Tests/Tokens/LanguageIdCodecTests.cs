using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class LanguageIdCodecTests
{
    [Fact]
    public void LanguageIdCodec_UnknownLanguage_EncodesToNull_AndDecodesToNullLanguage()
    {
        var codec = new LanguageIdCodec();

        Assert.Equal(LanguageId.Null, codec.EncodeLanguageId("csharp"));
        Assert.Equal(LanguageIdCodec.NullLanguageId, codec.DecodeLanguageId(42));
        Assert.Equal(LanguageIdCodec.NullLanguageId, codec.DecodeLanguageId(-1));
    }

    [Fact]
    public void LanguageIdCodec_KnowsNullAndPlainText()
    {
        var codec = new LanguageIdCodec();

        Assert.Equal(LanguageId.Null, codec.EncodeLanguageId("vs.editor.nullLanguage"));
        Assert.Equal(LanguageId.PlainText, codec.EncodeLanguageId("plaintext"));
        Assert.Equal("plaintext", codec.DecodeLanguageId(LanguageId.PlainText));
        Assert.Equal("vs.editor.nullLanguage", codec.DecodeLanguageId(LanguageId.Null));
    }

    [Fact]
    public void LanguageIdCodec_Register_AssignsIdsFromTwo_AndIsIdempotent()
    {
        var codec = new LanguageIdCodec();

        codec.Register("csharp");
        codec.Register("json");
        codec.Register("csharp");

        Assert.Equal(2, codec.EncodeLanguageId("csharp"));
        Assert.Equal(3, codec.EncodeLanguageId("json"));
        Assert.Equal("json", codec.DecodeLanguageId(3));
    }
}
