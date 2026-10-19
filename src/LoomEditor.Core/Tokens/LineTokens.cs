// Ported from microsoft/vscode: src/vs/editor/common/tokens/lineTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Diagnostics;

namespace LoomEditor.Core.Tokens;

/// <summary>
/// The tokens of one line: pairs <c>[endOffset, metadata]</c> over the line text (Monaco: <c>LineTokens</c>).
/// Not ported: <c>getClassName</c>, <c>getInlineStyle</c> (CSS), <c>getTokensInRange</c> and <c>sliceZeroCopy</c>
/// (need <c>TokenArray</c> / <c>OffsetRange</c>, later phases), <c>toString</c>, <c>getStandardTokenTypeAtPosition</c>
/// (needs <c>ITextModel</c>, P4-29). A token / text length mismatch is a <see cref="Guard"/> failure (Debug) instead of
/// Monaco's <c>onUnexpectedError</c>.
/// </summary>
/// <remarks>Threading: immutable as long as nobody changes the array it was created with.</remarks>
internal sealed partial class LineTokens : IViewLineTokens
{
    /// <summary><c>defaultTokenMetadata</c>.</summary>
    public const uint DefaultTokenMetadata =
        ((uint)FontStyle.None << MetadataConsts.FontStyleOffset)
        | ((uint)ColorId.DefaultForeground << MetadataConsts.ForegroundOffset)
        | ((uint)ColorId.DefaultBackground << MetadataConsts.BackgroundOffset);

    private readonly uint[] _tokens;
    private readonly int _tokensCount;
    private readonly string _text;

