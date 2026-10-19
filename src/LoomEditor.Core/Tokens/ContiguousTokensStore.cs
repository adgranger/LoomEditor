// Ported from microsoft/vscode: src/vs/editor/common/tokens/contiguousTokensStore.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Tokens;

/// <summary>
/// The tokens of every line of a text model (Monaco: <c>ContiguousTokensStore</c>). Not ported:
/// <c>setMultilineTokens</c> (needs <c>ContiguousMultilineTokens</c> and the model, P4-26 / P4-29).
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class ContiguousTokensStore
{
    private readonly ILanguageIdCodec _languageIdCodec;
    private List<uint[]?> _lineTokens = [];
    private int _len;

    /// <summary>Creates an empty store.</summary>
    public ContiguousTokensStore(ILanguageIdCodec languageIdCodec)
    {
        ArgumentNullException.ThrowIfNull(languageIdCodec);
        _languageIdCodec = languageIdCodec;
    }

    /// <summary><c>flush</c>.</summary>
    public void Flush()
    {
        _lineTokens = [];
        _len = 0;
    }

    /// <summary><c>hasTokens</c>.</summary>
    public bool HasTokens => _lineTokens.Count > 0;

    /// <summary><c>getTokens</c>: a line without tokens gets one default token.</summary>
    public LineTokens GetTokens(string topLevelLanguageId, int lineIndex, string lineText)
    {
        uint[]? rawLineTokens = lineIndex < _len ? _lineTokens[lineIndex] : null;

        if (rawLineTokens is not null && !ReferenceEquals(rawLineTokens, ContiguousTokensEditing.EmptyLineTokens))
        {
            return new LineTokens(rawLineTokens, lineText, _languageIdCodec);
        }

        uint[] lineTokens = [(uint)lineText.Length, GetDefaultMetadata(_languageIdCodec.EncodeLanguageId(topLevelLanguageId))];
        return new LineTokens(lineTokens, lineText, _languageIdCodec);
    }

    private static uint[] MassageTokens(int topLevelLanguageId, int lineTextLength, uint[]? tokens)
    {
        if (lineTextLength == 0)
        {
            bool hasDifferentLanguageId = false;
            if (tokens is { Length: > 1 })
            {
                hasDifferentLanguageId = TokenMetadata.GetLanguageId(tokens[1]) != topLevelLanguageId;
            }

            if (!hasDifferentLanguageId)
            {
                return ContiguousTokensEditing.EmptyLineTokens;
            }
        }

        if (tokens is null || tokens.Length == 0)
        {
            return [(uint)lineTextLength, GetDefaultMetadata(topLevelLanguageId)];
        }

        // Ensure the last token covers the end of the text
        tokens[^2] = (uint)lineTextLength;
        return tokens;
    }

    private void EnsureLine(int lineIndex)
    {
        while (lineIndex >= _len)
        {
            _lineTokens.Add(null);
            _len++;
        }
    }

    private void DeleteLines(int start, int deleteCount)
    {
        if (deleteCount == 0)
        {
            return;
        }

        if (start + deleteCount > _len)
        {
            deleteCount = _len - start;
        }

        _lineTokens.RemoveRange(start, deleteCount);
        _len -= deleteCount;
    }

    private void InsertLines(int insertIndex, int insertCount)
    {
        if (insertCount == 0)
        {
            return;
        }

        _lineTokens.InsertRange(insertIndex, new uint[]?[insertCount]);
        _len += insertCount;
    }

    /// <summary><c>setTokens</c>: returns whether the tokens changed (only computed when <paramref name="checkEquality"/>).</summary>
    public bool SetTokens(string topLevelLanguageId, int lineIndex, int lineTextLength, uint[]? tokens, bool checkEquality)
    {
        uint[] massaged = MassageTokens(_languageIdCodec.EncodeLanguageId(topLevelLanguageId), lineTextLength, tokens);
        EnsureLine(lineIndex);
        uint[]? oldTokens = _lineTokens[lineIndex];
        _lineTokens[lineIndex] = massaged;

        return checkEquality && !TokensEqual(oldTokens, massaged);
    }

    private static bool TokensEqual(uint[]? a, uint[]? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.AsSpan().SequenceEqual(b);
    }

    /// <summary>
    /// <c>acceptEdit</c>: <paramref name="eolCount"/> and <paramref name="firstLineLength"/> describe the inserted text
    /// (number of line breaks, length of its first line).
    /// </summary>
    public void AcceptEdit(Range range, int eolCount, int firstLineLength)
    {
        AcceptDeleteRange(range);
        AcceptInsertText(new Position(range.StartLineNumber, range.StartColumn), eolCount, firstLineLength);
    }

    private void AcceptDeleteRange(Range range)
    {
        int firstLineIndex = range.StartLineNumber - 1;
        if (firstLineIndex >= _len)
        {
            return;
        }

        if (range.StartLineNumber == range.EndLineNumber)
        {
            if (range.StartColumn == range.EndColumn)
            {
                // Nothing to delete
                return;
            }

            _lineTokens[firstLineIndex] = ContiguousTokensEditing.Delete(_lineTokens[firstLineIndex], range.StartColumn - 1, range.EndColumn - 1);
            return;
        }

        _lineTokens[firstLineIndex] = ContiguousTokensEditing.DeleteEnding(_lineTokens[firstLineIndex], range.StartColumn - 1);

        int lastLineIndex = range.EndLineNumber - 1;
        uint[]? lastLineTokens = null;
        if (lastLineIndex < _len)
        {
            lastLineTokens = ContiguousTokensEditing.DeleteBeginning(_lineTokens[lastLineIndex], range.EndColumn - 1);
        }

        // Take remaining text on last line and append it to remaining text on first line
        _lineTokens[firstLineIndex] = ContiguousTokensEditing.Append(_lineTokens[firstLineIndex], lastLineTokens);

        // Delete middle lines
        DeleteLines(range.StartLineNumber, range.EndLineNumber - range.StartLineNumber);
    }

    private void AcceptInsertText(Position position, int eolCount, int firstLineLength)
    {
        if (eolCount == 0 && firstLineLength == 0)
        {
            // Nothing to insert
            return;
        }

        int lineIndex = position.LineNumber - 1;
        if (lineIndex >= _len)
        {
            return;
        }

        if (eolCount == 0)
        {
            // Inserting text on one line
            _lineTokens[lineIndex] = ContiguousTokensEditing.Insert(_lineTokens[lineIndex], position.Column - 1, firstLineLength);
            return;
        }

        _lineTokens[lineIndex] = ContiguousTokensEditing.DeleteEnding(_lineTokens[lineIndex], position.Column - 1);
        _lineTokens[lineIndex] = ContiguousTokensEditing.Insert(_lineTokens[lineIndex], position.Column - 1, firstLineLength);

        InsertLines(position.LineNumber, eolCount);
    }

    private static uint GetDefaultMetadata(int topLevelLanguageId) =>
        ((uint)topLevelLanguageId << MetadataConsts.LanguageIdOffset)
        | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset)
        | ((uint)FontStyle.None << MetadataConsts.FontStyleOffset)
        | ((uint)ColorId.DefaultForeground << MetadataConsts.ForegroundOffset)
        | ((uint)ColorId.DefaultBackground << MetadataConsts.BackgroundOffset)
        // If there is no grammar, we just take a guess and try to match brackets.
        | MetadataConsts.BalancedBracketsMask;
}
