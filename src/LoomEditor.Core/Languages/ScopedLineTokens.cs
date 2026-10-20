// Ported from microsoft/vscode: src/vs/editor/common/languages/supports.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Languages;

/// <summary>
/// The tokens of one language around an offset of a line (Monaco: <c>ScopedLineTokens</c>). Monaco's module functions
/// <c>createScopedLineTokens</c> and <c>ignoreBracketsInToken</c> are static members.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class ScopedLineTokens
{
    private readonly LineTokens _actual;
    private readonly int _firstTokenIndex;
    private readonly int _lastTokenIndex;
    private readonly int _lastCharOffset;

    private ScopedLineTokens(LineTokens actual, string languageId, int firstTokenIndex, int lastTokenIndex, int firstCharOffset, int lastCharOffset)
    {
        _actual = actual;
        LanguageId = languageId;
        _firstTokenIndex = firstTokenIndex;
        _lastTokenIndex = lastTokenIndex;
        FirstCharOffset = firstCharOffset;
        _lastCharOffset = lastCharOffset;
        LanguageIdCodec = actual.LanguageIdCodec;
    }

    public ILanguageIdCodec LanguageIdCodec { get; }

    public string LanguageId { get; }

    public int FirstCharOffset { get; }

    /// <summary>The tokens of the language at <paramref name="offset"/> (Monaco: <c>createScopedLineTokens</c>).</summary>
    public static ScopedLineTokens Create(LineTokens context, int offset)
    {
        int tokenCount = context.GetCount();
        int tokenIndex = context.FindTokenIndexAtOffset(offset);
        string desiredLanguageId = context.GetLanguageId(tokenIndex);

        int lastTokenIndex = tokenIndex;
        while (lastTokenIndex + 1 < tokenCount && context.GetLanguageId(lastTokenIndex + 1) == desiredLanguageId)
        {
            lastTokenIndex++;
        }

        int firstTokenIndex = tokenIndex;
        while (firstTokenIndex > 0 && context.GetLanguageId(firstTokenIndex - 1) == desiredLanguageId)
        {
            firstTokenIndex--;
        }

        return new ScopedLineTokens(
            context,
            desiredLanguageId,
            firstTokenIndex,
            lastTokenIndex + 1,
            context.GetStartOffset(firstTokenIndex),
            context.GetEndOffset(lastTokenIndex));
    }

    /// <summary>True for comment, string and regex tokens (Monaco: <c>ignoreBracketsInToken</c>).</summary>
    public static bool IgnoreBracketsInToken(StandardTokenType standardTokenType) =>
        ((int)standardTokenType & ((int)StandardTokenType.Comment | (int)StandardTokenType.String | (int)StandardTokenType.RegEx)) != 0;

    public string GetLineContent()
    {
        string actualLineContent = _actual.GetLineContent();
        return actualLineContent.Substring(FirstCharOffset, _lastCharOffset - FirstCharOffset);
    }

    public int GetLineLength() => _lastCharOffset - FirstCharOffset;

    public string GetActualLineContentBefore(int offset)
    {
        string actualLineContent = _actual.GetLineContent();
        return actualLineContent.Substring(0, FirstCharOffset + offset);
    }

    public int GetTokenCount() => _lastTokenIndex - _firstTokenIndex;

    public int FindTokenIndexAtOffset(int offset) => _actual.FindTokenIndexAtOffset(offset + FirstCharOffset) - _firstTokenIndex;

    public StandardTokenType GetStandardTokenType(int tokenIndex) => _actual.GetStandardTokenType(tokenIndex + _firstTokenIndex);

    public IViewLineTokens ToIViewLineTokens() => _actual.SliceAndInflate(FirstCharOffset, _lastCharOffset, 0);
}