    /// <summary>Creates the tokens of <paramref name="text"/> from <c>[endOffset, metadata]</c> pairs.</summary>
    public LineTokens(uint[] tokens, string text, ILanguageIdCodec decoder)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(decoder);
        Guard.Assert(
            (tokens.Length > 1 ? tokens[^2] : 0) == (uint)text.Length,
            "Token length and text length do not match!");
        _tokens = tokens;
        _tokensCount = tokens.Length >>> 1;
        _text = text;
        LanguageIdCodec = decoder;
    }

    /// <inheritdoc />
    public ILanguageIdCodec LanguageIdCodec { get; }

    /// <summary><c>createEmpty</c>: one token with the default metadata.</summary>
    public static LineTokens CreateEmpty(string lineContent, ILanguageIdCodec decoder) =>
        new([(uint)lineContent.Length, DefaultTokenMetadata], lineContent, decoder);

    /// <summary><c>createFromTextAndMetadata</c>.</summary>
    public static LineTokens CreateFromTextAndMetadata(IReadOnlyList<(string Text, uint Metadata)> data, ILanguageIdCodec decoder)
    {
        int offset = 0;
        var fullText = new StringBuilder();
        var tokens = new List<uint>();
        foreach ((string text, uint metadata) in data)
        {
            tokens.Add((uint)(offset + text.Length));
            tokens.Add(metadata);
            offset += text.Length;
            fullText.Append(text);
        }

        return new LineTokens([.. tokens], fullText.ToString(), decoder);
    }

    /// <summary><c>convertToEndOffset</c>: turns <c>[startOffset, metadata]</c> pairs into <c>[endOffset, metadata]</c>.</summary>
    public static void ConvertToEndOffset(uint[] tokens, int lineTextLength)
    {
        if (tokens.Length == 0)
        {
            // Monaco writes at index -2 of an empty array, which JS ignores.
            return;
        }

        int tokenCount = tokens.Length >>> 1;
        int lastTokenIndex = tokenCount - 1;
        for (int tokenIndex = 0; tokenIndex < lastTokenIndex; tokenIndex++)
        {
            tokens[tokenIndex << 1] = tokens[(tokenIndex + 1) << 1];
        }

        tokens[lastTokenIndex << 1] = (uint)lineTextLength;
    }

    /// <summary><c>findIndexInTokensArray</c>.</summary>
    public static int FindIndexInTokensArray(uint[] tokens, int desiredIndex)
    {
        if (tokens.Length <= 2)
        {
            return 0;
        }

        int low = 0;
        int high = (tokens.Length >>> 1) - 1;

        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            uint endOffset = tokens[mid << 1];

            if (endOffset == desiredIndex)
            {
                return mid + 1;
            }

            if (endOffset < desiredIndex)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary><c>getTextLength</c>.</summary>
    public int GetTextLength() => _text.Length;

    /// <inheritdoc />
    public bool Equals(IViewLineTokens other) => other is LineTokens o && SlicedEquals(o, 0, _tokensCount);

    /// <summary><c>slicedEquals</c>.</summary>
    public bool SlicedEquals(LineTokens other, int sliceFromTokenIndex, int sliceTokenCount)
    {
        if (!string.Equals(_text, other._text, StringComparison.Ordinal) || _tokensCount != other._tokensCount)
        {
            return false;
        }

        int from = sliceFromTokenIndex << 1;
        int to = from + (sliceTokenCount << 1);
        for (int i = from; i < to; i++)
        {
            if (_tokens[i] != other._tokens[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public string GetLineContent() => _text;

    /// <inheritdoc />
    public int GetCount() => _tokensCount;

    /// <summary><c>getStartOffset</c>.</summary>
    public int GetStartOffset(int tokenIndex) => tokenIndex > 0 ? (int)_tokens[(tokenIndex - 1) << 1] : 0;

    /// <inheritdoc />
    public uint GetMetadata(int tokenIndex) => _tokens[(tokenIndex << 1) + 1];

    /// <inheritdoc />
    public string GetLanguageId(int tokenIndex) =>
        LanguageIdCodec.DecodeLanguageId(TokenMetadata.GetLanguageId(GetMetadata(tokenIndex)));

    /// <inheritdoc />
    public StandardTokenType GetStandardTokenType(int tokenIndex) => TokenMetadata.GetTokenType(GetMetadata(tokenIndex));

    /// <inheritdoc />
    public int GetForeground(int tokenIndex) => TokenMetadata.GetForeground(GetMetadata(tokenIndex));

    /// <inheritdoc />
    public TokenPresentation GetPresentation(int tokenIndex) => TokenMetadata.GetPresentationFromMetadata(GetMetadata(tokenIndex));

    /// <inheritdoc />
    public int GetEndOffset(int tokenIndex) => (int)_tokens[tokenIndex << 1];

    /// <summary>Finds the token containing <paramref name="offset"/>.</summary>
    public int FindTokenIndexAtOffset(int offset) => FindIndexInTokensArray(_tokens, offset);

    /// <summary><c>inflate</c>.</summary>
    public IViewLineTokens Inflate() => this;

    /// <summary><c>sliceAndInflate</c>.</summary>
    public IViewLineTokens SliceAndInflate(int startOffset, int endOffset, int deltaOffset) =>
        new SliceLineTokens(this, startOffset, endOffset, deltaOffset);

    /// <inheritdoc />
    public string GetTokenText(int tokenIndex) => _text[GetStartOffset(tokenIndex)..GetEndOffset(tokenIndex)];

    /// <inheritdoc />
    public void ForEach(Action<int> callback)
    {
        for (int tokenIndex = 0; tokenIndex < _tokensCount; tokenIndex++)
        {
            callback(tokenIndex);
        }
    }

    /// <summary><c>withInserted</c>: pure; <paramref name="insertTokens"/> must be sorted by offset.</summary>
    public LineTokens WithInserted(IReadOnlyList<(int Offset, string Text, uint TokenMetadata)> insertTokens)
    {
        if (insertTokens.Count == 0)
        {
            return this;
        }

        int nextOriginalTokenIdx = 0;
        int nextInsertTokenIdx = 0;
        var text = new StringBuilder();
        var newTokens = new List<uint>();

        int originalEndOffset = 0;
        while (true)
        {
            int nextOriginalTokenEndOffset = nextOriginalTokenIdx < _tokensCount ? (int)_tokens[nextOriginalTokenIdx << 1] : -1;
            bool hasInsert = nextInsertTokenIdx < insertTokens.Count;

            if (nextOriginalTokenEndOffset != -1 && (!hasInsert || nextOriginalTokenEndOffset <= insertTokens[nextInsertTokenIdx].Offset))
            {
                // original token ends before next insert token
                text.Append(_text, originalEndOffset, nextOriginalTokenEndOffset - originalEndOffset);
                newTokens.Add((uint)text.Length);
                newTokens.Add(_tokens[(nextOriginalTokenIdx << 1) + 1]);
                nextOriginalTokenIdx++;
                originalEndOffset = nextOriginalTokenEndOffset;
            }
            else if (hasInsert)
            {
                (int offset, string insertText, uint tokenMetadata) = insertTokens[nextInsertTokenIdx];
                if (offset > originalEndOffset)
                {
                    // insert token is in the middle of the next token.
                    text.Append(_text, originalEndOffset, offset - originalEndOffset);
                    newTokens.Add((uint)text.Length);
                    newTokens.Add(_tokens[(nextOriginalTokenIdx << 1) + 1]);
                    originalEndOffset = offset;
                }

                text.Append(insertText);
                newTokens.Add((uint)text.Length);
                newTokens.Add(tokenMetadata);
                nextInsertTokenIdx++;
            }
            else
            {
                break;
            }
        }

        return new LineTokens([.. newTokens], text.ToString(), LanguageIdCodec);
    }
}
