// Ported from microsoft/vscode: src/vs/editor/test/common/modesTestUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>A token of a fake line: its text and its standard token type.</summary>
internal readonly record struct TokenText(string Text, StandardTokenType Type);

/// <summary>Builds scoped line tokens from token texts, like Monaco's test utilities.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class ModesTestUtils
{
    public static ScopedLineTokens CreateFakeScopedLineTokens(IReadOnlyList<TokenText> rawTokens)
    {
        if (rawTokens.Count == 0)
        {
            // JavaScript reads the missing metadata of an empty token array as 0 (an `Other` token of language 0)
            // where .NET throws: use that empty token explicitly.
            rawTokens = [new TokenText("", StandardTokenType.Other)];
        }

        uint[] tokens = new uint[rawTokens.Count << 1];
        string line = "";

        for (int i = 0; i < rawTokens.Count; i++)
        {
            TokenText rawToken = rawTokens[i];

            int startOffset = line.Length;
            uint metadata = (uint)rawToken.Type << MetadataConsts.TokenTypeOffset;

            tokens[i << 1] = (uint)startOffset;
            tokens[(i << 1) + 1] = metadata;
            line += rawToken.Text;
        }

        LineTokens.ConvertToEndOffset(tokens, line.Length);
        return ScopedLineTokens.Create(new LineTokens(tokens, line, new LanguageIdCodec()), 0);
    }
}
