// Ported from microsoft/vscode: src/vs/editor/common/model/tokens/tokenizationTextModelPart.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// The tokenization of a <see cref="TextModel"/>: its language id and its syntax tokens (Monaco:
/// <c>TokenizationTextModelPart</c> / <c>ITokenizationTextModelPart</c>). Not ported: semantic tokens, tree-sitter,
/// font tokens.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class TokenizationTextModelPart : ITokenizationModelInfo
{
    private readonly TextModel _model;
    private readonly TokenizerSyntaxTokenBackend _tokens;

    /// <summary>Creates the part of <paramref name="model"/>, whose buffer must already exist.</summary>
    public TokenizationTextModelPart(
        TextModel model,
        string languageId,
        TokenizationRegistry registry,
        ILanguageIdCodec languageIdCodec,
        ITokenizationScheduler? scheduler,
        AttachedViews attachedViews)
    {
        _model = model;
        LanguageId = languageId;
        _tokens = new TokenizerSyntaxTokenBackend(
            languageIdCodec, model, registry, this, scheduler, attachedViews, model.RaiseTokensChanged, model.BracketPairs.HandleDidChangeBackgroundTokenizationState);
    }

    /// <summary><c>getLanguageId</c>.</summary>
    public string LanguageId { get; private set; }

    /// <summary>Whether the model is attached to an editor (background tokenization only runs then).</summary>
    public bool IsAttachedToEditor => _model.IsAttachedToEditor;

    /// <summary><c>backgroundTokenizationState</c>.</summary>
    public BackgroundTokenizationState BackgroundTokenizationState => _tokens.BackgroundTokenizationState;

    /// <summary><c>hasTokens</c>.</summary>
    public bool HasTokens => _tokens.HasTokens;

    /// <summary><c>handleDidChangeContent</c>.</summary>
    public void HandleDidChangeContent(ModelContentChangedEventArgs e) => _tokens.HandleDidChangeContent(e);

    /// <summary><c>handleDidChangeAttached</c>.</summary>
    public void HandleDidChangeAttached() => _tokens.HandleDidChangeAttached();

    /// <summary><c>getLineTokens</c>: the syntax tokens of a line.</summary>
    public LineTokens GetLineTokens(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        return _tokens.GetLineTokens(lineNumber);
    }

    private void ValidateLineNumber(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _model.LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "Illegal value for lineNumber");
        }
    }

    /// <summary><c>resetTokenization</c>.</summary>
    public void ResetTokenization() => _tokens.ResetTokenization();

    /// <summary><c>forceTokenization</c>.</summary>
    public void ForceTokenization(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        _tokens.ForceTokenization(lineNumber);
    }

    /// <summary><c>hasAccurateTokensForLine</c>.</summary>
    public bool HasAccurateTokensForLine(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        return _tokens.HasAccurateTokensForLine(lineNumber);
    }

    /// <summary><c>isCheapToTokenize</c>.</summary>
    public bool IsCheapToTokenize(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        return _tokens.IsCheapToTokenize(lineNumber);
    }

    /// <summary><c>tokenizeIfCheap</c>.</summary>
    public void TokenizeIfCheap(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        _tokens.TokenizeIfCheap(lineNumber);
    }

    /// <summary><c>getTokenTypeIfInsertingCharacter</c>.</summary>
    public StandardTokenType GetTokenTypeIfInsertingCharacter(int lineNumber, int column, string character) =>
        _tokens.GetTokenTypeIfInsertingCharacter(lineNumber, column, character);

    /// <summary><c>tokenizeLinesAt</c>.</summary>
    public List<LineTokens>? TokenizeLinesAt(int lineNumber, IReadOnlyList<string> lines) => _tokens.TokenizeLinesAt(lineNumber, lines);

    /// <summary><c>getLanguageId</c>.</summary>
    public string GetLanguageId() => LanguageId;

    /// <summary><c>getLanguageIdAtPosition</c>.</summary>
    public string GetLanguageIdAtPosition(int lineNumber, int column)
    {
        Position position = _model.ValidatePosition(new Position(lineNumber, column));
        LineTokens lineTokens = GetLineTokens(position.LineNumber);
        return lineTokens.GetLanguageId(lineTokens.FindTokenIndexAtOffset(position.Column - 1));
    }

    /// <summary>
    /// <c>setLanguageId</c>. Difference from Monaco, where the registry event or the tokenizer observable resets the
    /// tokenization: it is reset here, so the support of the new language is used.
    /// </summary>
    public void SetLanguageId(string languageId, string source = "api")
    {
        ArgumentNullException.ThrowIfNull(languageId);
        if (LanguageId == languageId)
        {
            // There's nothing to do
            return;
        }

        var e = new ModelLanguageChangedEventArgs(LanguageId, languageId, source);
        LanguageId = languageId;
        _tokens.ResetTokenization();
        _model.BracketPairs.HandleDidChangeLanguage(e);
        _model.RaiseLanguageChanged(e);
        _model.RaiseLanguageConfigurationChanged();
    }

    /// <summary><c>handleLanguageConfigurationServiceChange</c>.</summary>
    public void HandleLanguageConfigurationServiceChange(LanguageConfigurationChangedEventArgs e)
    {
        if (e.Affects(LanguageId))
        {
            _model.RaiseLanguageConfigurationChanged();
        }
    }

    /// <summary><c>getWordAtPosition</c>.</summary>
    public WordAtPosition? GetWordAtPosition(Position originalPosition)
    {
        Position position = _model.ValidatePosition(originalPosition);
        string lineContent = _model.GetLineContent(position.LineNumber);
        LineTokens lineTokens = GetLineTokens(position.LineNumber);
        int tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);

        // (1). First try checking right biased word
        (int rbStartOffset, int rbEndOffset) = FindLanguageBoundaries(lineTokens, tokenIndex);
        WordAtPosition? rightBiasedWord = WordHelper.GetWordAtText(
            position.Column,
            GetLanguageConfiguration(lineTokens.GetLanguageId(tokenIndex)).GetWordDefinition(),
            lineContent[rbStartOffset..rbEndOffset],
            rbStartOffset);
        // Make sure the result touches the original passed in position
        if (
            rightBiasedWord is { } rb &&
            rb.StartColumn <= originalPosition.Column &&
            originalPosition.Column <= rb.EndColumn)
        {
            return rb;
        }

        // (2). Else, if we were at a language boundary, check the left biased word
        if (tokenIndex > 0 && rbStartOffset == position.Column - 1)
        {
            // edge case, where `position` sits between two tokens belonging to two different languages
            (int lbStartOffset, int lbEndOffset) = FindLanguageBoundaries(lineTokens, tokenIndex - 1);
            WordAtPosition? leftBiasedWord = WordHelper.GetWordAtText(
                position.Column,
                GetLanguageConfiguration(lineTokens.GetLanguageId(tokenIndex - 1)).GetWordDefinition(),
                lineContent[lbStartOffset..lbEndOffset],
                lbStartOffset);
            // Make sure the result touches the original passed in position
            if (
                leftBiasedWord is { } lb &&
                lb.StartColumn <= originalPosition.Column &&
                originalPosition.Column <= lb.EndColumn)
            {
                return lb;
            }
        }

        return null;
    }

    private ResolvedLanguageConfiguration GetLanguageConfiguration(string languageId) =>
        _model.LanguageConfigurationRegistry.GetLanguageConfiguration(languageId);

    // Monaco: _findLanguageBoundaries
    private static (int StartOffset, int EndOffset) FindLanguageBoundaries(LineTokens lineTokens, int tokenIndex)
    {
        string languageId = lineTokens.GetLanguageId(tokenIndex);

        // go left until a different language is hit
        int startOffset = 0;
        for (int i = tokenIndex; i >= 0 && lineTokens.GetLanguageId(i) == languageId; i--)
        {
            startOffset = lineTokens.GetStartOffset(i);
        }

        // go right until a different language is hit
        int endOffset = lineTokens.GetLineContent().Length;
        for (
            int i = tokenIndex, tokenCount = lineTokens.GetCount();
            i < tokenCount && lineTokens.GetLanguageId(i) == languageId;
            i++)
        {
            endOffset = lineTokens.GetEndOffset(i);
        }

        return (startOffset, endOffset);
    }

    /// <summary><c>getWordUntilPosition</c>.</summary>
    public WordAtPosition GetWordUntilPosition(Position position)
    {
        WordAtPosition? wordAtPosition = GetWordAtPosition(position);
        if (wordAtPosition is not { } word)
        {
            return new WordAtPosition(string.Empty, position.Column, position.Column);
        }

        return new WordAtPosition(
            word.Word.Substring(0, position.Column - word.StartColumn),
            word.StartColumn,
            position.Column);
    }
}
