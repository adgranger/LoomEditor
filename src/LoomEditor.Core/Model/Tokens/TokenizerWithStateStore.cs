// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// A tokenization support with the end state of every line (Monaco: <c>TokenizerWithStateStore</c>, always with
/// <c>TState = IState</c>: C# has no generic default).
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal class TokenizerWithStateStore
{
    private readonly IState _initialState;

    /// <summary>Creates a tokenizer for a model of <paramref name="lineCount"/> lines.</summary>
    public TokenizerWithStateStore(int lineCount, ITokenizationSupport tokenizationSupport)
    {
        ArgumentNullException.ThrowIfNull(tokenizationSupport);
        TokenizationSupport = tokenizationSupport;
        _initialState = tokenizationSupport.GetInitialState();
        Store = new TrackingTokenizationStateStore<IState>(lineCount);
    }

    /// <summary><c>tokenizationSupport</c>.</summary>
    public ITokenizationSupport TokenizationSupport { get; }

    /// <summary><c>store</c>.</summary>
    public TrackingTokenizationStateStore<IState> Store { get; }

    /// <summary><c>getStartState</c>.</summary>
    public IState? GetStartState(int lineNumber) => Store.GetStartState(lineNumber, _initialState);

    /// <summary><c>getFirstInvalidLine</c>.</summary>
    public (int LineNumber, IState StartState)? GetFirstInvalidLine() => Store.GetFirstInvalidLine(_initialState);
}
