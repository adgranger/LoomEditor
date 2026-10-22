// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts (getLineTokensWithInjections)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Tokens;

/// <summary>Inserts injected text into the tokens of a line (Monaco: <c>getLineTokensWithInjections</c>).</summary>
/// <remarks>
/// Not ported: the <c>tokens</c> of injected text (pre-tokenized injected text); every injection gets the default token metadata.
/// Threading: stateless.
/// </remarks>
internal static class LineTokensWithInjections
{
    /// <summary>The tokens with the injected texts inserted at their offsets, or <paramref name="tokens"/> itself when nothing is injected.</summary>
    public static LineTokens GetLineTokensWithInjections(
        LineTokens tokens,
        IReadOnlyList<ModelDecorationInjectedTextOptions>? injectionOptions,
        IReadOnlyList<int>? injectionOffsets)
    {
        if (injectionOffsets is null)
        {
            return tokens;
        }

        var tokensToInsert = new List<(int Offset, string Text, uint TokenMetadata)>(injectionOffsets.Count);
        for (int idx = 0; idx < injectionOffsets.Count; idx++)
        {
            tokensToInsert.Add((injectionOffsets[idx], injectionOptions![idx].Content, LineTokens.DefaultTokenMetadata));
        }

        return tokens.WithInserted(tokensToInsert);
    }
}
