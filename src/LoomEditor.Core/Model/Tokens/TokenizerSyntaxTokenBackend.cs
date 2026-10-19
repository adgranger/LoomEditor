// Ported from microsoft/vscode: src/vs/editor/common/model/tokens/tokenizerSyntaxTokenBackend.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// The syntax tokens of a model, computed by the <see cref="ITokenizationSupport"/> of its language (Monaco:
/// <c>TokenizerSyntaxTokenBackend</c> merged with <c>AbstractSyntaxTokenBackend</c>: there is no other backend). Not
/// ported: attached views and the viewport refresh (P4-30), the size limit (P4-30), the verify-only debug path,
/// <c>onDidChangeBackgroundTokenizationState</c> (bracket pairs, phase 5), font tokens.
/// </summary>
/// <remarks>
/// Threading: not thread-safe. Differences from Monaco: without a scheduler, the default background tokenizer is not
/// created (tokens are then only computed by <see cref="ForceTokenization"/>); there is no disposal yet, so the
/// registry subscription lives as long as the registry.
/// </remarks>
internal sealed class TokenizerSyntaxTokenBackend
{
    private readonly TextModel _model;
    private readonly ILanguageIdCodec _languageIdCodec;
    private readonly TokenizationRegistry _registry;
    private readonly ITokenizationModelInfo _modelInfo;
    private readonly ITokenizationScheduler? _scheduler;
    private readonly Action<ModelTokensChangedEventArgs> _raiseTokensChanged;
    private readonly ContiguousTokensStore _tokens;
    private TokenizerWithStateStoreAndTextModel? _tokenizer;

    // Zero or one tokenizer (Monaco: a MutableDisposable). A list, because the analyzer rejects a disposable field in a
    // type that is not disposable, and disposal of the model is not ported yet.
    private readonly List<IBackgroundTokenizer> _backgroundTokenizers = [];

    /// <summary>Creates the backend and starts tokenizing with the support registered for the language.</summary>
    public TokenizerSyntaxTokenBackend(
        ILanguageIdCodec languageIdCodec,
        TextModel model,
        TokenizationRegistry registry,
        ITokenizationModelInfo modelInfo,
        ITokenizationScheduler? scheduler,
        Action<ModelTokensChangedEventArgs> raiseTokensChanged)
    {
        _languageIdCodec = languageIdCodec;
        _model = model;
        _registry = registry;
        _modelInfo = modelInfo;
        _scheduler = scheduler;
        _raiseTokensChanged = raiseTokensChanged;
        _tokens = new ContiguousTokensStore(languageIdCodec);

        registry.Changed += (_, e) =>
        {
            if (e.ChangedLanguages.Contains(_modelInfo.LanguageId))
            {
                ResetTokenization();
            }
        };

        ResetTokenization();
    }

    /// <summary><c>backgroundTokenizationState</c>.</summary>
    public BackgroundTokenizationState BackgroundTokenizationState { get; private set; } = BackgroundTokenizationState.InProgress;

    /// <summary><c>hasTokens</c>.</summary>
    public bool HasTokens => _tokens.HasTokens;

    /// <summary><c>todo_resetTokenization</c>: forgets all tokens and picks the support of the language again.</summary>
    public void ResetTokenization(bool fireTokenChangeEvent = true)
    {
        _tokens.Flush();
        if (fireTokenChangeEvent)
        {
            _raiseTokensChanged(new ModelTokensChangedEventArgs([new ChangedLineRange(1, _model.LineCount)]));
        }

        ITokenizationSupport? tokenizationSupport = InitializeTokenization();
        _tokenizer = tokenizationSupport is null
            ? null
            : new TokenizerWithStateStoreAndTextModel(_model.LineCount, tokenizationSupport, _model, _modelInfo, _languageIdCodec);

        foreach (IBackgroundTokenizer old in _backgroundTokenizers)
        {
            old.Dispose();
        }

        _backgroundTokenizers.Clear();
        if (_tokenizer is null)
        {
            return;
        }

        var store = new BackgroundStore(this);
        IBackgroundTokenizer? created = tokenizationSupport!.BackgroundTokenizerShouldOnlyVerifyTokens != true
            ? tokenizationSupport.CreateBackgroundTokenizer(_model, store)
            : null;
        if (created is null && _scheduler is not null)
        {
            var fallback = new DefaultBackgroundTokenizer(_tokenizer, store, _scheduler);
            created = fallback;
            fallback.HandleChanges();
        }

        if (created is not null)
        {
            _backgroundTokenizers.Add(created);
        }
    }

    private DefaultBackgroundTokenizer? DefaultBackgroundTokenizer => _backgroundTokenizers.OfType<DefaultBackgroundTokenizer>().FirstOrDefault();

