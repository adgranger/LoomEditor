// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>
/// A tokenization support that returns the given tokens for each line text (Monaco: the hand-written
/// <c>tokenizeEncoded</c> of <c>issue #95843</c> and <c>issue #88075</c>). A line it does not know throws.
/// </summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class LineTokensSupport(IReadOnlyDictionary<string, uint[]> tokensByLine) : ITokenizationSupport
{
    public IState GetInitialState() => NullState.Instance;

    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
        tokensByLine.TryGetValue(line, out uint[]? tokens)
            ? new EncodedTokenizationResult(tokens, state)
            : throw new InvalidOperationException("Unexpected");
}
