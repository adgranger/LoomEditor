// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// Reads the model's syntax tokens as bracket and text tokens: brackets are only found inside "other" tokens that may
/// contain balanced brackets, with the brackets of the language of each token (Monaco: <c>TextBufferTokenizer</c>).
/// </summary>
/// <remarks>
/// <para>
/// Like Monaco, <see cref="Read"/> returns the token stored by the last <see cref="Peek"/> and never clears it: callers
/// always call <see cref="Peek"/> before each <see cref="Read"/> that follows a peek.
/// </para>
/// <para>Threading: single-threaded (UI thread), like the tree that owns it.</para>
/// </remarks>
internal sealed class TextBufferTokenizer : ITokenizer
{
    private readonly ITokenizerSource _textModel;
    private readonly int _textBufferLineCount;
    private readonly int _textBufferLastLineLength;
    private readonly NonPeekableTextBufferTokenizer _reader;

    private Length _offset;
    private bool _didPeek;
    private Token? _peeked;

    /// <summary>Creates a tokenizer at the start of the model.</summary>
    public TextBufferTokenizer(ITokenizerSource textModel, LanguageAgnosticBracketTokens bracketTokens)
    {
        ArgumentNullException.ThrowIfNull(textModel);
        ArgumentNullException.ThrowIfNull(bracketTokens);

        _textModel = textModel;
        _reader = new NonPeekableTextBufferTokenizer(textModel, bracketTokens);
        _offset = Length.Zero;
        _didPeek = false;
        _peeked = null;
        _textBufferLineCount = textModel.GetLineCount();
        _textBufferLastLineLength = textModel.GetLineLength(_textBufferLineCount);
    }

    /// <inheritdoc />
    public Length Offset => _offset;

    /// <inheritdoc />
    public Length Length => Length.Of(_textBufferLineCount - 1, _textBufferLastLineLength);

    /// <inheritdoc />
    public string GetText()
    {
        return _textModel.GetValue();
    }

    /// <inheritdoc />
    public void Skip(Length length)
    {
        _didPeek = false;
        _offset = Length.Add(_offset, length);
        var obj = _offset.ToObj();
        _reader.SetPosition(obj.LineCount, obj.ColumnCount);
    }

    /// <inheritdoc />
    public Token? Read()
    {
        Token? token;
        if (_peeked is not null)
        {
            _didPeek = false;
            token = _peeked;
        }
        else
        {
            token = _reader.Read();
        }

        if (token is not null)
        {
            _offset = Length.Add(_offset, token.Length);
        }

        return token;
    }

    /// <inheritdoc />
    public Token? Peek()
    {
        if (!_didPeek)
        {
            _peeked = _reader.Read();
            _didPeek = true;
        }

        return _peeked;
    }

    /// <summary>Does not support peek.</summary>
    private sealed class NonPeekableTextBufferTokenizer
    {
        private readonly ITokenizerSource _textModel;
        private readonly LanguageAgnosticBracketTokens _bracketTokens;
        private readonly int _textBufferLineCount;
        private readonly int _textBufferLastLineLength;

        private int _lineIdx;
        private string? _line;
        private int _lineCharOffset;
        private IViewLineTokens? _lineTokens;
        private int _lineTokenOffset;

        // Must be a zero line token. The end of the document cannot be peeked.
        private Token? _peekedToken;

        public NonPeekableTextBufferTokenizer(ITokenizerSource textModel, LanguageAgnosticBracketTokens bracketTokens)
        {
            _textModel = textModel;
            _bracketTokens = bracketTokens;
            _textBufferLineCount = textModel.GetLineCount();
            _textBufferLastLineLength = textModel.GetLineLength(_textBufferLineCount);
        }

        public void SetPosition(int lineIdx, int column)
        {
            // We must not jump into a token!
            if (lineIdx == _lineIdx)
            {
                _lineCharOffset = column;
                if (_line is not null)
                {
                    _lineTokenOffset = _lineCharOffset == 0 ? 0 : _lineTokens!.FindTokenIndexAtOffset(_lineCharOffset);
                }
            }
            else
            {
                _lineIdx = lineIdx;
                _lineCharOffset = column;
                _line = null;
            }

            _peekedToken = null;
        }

