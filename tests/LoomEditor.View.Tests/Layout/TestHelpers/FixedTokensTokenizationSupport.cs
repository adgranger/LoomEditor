// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts (suite 'SplitLinesCollection', setup)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.Layout.TestHelpers;

/// <summary>A tokenization support that returns the next given line of tokens for each line it is asked about (Monaco: the suite's <c>tokenizeEncoded</c>).</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class FixedTokensTokenizationSupport(IReadOnlyList<(int StartIndex, int Value)[]> tokens) : ITokenizationSupport
{
    private int _lineIndex;

    public IState GetInitialState() => NullState.Instance;

    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
    {
        (int StartIndex, int Value)[] lineTokens = tokens[_lineIndex++];

        uint[] result = new uint[2 * lineTokens.Length];
        for (int i = 0; i < lineTokens.Length; i++)
        {
            result[2 * i] = (uint)lineTokens[i].StartIndex;
            result[(2 * i) + 1] = (uint)lineTokens[i].Value << MetadataConsts.ForegroundOffset;
        }

        return new EncodedTokenizationResult(result, state);
    }
}
