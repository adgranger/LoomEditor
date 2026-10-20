// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// Reads the raw text of a model without syntax tokens as bracket and text tokens, with one regex over the whole text
/// (Monaco: <c>FastTokenizer</c>). All the tokens are computed in the constructor.
/// </summary>
/// <remarks>
/// <para>
/// Behavior difference: the regex is a .NET regex (see <see cref="BracketTokens"/>); <see cref="Skip"/> throws
/// <see cref="NotSupportedException"/> (Monaco: <c>NotSupportedError</c>).
/// </para>
/// <para>Threading: single-threaded (UI thread), like the tree that owns it.</para>
/// </remarks>
internal sealed class FastTokenizer : ITokenizer
{
    private readonly string _text;
    private readonly Length _offset = Length.Zero;
    private readonly Token[] _tokens;
    private int _idx;

    /// <summary>Tokenizes <paramref name="text"/> with the brackets of one language.</summary>
    public FastTokenizer(string text, BracketTokens brackets)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(brackets);

        _text = text;

        var regExpStr = brackets.GetRegExpStr();
        var regexp = regExpStr is not null
            ? new Regex(regExpStr + "|\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            : null;

        var tokens = new List<Token>();

        var curLineCount = 0;
        var lastLineBreakOffset = 0;

        var lastTokenEndOffset = 0;
        var lastTokenEndLine = 0;

        var smallTextTokens0Line = new Token[60];
        for (var i = 0; i < smallTextTokens0Line.Length; i++)
        {
            smallTextTokens0Line[i] = CreateTextToken(Length.Of(0, i));
        }

        var smallTextTokens1Line = new Token[60];
        for (var i = 0; i < smallTextTokens1Line.Length; i++)
        {
            smallTextTokens1Line[i] = CreateTextToken(Length.Of(1, i));
        }

        if (regexp is not null)
        {
            // If a token contains indentation, it also contains \n{INDENTATION+}(?!{INDENTATION})
            // Matches are never empty (brackets are non-empty and "\n" is one character).
            for (var match = regexp.Match(text); match.Success; match = regexp.Match(text, match.Index + match.Length))
            {
                var curOffset = match.Index;
                var value = match.Value;
                if (value == "\n")
                {
                    curLineCount++;
                    lastLineBreakOffset = curOffset + 1;
                }
                else
                {
                    if (lastTokenEndOffset != curOffset)
                    {
                        Token token;
                        if (lastTokenEndLine == curLineCount)
                        {
                            var colCount = curOffset - lastTokenEndOffset;
                            token = colCount < smallTextTokens0Line.Length
                                ? smallTextTokens0Line[colCount]
                                : CreateTextToken(Length.Of(0, colCount));
                        }
                        else
                        {
                            var lineCount = curLineCount - lastTokenEndLine;
                            var colCount = curOffset - lastLineBreakOffset;
                            token = lineCount == 1 && colCount < smallTextTokens1Line.Length
                                ? smallTextTokens1Line[colCount]
                                : CreateTextToken(Length.Of(lineCount, colCount));
                        }

                        tokens.Add(token);
                    }

                    // value is matched by regexp, so the token must exist
                    tokens.Add(brackets.GetToken(value)!);

                    lastTokenEndOffset = curOffset + value.Length;
                    lastTokenEndLine = curLineCount;
                }
            }
        }

        var offset = text.Length;

        if (lastTokenEndOffset != offset)
        {
            var length = lastTokenEndLine == curLineCount
                ? Length.Of(0, offset - lastTokenEndOffset)
                : Length.Of(curLineCount - lastTokenEndLine, offset - lastLineBreakOffset);
            tokens.Add(CreateTextToken(length));
        }

        Length = Length.Of(curLineCount, offset - lastLineBreakOffset);
        _tokens = [.. tokens];
    }

    /// <inheritdoc />
    /// <remarks>Always zero: <see cref="Skip"/> is not supported, so the offset never moves.</remarks>
    public Length Offset => _offset;

    /// <inheritdoc />
    public Length Length { get; }

    /// <inheritdoc />
    public Token? Read()
    {
        return _idx < _tokens.Length ? _tokens[_idx++] : null;
    }

    /// <inheritdoc />
    public Token? Peek()
    {
        return _idx < _tokens.Length ? _tokens[_idx] : null;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always.</exception>
    public void Skip(Length length)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public string GetText()
    {
        return _text;
    }

    private static Token CreateTextToken(Length length)
    {
        return new Token(length, TokenKind.Text, -1, SmallImmutableSet<int>.Empty, new TextAstNode(length));
    }
}