    private ITokenizationSupport? InitializeTokenization()
    {
        ITokenizationSupport? tokenizationSupport = _registry.Get(_modelInfo.LanguageId);
        if (tokenizationSupport is null)
        {
            return null;
        }

        try
        {
            tokenizationSupport.GetInitialState();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Tokenization failed: {e}");
            return null;
        }

        return tokenizationSupport;
    }

    /// <summary><c>handleDidChangeAttached</c>.</summary>
    public void HandleDidChangeAttached() => DefaultBackgroundTokenizer?.HandleChanges();

    /// <summary><c>handleDidChangeContent</c>.</summary>
    public void HandleDidChangeContent(ModelContentChangedEventArgs e)
    {
        if (e.IsFlush)
        {
            // Don't fire the event, as the view might not have got the text change event yet
            ResetTokenization(false);
        }
        else if (!e.IsEolChange)
        {
            // We don't have to do anything on an EOL change
            foreach (ModelContentChange c in e.Changes)
            {
                EolCount eol = EolCounter.Count(c.Text);
                _tokens.AcceptEdit(c.Range, eol.Count, eol.FirstLineLength);
            }

            _tokenizer?.Store.AcceptChanges(e.Changes);
            DefaultBackgroundTokenizer?.HandleChanges();
        }
    }

    private List<ChangedLineRange> SetTokens(IReadOnlyList<ContiguousMultilineTokens> tokens)
    {
        List<ChangedLineRange> changes = _tokens.SetMultilineTokens(tokens, _model, _modelInfo.LanguageId);

        if (changes.Count > 0)
        {
            _raiseTokensChanged(new ModelTokensChangedEventArgs(changes));
        }

        return changes;
    }

    /// <summary><c>forceTokenization</c>: tokenizes synchronously up to <paramref name="lineNumber"/>.</summary>
    public void ForceTokenization(int lineNumber)
    {
        var builder = new ContiguousMultilineTokensBuilder();
        _tokenizer?.UpdateTokensUntilLine(builder, lineNumber);
        SetTokens(builder.Finalize());
        DefaultBackgroundTokenizer?.CheckFinished();
    }

    /// <summary><c>hasAccurateTokensForLine</c>.</summary>
    public bool HasAccurateTokensForLine(int lineNumber) => _tokenizer?.HasAccurateTokensForLine(lineNumber) ?? true;

    /// <summary><c>isCheapToTokenize</c>.</summary>
    public bool IsCheapToTokenize(int lineNumber) => _tokenizer?.IsCheapToTokenize(lineNumber) ?? true;

    /// <summary><c>tokenizeIfCheap</c>.</summary>
    public void TokenizeIfCheap(int lineNumber)
    {
        if (IsCheapToTokenize(lineNumber))
        {
            ForceTokenization(lineNumber);
        }
    }

    /// <summary><c>getLineTokens</c>.</summary>
    public LineTokens GetLineTokens(int lineNumber) =>
        _tokens.GetTokens(_modelInfo.LanguageId, lineNumber - 1, _model.GetLineContent(lineNumber));

    /// <summary><c>getTokenTypeIfInsertingCharacter</c>.</summary>
    public StandardTokenType GetTokenTypeIfInsertingCharacter(int lineNumber, int column, string character)
    {
        if (_tokenizer is null)
        {
            return StandardTokenType.Other;
        }

        Position position = _model.ValidatePosition(new Position(lineNumber, column));
        ForceTokenization(position.LineNumber);
        return _tokenizer.GetTokenTypeIfInsertingCharacter(position, character);
    }

    /// <summary><c>tokenizeLinesAt</c>: <c>null</c> when there is no tokenizer.</summary>
    public List<LineTokens>? TokenizeLinesAt(int lineNumber, IReadOnlyList<string> lines)
    {
        if (_tokenizer is null)
        {
            return null;
        }

        ForceTokenization(lineNumber);
        return _tokenizer.TokenizeLinesAt(lineNumber, lines);
    }

    // The IBackgroundTokenizationStore Monaco builds inline in todo_resetTokenization.
    private sealed class BackgroundStore(TokenizerSyntaxTokenBackend owner) : IBackgroundTokenizationStore
    {
        public void SetTokens(IReadOnlyList<ContiguousMultilineTokens> tokens) => owner.SetTokens(tokens);

        public void BackgroundTokenizationFinished()
        {
            // We already did a full tokenization and don't go back to progressing.
            owner.BackgroundTokenizationState = BackgroundTokenizationState.Completed;
        }

        public void SetEndState(int lineNumber, IState state)
        {
            if (owner._tokenizer is null)
            {
                return;
            }

            // Don't accept states for definitely valid states, the renderer is ahead of the worker!
            int? firstInvalidEndStateLineNumber = owner._tokenizer.Store.GetFirstInvalidEndStateLineNumber();
            if (firstInvalidEndStateLineNumber is not null && lineNumber >= firstInvalidEndStateLineNumber)
            {
                owner._tokenizer.Store.SetEndState(lineNumber, state);
            }
        }
    }
}
