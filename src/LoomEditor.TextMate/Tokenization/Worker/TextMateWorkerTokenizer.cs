// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/backgroundTokenization/worker/textMateWorkerTokenizer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// The worker's copy of one document, tokenized from its first invalid line (Monaco: <c>TextMateWorkerTokenizer</c>).
/// It has no thread and no timer: <see cref="TokenizationWorker"/> decides when to call
/// <see cref="TokenizeSlice"/> (it replaces <c>RunOnceScheduler</c> and <c>setTimeout0</c>). Not ported:
/// <c>_getFontTokensUpdate</c> and the tokenization time report. Grammars are created synchronously by
/// <c>getOrCreateGrammar</c>, which returns <see langword="null"/> when the language cannot be tokenized.
/// </summary>
/// <remarks>Threading: not thread-safe; used by the worker thread only, which also owns the grammars.</remarks>
internal sealed class TextMateWorkerTokenizer : MirrorTextModel
{
    private readonly ITextMateWorkerHost _host;
    private readonly int _controllerId;
    private readonly Func<string, int, CreateGrammarResult?> _getOrCreateGrammar;
    private TokenizerWithStateStore? _tokenizerWithStateStore;
    private TokenizationSupportWithLineLimit? _lineLimit;
    private string _languageId;
    private int _encodedLanguageId;
    private int _maxTokenizationLineLength;

    /// <summary>Creates the tokenizer and starts with the language <paramref name="languageId"/> (no line is tokenized yet).</summary>
    public TextMateWorkerTokenizer(
        List<string> lines,
        string eol,
        int versionId,
        ITextMateWorkerHost host,
        int controllerId,
        Func<string, int, CreateGrammarResult?> getOrCreateGrammar,
        string languageId,
        int encodedLanguageId,
        int maxTokenizationLineLength)
        : base(lines, eol, versionId)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(getOrCreateGrammar);
        ArgumentNullException.ThrowIfNull(languageId);
        _host = host;
        _controllerId = controllerId;
        _getOrCreateGrammar = getOrCreateGrammar;
        _languageId = languageId;
        _encodedLanguageId = encodedLanguageId;
        _maxTokenizationLineLength = maxTokenizationLineLength;
        ResetTokenization();
    }

    /// <summary>Whether some line still has to be tokenized.</summary>
    public bool HasInvalidLines => _tokenizerWithStateStore?.GetFirstInvalidLine() is not null;

    /// <inheritdoc />
    public override void Dispose()
    {
        _tokenizerWithStateStore = null;
        _lineLimit = null;
        base.Dispose();
    }

    /// <summary><c>onLanguageId</c>.</summary>
    public void OnLanguageId(string languageId, int encodedLanguageId)
    {
        _languageId = languageId;
        _encodedLanguageId = encodedLanguageId;
        ResetTokenization();
    }

    /// <summary>The model changed: <c>onEvents</c> (the model, then the end states).</summary>
    public void AcceptEvents(MirrorModelChangedEvent e)
    {
        OnEvents(e);
        _tokenizerWithStateStore?.Store.AcceptChanges(e.Changes);
    }

    /// <summary><c>acceptMaxTokenizationLineLength</c>.</summary>
    public void AcceptMaxTokenizationLineLength(int maxTokenizationLineLength)
    {
        _maxTokenizationLineLength = maxTokenizationLineLength;
        if (_lineLimit is not null)
        {
            _lineLimit.MaxTokenizationLineLength = maxTokenizationLineLength;
        }
    }

    /// <summary><c>retokenize</c>: the end states of the lines <paramref name="startLineNumber"/> to <paramref name="endLineNumberExclusive"/> are not trusted anymore.</summary>
    public void Retokenize(int startLineNumber, int endLineNumberExclusive) =>
        _tokenizerWithStateStore?.Store.InvalidateEndStateRange(new OffsetRange(startLineNumber, endLineNumberExclusive));

    /// <summary>
    /// <c>_tokenize</c>, one batch: tokenizes invalid lines until <paramref name="maxLines"/> lines are done or
    /// <paramref name="maxMilliseconds"/> have passed, then sends the tokens and the end states to the host. Monaco
    /// uses 200 lines (it actually does 201) and 20 ms.
    /// </summary>
    /// <returns>Whether invalid lines remain, so another slice is needed.</returns>
    public bool TokenizeSlice(int maxLines, int maxMilliseconds)
    {
        if (_tokenizerWithStateStore is not { } tokenizer)
        {
            return false;
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        int tokenizedLines = 0;
        var tokenBuilder = new ContiguousMultilineTokensBuilder();
        var stateDeltaBuilder = new StateDeltaBuilder();

        while (tokenizedLines < maxLines && tokenizer.GetFirstInvalidLine() is { } lineToTokenize)
        {
            tokenizedLines++;

            string text = _lines[lineToTokenize.LineNumber - 1];
            EncodedTokenizationResult r = tokenizer.TokenizationSupport.TokenizeEncoded(text, true, lineToTokenize.StartState);
            var endState = (StateStack)r.EndState;
            stateDeltaBuilder.SetState(lineToTokenize.LineNumber, tokenizer.Store.SetEndState(lineToTokenize.LineNumber, r.EndState) ? endState : null);

            Core.Tokens.LineTokens.ConvertToEndOffset(r.Tokens, text.Length);
            tokenBuilder.Add(lineToTokenize.LineNumber, r.Tokens);

            if (Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds > maxMilliseconds)
            {
                // yield to check for changes
                break;
            }
        }

        if (tokenizedLines == 0)
        {
            return false;
        }

        _host.SetTokensAndStates(_controllerId, _versionId, tokenBuilder.Finalize(), stateDeltaBuilder.GetStateDeltas());
        return tokenizer.GetFirstInvalidLine() is not null;
    }

    // _resetTokenization (synchronous here: the grammar comes from the worker's own factory).
    private void ResetTokenization()
    {
        _tokenizerWithStateStore = null;
        _lineLimit = null;

        CreateGrammarResult? r = _getOrCreateGrammar(_languageId, _encodedLanguageId);
        if (r?.Grammar is null)
        {
            return;
        }

        _lineLimit = new TokenizationSupportWithLineLimit(
            _encodedLanguageId,
            new TextMateTokenizationSupport(r.Grammar, r.InitialState, false, null, () => false),
            _maxTokenizationLineLength);
        _tokenizerWithStateStore = new TokenizerWithStateStore(_lines.Count, _lineLimit);
    }
}
