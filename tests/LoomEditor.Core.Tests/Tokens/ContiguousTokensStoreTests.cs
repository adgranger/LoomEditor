using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class ContiguousTokensStoreTests
{
    private const string Language = "plaintext";
    private const uint M1 = 1u << MetadataConsts.ForegroundOffset;
    private const uint M2 = 2u << MetadataConsts.ForegroundOffset;
    private const uint M3 = 3u << MetadataConsts.ForegroundOffset;
    private const uint M4 = 4u << MetadataConsts.ForegroundOffset;

    private static ContiguousTokensStore CreateStore() => new(new LanguageIdCodec());

    private static void Set(ContiguousTokensStore store, int lineIndex, int length, params uint[] tokens) =>
        store.SetTokens(Language, lineIndex, length, tokens, checkEquality: false);

    private static (int End, uint Metadata)[] Read(ContiguousTokensStore store, int lineIndex, string text)
    {
        LineTokens lineTokens = store.GetTokens(Language, lineIndex, text);
        var result = new (int, uint)[lineTokens.GetCount()];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (lineTokens.GetEndOffset(i), lineTokens.GetMetadata(i));
        }

        return result;
    }

    [Fact]
    public void GetTokens_LineWithoutTokens_ReturnsOneDefaultToken()
    {
        ContiguousTokensStore store = CreateStore();

        LineTokens tokens = store.GetTokens(Language, 3, "hello");

        Assert.Equal(1, tokens.GetCount());
        Assert.Equal(5, tokens.GetEndOffset(0));
        Assert.Equal(LanguageId.PlainText, TokenMetadata.GetLanguageId(tokens.GetMetadata(0)));
        Assert.Equal(ColorId.DefaultForeground, tokens.GetForeground(0));
    }

    [Fact]
    public void SetTokens_SameTokens_ReportsNoChange()
    {
        ContiguousTokensStore store = CreateStore();

        bool first = store.SetTokens(Language, 0, 6, [3, M1, 6, M2], checkEquality: true);
        bool second = store.SetTokens(Language, 0, 6, [3, M1, 6, M2], checkEquality: true);
        bool third = store.SetTokens(Language, 0, 6, [3, M1, 6, M3], checkEquality: true);

        Assert.True(first);
        Assert.False(second);
        Assert.True(third);
    }

    [Fact]
    public void AcceptEdit_DeleteAcrossLines_JoinsTokensOfFirstAndLastLine()
    {
        ContiguousTokensStore store = CreateStore();
        Set(store, 0, 6, 3, M1, 6, M2);
        Set(store, 1, 4, 2, M3, 4, M4);

        store.AcceptEdit(new Range(1, 3, 2, 3), eolCount: 0, firstLineLength: 0);

        Assert.Equal([(2, M1), (4, M4)], Read(store, 0, "abij"));
    }

    [Fact]
    public void AcceptEdit_InsertLines_ShiftsTokensDown()
    {
        ContiguousTokensStore store = CreateStore();
        Set(store, 0, 4, 2, M1, 4, M2);
        Set(store, 1, 2, 2, M3);

        store.AcceptEdit(new Range(1, 3, 1, 3), eolCount: 1, firstLineLength: 1);

        Assert.Equal([(3, M1)], Read(store, 0, "abZ"));
        Assert.Equal([(2, M3)], Read(store, 2, "xy"));
        Assert.NotEqual(M3, Read(store, 1, "cd")[0].Metadata);
    }
}
