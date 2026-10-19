// Ported from microsoft/vscode: src/vs/editor/common/tokens/lineTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

internal sealed partial class LineTokens
{
    /// <summary>Monaco's <c>SliceLineTokens</c>: a window on a <see cref="LineTokens"/>, shifted by a delta.</summary>
    /// <remarks>Threading: immutable.</remarks>
    private sealed class SliceLineTokens : IViewLineTokens
    {
        private readonly LineTokens _source;
        private readonly int _startOffset;
        private readonly int _endOffset;
        private readonly int _deltaOffset;
        private readonly int _firstTokenIndex;
        private readonly int _tokensCount;

        public SliceLineTokens(LineTokens source, int startOffset, int endOffset, int deltaOffset)
        {
            _source = source;
            _startOffset = startOffset;
            _endOffset = endOffset;
            _deltaOffset = deltaOffset;
            _firstTokenIndex = source.FindTokenIndexAtOffset(startOffset);
            LanguageIdCodec = source.LanguageIdCodec;

            _tokensCount = 0;
            for (int i = _firstTokenIndex, len = source.GetCount(); i < len; i++)
            {
                if (source.GetStartOffset(i) >= endOffset)
                {
                    break;
                }

                _tokensCount++;
            }
        }

        public ILanguageIdCodec LanguageIdCodec { get; }

        public uint GetMetadata(int tokenIndex) => _source.GetMetadata(_firstTokenIndex + tokenIndex);

        public string GetLanguageId(int tokenIndex) => _source.GetLanguageId(_firstTokenIndex + tokenIndex);

        public string GetLineContent() => _source.GetLineContent()[_startOffset.._endOffset];

        public bool Equals(IViewLineTokens other) =>
            other is SliceLineTokens o
            && _startOffset == o._startOffset
            && _endOffset == o._endOffset
            && _deltaOffset == o._deltaOffset
            && _source.SlicedEquals(o._source, _firstTokenIndex, _tokensCount);

        public int GetCount() => _tokensCount;

        public StandardTokenType GetStandardTokenType(int tokenIndex) => _source.GetStandardTokenType(_firstTokenIndex + tokenIndex);

        public int GetForeground(int tokenIndex) => _source.GetForeground(_firstTokenIndex + tokenIndex);

        public int GetEndOffset(int tokenIndex) =>
            Math.Min(_endOffset, _source.GetEndOffset(_firstTokenIndex + tokenIndex)) - _startOffset + _deltaOffset;

        public TokenPresentation GetPresentation(int tokenIndex) => _source.GetPresentation(_firstTokenIndex + tokenIndex);

        public int FindTokenIndexAtOffset(int offset) =>
            _source.FindTokenIndexAtOffset(offset + _startOffset - _deltaOffset) - _firstTokenIndex;

        public string GetTokenText(int tokenIndex)
        {
            int adjustedTokenIndex = _firstTokenIndex + tokenIndex;
            int tokenStartOffset = _source.GetStartOffset(adjustedTokenIndex);
            int tokenEndOffset = _source.GetEndOffset(adjustedTokenIndex);
            string text = _source.GetTokenText(adjustedTokenIndex);
            if (tokenStartOffset < _startOffset)
            {
                text = text[(_startOffset - tokenStartOffset)..];
            }

            if (tokenEndOffset > _endOffset)
            {
                text = text[..(text.Length - (tokenEndOffset - _endOffset))];
            }

            return text;
        }

        public void ForEach(Action<int> callback)
        {
            for (int tokenIndex = 0; tokenIndex < GetCount(); tokenIndex++)
            {
                callback(tokenIndex);
            }
        }
    }
}