        public Token? Read()
        {
            if (_peekedToken is not null)
            {
                var token = _peekedToken;
                _peekedToken = null;
                _lineCharOffset += token.Length.GetColumnCountIfZeroLineCount();
                return token;
            }

            if (_lineIdx > _textBufferLineCount - 1 || (_lineIdx == _textBufferLineCount - 1 && _lineCharOffset >= _textBufferLastLineLength))
            {
                // We are after the end
                return null;
            }

            if (_line is null)
            {
                _lineTokens = _textModel.GetLineTokens(_lineIdx + 1);
                _line = _lineTokens.GetLineContent();
                _lineTokenOffset = _lineCharOffset == 0 ? 0 : _lineTokens.FindTokenIndexAtOffset(_lineCharOffset);
            }

            var startLineIdx = _lineIdx;
            var startLineCharOffset = _lineCharOffset;

            // limits the length of text tokens.
            // If text tokens get too long, incremental updates will be slow
            var lengthHeuristic = 0;
            while (true)
            {
                var lineTokens = _lineTokens!;
                var tokenCount = lineTokens.GetCount();

                Token? peekedBracketToken = null;

                if (_lineTokenOffset < tokenCount)
                {
                    var tokenMetadata = lineTokens.GetMetadata(_lineTokenOffset);
                    while (_lineTokenOffset + 1 < tokenCount && tokenMetadata == lineTokens.GetMetadata(_lineTokenOffset + 1))
                    {
                        // Skip tokens that are identical.
                        // Sometimes, (bracket) identifiers are split up into multiple tokens.
                        _lineTokenOffset++;
                    }

                    var isOther = TokenMetadata.GetTokenType(tokenMetadata) == StandardTokenType.Other;
                    var containsBracketType = TokenMetadata.ContainsBalancedBrackets(tokenMetadata);

                    var endOffset = lineTokens.GetEndOffset(_lineTokenOffset);
                    // Is there a bracket token next? Only consume text.
                    if (containsBracketType && isOther && _lineCharOffset < endOffset)
                    {
                        var languageId = lineTokens.GetLanguageId(_lineTokenOffset);
                        var text = _line[_lineCharOffset..endOffset];

                        var brackets = _bracketTokens.GetSingleLanguageBracketTokens(languageId);
                        var regexp = brackets.RegExpGlobal;
                        if (regexp is not null)
                        {
                            Match match = regexp.Match(text);
                            if (match.Success)
                            {
                                peekedBracketToken = brackets.GetToken(match.Value);
                                if (peekedBracketToken is not null)
                                {
                                    // Consume leading text of the token
                                    _lineCharOffset += match.Index;
                                }
                            }
                        }
                    }

                    lengthHeuristic += endOffset - _lineCharOffset;

                    if (peekedBracketToken is not null)
                    {
                        // Don't skip the entire token, as a single token could contain multiple brackets.
                        if (startLineIdx != _lineIdx || startLineCharOffset != _lineCharOffset)
                        {
                            // There is text before the bracket
                            _peekedToken = peekedBracketToken;
                            break;
                        }

                        // Consume the peeked token
                        _lineCharOffset += peekedBracketToken.Length.GetColumnCountIfZeroLineCount();
                        return peekedBracketToken;
                    }

                    // Skip the entire token, as the token contains no brackets at all.
                    _lineTokenOffset++;
                    _lineCharOffset = endOffset;
                }
                else
                {
                    if (_lineIdx == _textBufferLineCount - 1)
                    {
                        break;
                    }

                    _lineIdx++;
                    _lineTokens = _textModel.GetLineTokens(_lineIdx + 1);
                    _lineTokenOffset = 0;
                    _line = _lineTokens.GetLineContent();
                    _lineCharOffset = 0;

                    lengthHeuristic += 33; // max 1000/33 = 30 lines
                    // This limits the amount of work to recompute min-indentation

                    if (lengthHeuristic > 1000)
                    {
                        // only break (automatically) at the end of line.
                        break;
                    }
                }

                if (lengthHeuristic > 1500)
                {
                    // Eventually break regardless of the line length so that
                    // very long lines do not cause bad performance.
                    // This effective limits max indentation to 500, as
                    // indentation is not computed across multiple text nodes.
                    break;
                }
            }

            // If a token contains some proper indentation, it also contains \n{INDENTATION+}(?!{INDENTATION}),
            // unless the line is too long.
            // Thus, the min indentation of the document is the minimum min indentation of every text node.
            var length = Length.Diff(startLineIdx, startLineCharOffset, _lineIdx, _lineCharOffset);
            return new Token(length, TokenKind.Text, -1, SmallImmutableSet<int>.Empty, new TextAstNode(length));
        }
    }
}
