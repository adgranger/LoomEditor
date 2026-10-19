// Ported from microsoft/vscode: src/vs/editor/common/tokens/contiguousTokensEditing.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Edits the <c>[endOffset, metadata]</c> pairs of one line (Monaco: <c>ContiguousTokensEditing</c>). A <c>null</c>
/// array means "tokens unknown"; <see cref="EmptyLineTokens"/> means "no tokens needed" (empty line). Arrays may be
/// modified in place and returned.
/// </summary>
/// <remarks>
/// Threading: stateless. Monaco's <c>toUint32Array</c> (<c>ArrayBuffer</c> to <c>Uint32Array</c>) is not ported: lines
/// are stored as <c>uint[]</c>, so it would be the identity.
/// </remarks>
internal static class ContiguousTokensEditing
{
    /// <summary><c>EMPTY_LINE_TOKENS</c>: compared by reference.</summary>
    public static readonly uint[] EmptyLineTokens = [];

    /// <summary><c>deleteBeginning</c>.</summary>
    public static uint[]? DeleteBeginning(uint[]? lineTokens, int toChIndex)
    {
        if (lineTokens is null || ReferenceEquals(lineTokens, EmptyLineTokens))
        {
            return lineTokens;
        }

        return Delete(lineTokens, 0, toChIndex);
    }

    /// <summary><c>deleteEnding</c>.</summary>
    public static uint[]? DeleteEnding(uint[]? lineTokens, int fromChIndex)
    {
        if (lineTokens is null || ReferenceEquals(lineTokens, EmptyLineTokens))
        {
            return lineTokens;
        }

        int lineTextLength = (int)lineTokens[^2];
        return Delete(lineTokens, fromChIndex, lineTextLength);
    }

    /// <summary><c>delete</c>.</summary>
    public static uint[]? Delete(uint[]? lineTokens, int fromChIndex, int toChIndex)
    {
        if (lineTokens is null || ReferenceEquals(lineTokens, EmptyLineTokens) || fromChIndex == toChIndex)
        {
            return lineTokens;
        }

        uint[] tokens = lineTokens;
        int tokensCount = tokens.Length >>> 1;

        // special case: deleting everything
        if (fromChIndex == 0 && tokens[^2] == (uint)toChIndex)
        {
            return EmptyLineTokens;
        }

        int fromTokenIndex = LineTokens.FindIndexInTokensArray(tokens, fromChIndex);
        int fromTokenStartOffset = fromTokenIndex > 0 ? (int)tokens[(fromTokenIndex - 1) << 1] : 0;
        int fromTokenEndOffset = (int)tokens[fromTokenIndex << 1];
        int delta = toChIndex - fromChIndex;

        if (toChIndex < fromTokenEndOffset)
        {
            // the delete range is inside a single token
            for (int i = fromTokenIndex; i < tokensCount; i++)
            {
                tokens[i << 1] -= (uint)delta;
            }

            return lineTokens;
        }

        int dest;
        int lastEnd;
        if (fromTokenStartOffset != fromChIndex)
        {
            tokens[fromTokenIndex << 1] = (uint)fromChIndex;
            dest = (fromTokenIndex + 1) << 1;
            lastEnd = fromChIndex;
        }
        else
        {
            dest = fromTokenIndex << 1;
            lastEnd = fromTokenStartOffset;
        }

        for (int tokenIndex = fromTokenIndex + 1; tokenIndex < tokensCount; tokenIndex++)
        {
            int tokenEndOffset = (int)tokens[tokenIndex << 1] - delta;
            if (tokenEndOffset > lastEnd)
            {
                tokens[dest++] = (uint)tokenEndOffset;
                tokens[dest++] = tokens[(tokenIndex << 1) + 1];
                lastEnd = tokenEndOffset;
            }
        }

        if (dest == tokens.Length)
        {
            // nothing to trim
            return lineTokens;
        }

        return tokens.AsSpan(0, dest).ToArray();
    }

    /// <summary><c>append</c>.</summary>
    public static uint[]? Append(uint[]? lineTokens, uint[]? otherTokens)
    {
        if (ReferenceEquals(otherTokens, EmptyLineTokens))
        {
            return lineTokens;
        }

        if (ReferenceEquals(lineTokens, EmptyLineTokens))
        {
            return otherTokens;
        }

        if (lineTokens is null)
        {
            return lineTokens;
        }

        if (otherTokens is null)
        {
            // cannot determine combined line length...
            return null;
        }

        int otherTokensCount = otherTokens.Length >>> 1;

        var result = new uint[lineTokens.Length + otherTokens.Length];
        lineTokens.CopyTo(result, 0);
        int dest = lineTokens.Length;
        uint delta = lineTokens[^2];
        for (int i = 0; i < otherTokensCount; i++)
        {
            result[dest++] = otherTokens[i << 1] + delta;
            result[dest++] = otherTokens[(i << 1) + 1];
        }

        return result;
    }

    /// <summary><c>insert</c>.</summary>
    public static uint[]? Insert(uint[]? lineTokens, int chIndex, int textLength)
    {
        if (lineTokens is null || ReferenceEquals(lineTokens, EmptyLineTokens))
        {
            // nothing to do
            return lineTokens;
        }

        int tokensCount = lineTokens.Length >>> 1;

        int fromTokenIndex = LineTokens.FindIndexInTokensArray(lineTokens, chIndex);
        if (fromTokenIndex > 0)
        {
            uint fromTokenStartOffset = lineTokens[(fromTokenIndex - 1) << 1];
            if (fromTokenStartOffset == (uint)chIndex)
            {
                fromTokenIndex--;
            }
        }

        for (int tokenIndex = fromTokenIndex; tokenIndex < tokensCount; tokenIndex++)
        {
            lineTokens[tokenIndex << 1] += (uint)textLength;
        }

        return lineTokens;
    }
}
