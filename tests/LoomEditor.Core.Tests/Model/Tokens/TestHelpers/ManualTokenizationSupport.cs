// Ported from microsoft/vscode: src/vs/editor/test/common/model/model.line.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens.TestHelpers;

/// <summary>Monaco's <c>LineState</c>: the number of the line to tokenize.</summary>
internal sealed class LineState(int lineNumber) : IState
{
    public int LineNumber { get; } = lineNumber;

    public IState Clone() => this;

    public bool Equals(IState? other) => (other as LineState)?.LineNumber == LineNumber;
}

/// <summary>
/// Monaco's <c>ManualTokenizationSupport</c>: the test sets the tokens of a line, which are sent to the background
/// tokenization stores of the models using this support.
/// </summary>
internal sealed class ManualTokenizationSupport : ITokenizationSupport
{
    // Never filled upstream either: the tokens only reach the models through the stores.
    private readonly Dictionary<int, uint[]> _tokens = [];
    private readonly HashSet<IBackgroundTokenizationStore> _stores = [];

    public void SetLineTokens(int lineNumber, uint[] tokens)
    {
        var builder = new ContiguousMultilineTokensBuilder();
        builder.Add(lineNumber, tokens);
        foreach (IBackgroundTokenizationStore store in _stores)
        {
            store.SetTokens(builder.Finalize());
        }
    }

    public IState GetInitialState() => new LineState(1);

    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
    {
        var s = (LineState)state;
        return new EncodedTokenizationResult(_tokens[s.LineNumber], new LineState(s.LineNumber + 1));
    }

    public IBackgroundTokenizer? CreateBackgroundTokenizer(TextModel textModel, IBackgroundTokenizationStore store)
    {
        _stores.Add(store);
        return new Tokenizer(() => _stores.Remove(store));
    }

    private sealed class Tokenizer(Action onDispose) : IBackgroundTokenizer
    {
        public void Dispose() => onDispose();

        public void RequestTokens(int startLineNumber, int endLineNumberExclusive)
        {
        }
    }
}
