// Ported from microsoft/vscode: src/vs/editor/common/tokens/contiguousMultilineTokensBuilder.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Groups the tokens of consecutive lines into blocks (Monaco: <c>ContiguousMultilineTokensBuilder</c>). Not ported:
/// <c>deserialize</c> and <c>serialize</c> (ADR 0008 decision 5).
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class ContiguousMultilineTokensBuilder
{
    private readonly List<ContiguousMultilineTokens> _tokens = [];

    /// <summary><c>add</c>: appends to the last block when <paramref name="lineNumber"/> follows it.</summary>
    public void Add(int lineNumber, uint[] lineTokens)
    {
        if (_tokens.Count > 0)
        {
            ContiguousMultilineTokens last = _tokens[^1];
            if (last.EndLineNumber + 1 == lineNumber)
            {
                last.AppendLineTokens(lineTokens);
                return;
            }
        }

        _tokens.Add(new ContiguousMultilineTokens(lineNumber, [lineTokens]));
    }

    /// <summary><c>finalize</c>.</summary>
    public List<ContiguousMultilineTokens> Finalize() => _tokens;
}
