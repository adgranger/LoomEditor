// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// A <see cref="TokenizerWithStateStore"/> that reads its lines from a <see cref="TextModel"/> (Monaco:
/// <c>TokenizerWithStateStoreAndTextModel</c>). The language id comes from <see cref="ITokenizationModelInfo"/>.
/// </summary>
/// <remarks>
/// Threading: not thread-safe. Difference from Monaco: an exception of the tokenization support is not reported with
/// <c>onUnexpectedError</c> but traced with <see cref="Debug.WriteLine(string)"/>, and the line gets the null tokens.
/// </remarks>
internal sealed class TokenizerWithStateStoreAndTextModel : TokenizerWithStateStore
{
    /// <summary><c>CHEAP_TOKENIZATION_LENGTH_LIMIT</c>.</summary>
    private const int CheapTokenizationLengthLimit = 2048;

    private readonly ILanguageIdCodec _languageIdCodec;

    /// <summary>Creates a tokenizer for <paramref name="model"/>.</summary>
    public TokenizerWithStateStoreAndTextModel(
        int lineCount,
        ITokenizationSupport tokenizationSupport,
        TextModel model,
        ITokenizationModelInfo modelInfo,
        ILanguageIdCodec languageIdCodec)
        : base(lineCount, tokenizationSupport)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(modelInfo);
        ArgumentNullException.ThrowIfNull(languageIdCodec);
        Model = model;
        ModelInfo = modelInfo;
        _languageIdCodec = languageIdCodec;
    }

    /// <summary><c>_textModel</c>.</summary>
    public TextModel Model { get; }

    /// <summary>Monaco's <c>_textModel.getLanguageId()</c> and <c>isAttachedToEditor()</c>.</summary>
    public ITokenizationModelInfo ModelInfo { get; }

    /// <summary><c>updateTokensUntilLine</c>: tokenizes from the first invalid line to <paramref name="lineNumber"/>.</summary>
    public void UpdateTokensUntilLine(ContiguousMultilineTokensBuilder builder, int lineNumber)
    {
        string languageId = ModelInfo.LanguageId;

        while (true)
        {
            (int LineNumber, IState StartState)? lineToTokenize = GetFirstInvalidLine();
            if (lineToTokenize is null || lineToTokenize.Value.LineNumber > lineNumber)
            {
                break;
            }

            (int number, IState startState) = lineToTokenize.Value;
            string text = Model.GetLineContent(number);

            EncodedTokenizationResult r = SafeTokenize(_languageIdCodec, languageId, TokenizationSupport, text, true, startState);
            builder.Add(number, r.Tokens);
            Store.SetEndState(number, r.EndState);
        }
    }

    /// <summary><c>getTokenTypeIfInsertingCharacter</c>: assumes the state is up to date.</summary>
    public StandardTokenType GetTokenTypeIfInsertingCharacter(Position position, string character)
    {
        IState? lineStartState = GetStartState(position.LineNumber);
        if (lineStartState is null)
        {
            return StandardTokenType.Other;
        }

        string languageId = ModelInfo.LanguageId;
        string lineContent = Model.GetLineContent(position.LineNumber);

        // Create the text as if `character` was inserted
        string text = string.Concat(lineContent.AsSpan(0, position.Column - 1), character, lineContent.AsSpan(position.Column - 1));

        EncodedTokenizationResult r = SafeTokenize(_languageIdCodec, languageId, TokenizationSupport, text, true, lineStartState);
        var lineTokens = new LineTokens(r.Tokens, text, _languageIdCodec);
        if (lineTokens.GetCount() == 0)
        {
            return StandardTokenType.Other;
        }

        int tokenIndex = lineTokens.FindTokenIndexAtOffset(position.Column - 1);
        return lineTokens.GetStandardTokenType(tokenIndex);
    }

    /// <summary><c>tokenizeLinesAt</c>: assumes the state is up to date; <c>null</c> when the start state is unknown.</summary>
    public List<LineTokens>? TokenizeLinesAt(int lineNumber, IReadOnlyList<string> lines)
    {
        IState? lineStartState = GetStartState(lineNumber);
        if (lineStartState is null)
        {
            return null;
        }

        string languageId = ModelInfo.LanguageId;
        var result = new List<LineTokens>();

        IState state = lineStartState;
        foreach (string line in lines)
        {
            EncodedTokenizationResult r = SafeTokenize(_languageIdCodec, languageId, TokenizationSupport, line, true, state);
            result.Add(new LineTokens(r.Tokens, line, _languageIdCodec));
            state = r.EndState;
        }

        return result;
    }

    /// <summary><c>hasAccurateTokensForLine</c>.</summary>
    public bool HasAccurateTokensForLine(int lineNumber) => lineNumber < Store.GetFirstInvalidEndStateLineNumberOrMax();

    /// <summary><c>isCheapToTokenize</c>.</summary>
    public bool IsCheapToTokenize(int lineNumber)
    {
        int firstInvalidLineNumber = Store.GetFirstInvalidEndStateLineNumberOrMax();
        if (lineNumber < firstInvalidLineNumber)
        {
            return true;
        }

        return lineNumber == firstInvalidLineNumber && Model.GetLineLength(lineNumber) < CheapTokenizationLengthLimit;
    }

    /// <summary>
    /// <c>tokenizeHeuristically</c>: the result is not cached. Returns whether the tokens are heuristic (computed from a
    /// guessed start state).
    /// </summary>
    public bool TokenizeHeuristically(ContiguousMultilineTokensBuilder builder, int startLineNumber, int endLineNumber)
    {
        if (endLineNumber <= Store.GetFirstInvalidEndStateLineNumberOrMax())
        {
            // nothing to do
            return false;
        }

        if (startLineNumber <= Store.GetFirstInvalidEndStateLineNumberOrMax())
        {
            // tokenization has reached the viewport start...
            UpdateTokensUntilLine(builder, endLineNumber);
            return false;
        }

        IState state = GuessStartState(startLineNumber);
        string languageId = ModelInfo.LanguageId;

        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            string text = Model.GetLineContent(lineNumber);
            EncodedTokenizationResult r = SafeTokenize(_languageIdCodec, languageId, TokenizationSupport, text, true, state);
            builder.Add(lineNumber, r.Tokens);
            state = r.EndState;
        }

        return true;
    }

    private IState GuessStartState(int lineNumber)
    {
        (List<string> likelyRelevantLines, IState? initialState) = FindLikelyRelevantLines(Model, lineNumber, this);

        initialState ??= TokenizationSupport.GetInitialState();

        string languageId = ModelInfo.LanguageId;
        IState state = initialState;
        foreach (string line in likelyRelevantLines)
        {
            EncodedTokenizationResult r = SafeTokenize(_languageIdCodec, languageId, TokenizationSupport, line, false, state);
            state = r.EndState;
        }

        return state;
    }

    /// <summary><c>findLikelyRelevantLines</c>: the lines above <paramref name="lineNumber"/> with a smaller indentation.</summary>
    public static (List<string> LikelyRelevantLines, IState? InitialState) FindLikelyRelevantLines(
        TextModel model,
        int lineNumber,
        TokenizerWithStateStore? store = null)
    {
        int nonWhitespaceColumn = model.GetLineFirstNonWhitespaceColumn(lineNumber);
        var likelyRelevantLines = new List<string>();
        IState? initialState = null;
        for (int i = lineNumber - 1; nonWhitespaceColumn > 1 && i >= 1; i--)
        {
            int newNonWhitespaceIndex = model.GetLineFirstNonWhitespaceColumn(i);
            // Ignore lines full of whitespace
            if (newNonWhitespaceIndex == 0)
            {
                continue;
            }

            if (newNonWhitespaceIndex < nonWhitespaceColumn)
            {
                likelyRelevantLines.Add(model.GetLineContent(i));
                nonWhitespaceColumn = newNonWhitespaceIndex;
                initialState = store?.GetStartState(i);
                if (initialState is not null)
                {
                    break;
                }
            }
        }

        likelyRelevantLines.Reverse();
        return (likelyRelevantLines, initialState);
    }

    /// <summary>
    /// <c>safeTokenize</c>: tokenizes with a clone of <paramref name="state"/>; a missing or throwing support gives the
    /// null tokens. The tokens are converted to end offsets.
    /// </summary>
    public static EncodedTokenizationResult SafeTokenize(
        ILanguageIdCodec languageIdCodec,
        string languageId,
        ITokenizationSupport? tokenizationSupport,
        string text,
        bool hasEol,
        IState state)
    {
        EncodedTokenizationResult? r = null;

        if (tokenizationSupport is not null)
        {
            try
            {
                r = tokenizationSupport.TokenizeEncoded(text, hasEol, state.Clone());
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Tokenization failed: {e}");
            }
        }

        r ??= NullTokenize.NullTokenizeEncoded(languageIdCodec.EncodeLanguageId(languageId), state);

        LineTokens.ConvertToEndOffset(r.Tokens, text.Length);
        return r;
    }
}
