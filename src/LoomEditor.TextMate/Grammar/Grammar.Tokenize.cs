// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

// tokenizeLine, tokenizeLine2 and _tokenize.
internal sealed partial class Grammar : IGrammar
{
    private RuleId? _rootId;

    /// <inheritdoc />
    public TokenizeLineResult TokenizeLine(string lineText, StateStack? prevState, int timeLimit = 0)
    {
        TokenizeResult r = Tokenize(lineText, ToImpl(prevState), false, timeLimit);
        return new TokenizeLineResult(r.LineTokens.GetResult(r.RuleStack, r.LineLength), r.RuleStack, r.StoppedEarly);
    }

    /// <inheritdoc />
    public TokenizeLineResult2 TokenizeLine2(string lineText, StateStack? prevState, int timeLimit = 0)
    {
        TokenizeResult r = Tokenize(lineText, ToImpl(prevState), true, timeLimit);
        return new TokenizeLineResult2(r.LineTokens.GetBinaryResult(r.RuleStack, r.LineLength), r.RuleStack, r.StoppedEarly);
    }

    private static StateStackImpl? ToImpl(StateStack? prevState) => prevState switch
    {
        null => null,
        StateStackImpl impl => impl,
        _ => throw new ArgumentException("The state was not produced by a grammar.", nameof(prevState)),
    };

    // upstream's anonymous result type of _tokenize
    private sealed record TokenizeResult(int LineLength, LineTokens LineTokens, StateStackImpl RuleStack, bool StoppedEarly);

    // _tokenize
    private TokenizeResult Tokenize(string lineText, StateStackImpl? prevState, bool emitBinaryTokens, int timeLimit)
    {
        ArgumentNullException.ThrowIfNull(lineText);
        if (_rootId is null)
        {
            _rootId = RuleFactory.GetCompiledRuleId(_grammar.Repository!["$self"], this, _grammar.Repository!);
            // This ensures ids are deterministic, and thus equal in renderer and webworker.
            GetInjections();
        }

        bool isFirstLine;
        if (prevState is null || ReferenceEquals(prevState, StateStackImpl.Null))
        {
            isFirstLine = true;
            BasicScopeAttributes rawDefaultMetadata = _basicScopeAttributesProvider.GetDefaultAttributes();
            Theme.StyleAttributes defaultStyle = ThemeProvider.GetDefaults();
            uint defaultMetadata = EncodedTokenAttributes.Set(
                0,
                rawDefaultMetadata.LanguageId,
                rawDefaultMetadata.TokenType,
                null,
                defaultStyle.FontStyle,
                defaultStyle.ForegroundId,
                defaultStyle.BackgroundId);

            string? rootScopeName = GetRule(_rootId.Value)!.GetName(null, null);

            AttributedScopeStack scopeList = rootScopeName is not null
                ? AttributedScopeStack.CreateRootAndLookUpScopeName(rootScopeName, defaultMetadata, this)
                : AttributedScopeStack.CreateRoot("unknown", defaultMetadata);

            prevState = new StateStackImpl(null, _rootId.Value, -1, -1, false, null, scopeList, scopeList);
        }
        else
        {
            isFirstLine = false;
            prevState.Reset();
        }

        lineText += "\n";
        using IOnigString onigLineText = CreateOnigString(lineText);
        int lineLength = onigLineText.Content.Length;
        var lineTokens = new LineTokens(emitBinaryTokens, lineText, _tokenTypeMatchers, _balancedBracketSelectors);
        TokenizeStringResult r = TokenizeString.Tokenize(this, onigLineText, isFirstLine, 0, prevState, lineTokens, true, timeLimit);

        return new TokenizeResult(lineLength, lineTokens, r.Stack, r.StoppedEarly);
    }
}
