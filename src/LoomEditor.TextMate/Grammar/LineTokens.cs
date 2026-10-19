// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>LineTokens</c>: collects the tokens of one line, either as scope names (<c>tokenizeLine</c>) or as binary
/// (start index, metadata) pairs (<c>tokenizeLine2</c>).
/// </summary>
/// <remarks>
/// Deliberate differences: <c>FontInfo</c> / <c>LineFonts</c> are not ported (ADR 0008, decision 3); debug logging is
/// never done (<c>DebugFlags.InDebugMode</c> is always false), so the line text is only used to detect RTL text, with
/// <see cref="Strings.ContainsRtl(string)"/> (<c>containsRTL</c>).
/// Threading: single-threaded; one instance per tokenized line.
/// </remarks>
internal sealed class LineTokens
{
    private readonly bool _emitBinaryTokens;
    private readonly List<Token> _tokens = [];
    private readonly List<uint> _binaryTokens = [];
    private readonly IReadOnlyList<TokenTypeMatcher> _tokenTypeOverrides;
    private readonly BalancedBracketSelectors? _balancedBracketSelectors;
    private readonly bool _mergeConsecutiveTokensWithEqualMetadata;
    private int _lastTokenEndIndex;

    /// <summary>Starts collecting the tokens of <paramref name="lineText"/>.</summary>
    /// <param name="emitBinaryTokens">Whether to collect binary tokens (<c>tokenizeLine2</c>) or scope names.</param>
    /// <param name="lineText">The line, used only to decide whether equal consecutive tokens may be merged.</param>
    /// <param name="tokenTypeOverrides"><c>tokenTypes</c>: selectors that force a standard token type.</param>
    /// <param name="balancedBracketSelectors">Decides which tokens get the balanced brackets bit, if any.</param>
    public LineTokens(
        bool emitBinaryTokens,
        string lineText,
        IReadOnlyList<TokenTypeMatcher> tokenTypeOverrides,
        BalancedBracketSelectors? balancedBracketSelectors)
    {
        ArgumentNullException.ThrowIfNull(lineText);
        ArgumentNullException.ThrowIfNull(tokenTypeOverrides);
        _emitBinaryTokens = emitBinaryTokens;
        _tokenTypeOverrides = tokenTypeOverrides;
        _balancedBracketSelectors = balancedBracketSelectors;
        // Don't merge tokens if the line contains RTL characters
        _mergeConsecutiveTokensWithEqualMetadata = !Strings.ContainsRtl(lineText);
    }

    /// <summary><c>produce</c>: a token up to <paramref name="endIndex"/> with the content scopes of <paramref name="stack"/>.</summary>
    public void Produce(StateStackImpl stack, int endIndex)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ProduceFromScopes(stack.ContentNameScopesList, endIndex);
    }

    /// <summary><c>produceFromScopes</c>.</summary>
    public void ProduceFromScopes(AttributedScopeStack? scopesList, int endIndex)
    {
        if (_lastTokenEndIndex >= endIndex)
        {
            return;
        }

        if (_emitBinaryTokens)
        {
            ProduceBinary(scopesList, endIndex);
            return;
        }

        List<string> scopes = scopesList?.GetScopeNames() ?? [];
        _tokens.Add(new Token(_lastTokenEndIndex, endIndex, scopes));
        _lastTokenEndIndex = endIndex;
    }

    /// <summary><c>getResult</c>.</summary>
    public List<Token> GetResult(StateStackImpl stack, int lineLength)
    {
        if (_tokens.Count > 0 && _tokens[^1].StartIndex == lineLength - 1)
        {
            // pop produced token for newline
            _tokens.RemoveAt(_tokens.Count - 1);
        }

        if (_tokens.Count == 0)
        {
            _lastTokenEndIndex = -1;
            Produce(stack, lineLength);
            _tokens[^1] = _tokens[^1] with { StartIndex = 0 };
        }

        return _tokens;
    }

    /// <summary><c>getBinaryResult</c>.</summary>
    public uint[] GetBinaryResult(StateStackImpl stack, int lineLength)
    {
        if (_binaryTokens.Count > 0 && _binaryTokens[^2] == (uint)(lineLength - 1))
        {
            // pop produced token for newline
            _binaryTokens.RemoveRange(_binaryTokens.Count - 2, 2);
        }

        if (_binaryTokens.Count == 0)
        {
            _lastTokenEndIndex = -1;
            Produce(stack, lineLength);
            _binaryTokens[^2] = 0;
        }

        return [.. _binaryTokens];
    }

    private void ProduceBinary(AttributedScopeStack? scopesList, int endIndex)
    {
        uint metadata = scopesList?.TokenAttributes ?? 0;
        bool containsBalancedBrackets = false;
        if (_balancedBracketSelectors is { MatchesAlways: true })
        {
            containsBalancedBrackets = true;
        }

        if (_tokenTypeOverrides.Count > 0
            || (_balancedBracketSelectors is { MatchesAlways: false, MatchesNever: false }))
        {
            // Only generate scope array when required to improve performance
            string[] scopes = [.. scopesList?.GetScopeNames() ?? []];
            foreach (TokenTypeMatcher tokenType in _tokenTypeOverrides)
            {
                if (tokenType.Matcher(scopes))
                {
                    metadata = EncodedTokenAttributes.Set(
                        metadata,
                        0,
                        EncodedTokenAttributes.ToOptionalTokenType(tokenType.Type),
                        null,
                        FontStyle.NotSet,
                        0,
                        0);
                }
            }

            if (_balancedBracketSelectors is not null)
            {
                containsBalancedBrackets = _balancedBracketSelectors.Match(scopes);
            }
        }

        if (containsBalancedBrackets)
        {
            metadata = EncodedTokenAttributes.Set(
                metadata,
                0,
                OptionalStandardTokenType.NotSet,
                containsBalancedBrackets,
                FontStyle.NotSet,
                0,
                0);
        }

        if (_mergeConsecutiveTokensWithEqualMetadata && _binaryTokens.Count > 0 && _binaryTokens[^1] == metadata)
        {
            // no need to push a token with the same metadata
            _lastTokenEndIndex = endIndex;
            return;
        }

        _binaryTokens.Add((uint)_lastTokenEndIndex);
        _binaryTokens.Add(metadata);

        _lastTokenEndIndex = endIndex;
    }
}
