// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/tokenizationSupport/textMateTokenizationSupport.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// A vscode-textmate grammar as a Monaco <see cref="ITokenizationSupport"/> (Monaco: <c>TextMateTokenizationSupport</c>).
/// Not ported: the tokenization time telemetry (<c>_reportTokenizationTime</c>, <c>_reportSlowTokenization</c>) and the
/// string <c>tokenize</c>.
/// </summary>
/// <remarks>Threading: one thread at a time (see <see cref="IGrammar"/>). <see cref="EncounteredLanguage"/> is raised on the tokenizing thread.</remarks>
internal sealed class TextMateTokenizationSupport : ITokenizationSupport
{
    /// <summary>The time limit of the tokenization of a line, in milliseconds (upstream: <c>500</c>).</summary>
    private const int TimeLimitMilliseconds = 500;

    private readonly IGrammar _grammar;
    private readonly StateStack _initialState;
    private readonly bool _containsEmbeddedLanguages;
    private readonly Func<TextModel, IBackgroundTokenizationStore, IBackgroundTokenizer?>? _createBackgroundTokenizer;
    private readonly Func<bool> _backgroundTokenizerShouldOnlyVerifyTokens;
    private readonly HashSet<int> _seenLanguages = [];

    /// <summary>Creates the support of <paramref name="grammar"/>.</summary>
    public TextMateTokenizationSupport(
        IGrammar grammar,
        StateStack initialState,
        bool containsEmbeddedLanguages,
        Func<TextModel, IBackgroundTokenizationStore, IBackgroundTokenizer?>? createBackgroundTokenizer,
        Func<bool> backgroundTokenizerShouldOnlyVerifyTokens)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(backgroundTokenizerShouldOnlyVerifyTokens);
        _grammar = grammar;
        _initialState = initialState;
        _containsEmbeddedLanguages = containsEmbeddedLanguages;
        _createBackgroundTokenizer = createBackgroundTokenizer;
        _backgroundTokenizerShouldOnlyVerifyTokens = backgroundTokenizerShouldOnlyVerifyTokens;
    }

    /// <summary><c>onDidEncounterLanguage</c>: raised once per language, with its encoded id.</summary>
    public event EventHandler<int>? EncounteredLanguage;

    /// <inheritdoc />
    public bool? BackgroundTokenizerShouldOnlyVerifyTokens => _backgroundTokenizerShouldOnlyVerifyTokens();

    /// <inheritdoc />
    public IState GetInitialState() => _initialState;

    /// <inheritdoc />
    public IBackgroundTokenizer? CreateBackgroundTokenizer(TextModel textModel, IBackgroundTokenizationStore store) =>
        _createBackgroundTokenizer?.Invoke(textModel, store);

    /// <inheritdoc />
    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
    {
        var start = (StateStack)state;
        TokenizeLineResult2 textMateResult = _grammar.TokenizeLine2(line, start, TimeLimitMilliseconds);

        if (textMateResult.StoppedEarly)
        {
            Debug.WriteLine($"Time limit reached when tokenizing line: {line[..Math.Min(100, line.Length)]}");
            // return the state at the beginning of the line
            return new EncodedTokenizationResult(textMateResult.Tokens, start);
        }

        if (_containsEmbeddedLanguages)
        {
            uint[] tokens = textMateResult.Tokens;

            // Must check if any of the embedded languages was hit
            for (int i = 0, len = tokens.Length >>> 1; i < len; i++)
            {
                int languageId = TokenMetadata.GetLanguageId(tokens[(i << 1) + 1]);
                if (_seenLanguages.Add(languageId))
                {
                    EncounteredLanguage?.Invoke(this, languageId);
                }
            }
        }

        // try to save an object if possible
        StateStack endState = start.Equals(textMateResult.RuleStack) ? start : textMateResult.RuleStack;
        return new EncodedTokenizationResult(textMateResult.Tokens, endState);
    }
}
