// Ported from microsoft/vscode: src/vs/editor/test/common/model/bracketPairColorizer/tokenizer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>A token of a <see cref="TokenizedDocument"/>: its text and the metadata it is tokenized with.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record TokenInfo(string Text, int LanguageId, StandardTokenType TokenType, bool HasBalancedBrackets)
{
    public uint GetMetadata() =>
        ((uint)LanguageId << MetadataConsts.LanguageIdOffset)
        | ((uint)TokenType << MetadataConsts.TokenTypeOffset)
        | (HasBalancedBrackets ? MetadataConsts.BalancedBracketsMask : 0);

    public TokenInfo WithText(string text) => this with { Text = text };
}

/// <summary>A document whose syntax tokens are given by the test, line by line (Monaco: <c>TokenizedDocument</c>).</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class TokenizedDocument
{
    private readonly List<List<TokenInfo>> _tokensByLine;

    public TokenizedDocument(IReadOnlyList<TokenInfo> tokens)
    {
        var tokensByLine = new List<List<TokenInfo>>();
        var curLine = new List<TokenInfo>();

        foreach (var token in tokens)
        {
            var lines = token.Text.Split('\n').ToList();
            var first = true;
            while (lines.Count > 0)
            {
                if (!first)
                {
                    tokensByLine.Add(curLine);
                    curLine = [];
                }
                else
                {
                    first = false;
                }

                if (lines[0].Length > 0)
                {
                    curLine.Add(token.WithText(lines[0]));
                }

                // Monaco's `lines.pop()` removes the last line (not the first): kept as is, so that a token that spans
                // several lines tokenizes exactly like upstream.
                lines.RemoveAt(lines.Count - 1);
            }
        }

        tokensByLine.Add(curLine);
        _tokensByLine = tokensByLine;
    }

    public string GetText() => string.Join('\n', _tokensByLine.Select(line => string.Concat(line.Select(t => t.Text))));

    public ITokenizationSupport GetTokenizationSupport() => new TokenizationSupport(this);

    private sealed class State(int lineNumber) : IState
    {
        public int LineNumber { get; } = lineNumber;

        public IState Clone() => new State(LineNumber);

        public bool Equals(IState? other) => other is State state && LineNumber == state.LineNumber;
    }

    private sealed class TokenizationSupport(TokenizedDocument document) : ITokenizationSupport
    {
        public IState GetInitialState() => new State(0);

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            var state2 = (State)state;
            var tokens = document._tokensByLine[state2.LineNumber];
            var arr = new List<uint>();
            uint offset = 0;
            foreach (var token in tokens)
            {
                arr.Add(offset);
                arr.Add(token.GetMetadata());
                offset += (uint)token.Text.Length;
            }

            return new EncodedTokenizationResult([.. arr], new State(state2.LineNumber + 1));
        }
    }
}
