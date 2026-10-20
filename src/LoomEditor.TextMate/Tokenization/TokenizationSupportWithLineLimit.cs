// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/tokenizationSupport/tokenizationSupportWithLineLimit.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// Gives the null tokens to lines that are too long (Monaco: <c>TokenizationSupportWithLineLimit</c>). The limit is a
/// constructor argument, not an observable of <c>editor.maxTokenizationLineLength</c> (default 20 000).
/// </summary>
/// <remarks>Threading: same as the wrapped support.</remarks>
internal sealed class TokenizationSupportWithLineLimit : ITokenizationSupport
{
    /// <summary><c>editor.maxTokenizationLineLength</c> default.</summary>
    public const int DefaultMaxTokenizationLineLength = 20_000;

    private readonly int _encodedLanguageId;
    private readonly ITokenizationSupport _actual;
    private int _maxTokenizationLineLength;

    /// <summary>Wraps <paramref name="actual"/>; lines of <paramref name="maxTokenizationLineLength"/> characters or more are not tokenized.</summary>
    public TokenizationSupportWithLineLimit(int encodedLanguageId, ITokenizationSupport actual, int maxTokenizationLineLength = DefaultMaxTokenizationLineLength)
    {
        ArgumentNullException.ThrowIfNull(actual);
        _encodedLanguageId = encodedLanguageId;
        _actual = actual;
        _maxTokenizationLineLength = maxTokenizationLineLength;
    }

    /// <summary>
    /// The limit; set it to follow <c>editor.maxTokenizationLineLength</c> (Monaco's observable). Lines tokenized later
    /// use the new value; the tokenizing thread must be the one that sets it.
    /// </summary>
    public int MaxTokenizationLineLength
    {
        get => _maxTokenizationLineLength;
        set => _maxTokenizationLineLength = value;
    }

    /// <inheritdoc />
    public bool? BackgroundTokenizerShouldOnlyVerifyTokens => _actual.BackgroundTokenizerShouldOnlyVerifyTokens;

    /// <inheritdoc />
    public IState GetInitialState() => _actual.GetInitialState();

    /// <inheritdoc />
    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
        // Do not attempt to tokenize if a line is too long
        line.Length >= _maxTokenizationLineLength
            ? NullTokenize.NullTokenizeEncoded(_encodedLanguageId, state)
            : _actual.TokenizeEncoded(line, hasEol, state);

    /// <inheritdoc />
    public IBackgroundTokenizer? CreateBackgroundTokenizer(TextModel textModel, IBackgroundTokenizationStore store) =>
        _actual.CreateBackgroundTokenizer(textModel, store);
}
