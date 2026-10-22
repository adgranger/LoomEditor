// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts (getLineTokensWithInjections)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class LineTokensWithInjectionsTests
{
    private static LineTokens CreateTokens() =>
        new([5u, 1u << MetadataConsts.ForegroundOffset, 10u, 2u << MetadataConsts.ForegroundOffset], "abcdefghij", new LanguageIdCodec());

    private static ModelDecorationInjectedTextOptions Inj(string content) =>
        ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = content });

    [Fact]
    public void GetLineTokensWithInjections_NoInjections_ReturnsTheSameTokens()
    {
        LineTokens tokens = CreateTokens();

        Assert.Same(tokens, LineTokensWithInjections.GetLineTokensWithInjections(tokens, null, null));
    }

    [Fact]
    public void GetLineTokensWithInjections_InsertsDefaultTokens()
    {
        LineTokens result = LineTokensWithInjections.GetLineTokensWithInjections(CreateTokens(), [Inj("XY")], [3]);

        Assert.Equal("abcXYdefghij", result.GetLineContent());
        Assert.Equal(4, result.GetCount());
        Assert.Equal([3, 5, 7, 12], Enumerable.Range(0, 4).Select(result.GetEndOffset));
        Assert.Equal(LineTokens.DefaultTokenMetadata, result.GetMetadata(1));
    }
}
