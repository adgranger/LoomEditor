// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (setupAutoClosingLanguageTokenization)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.Cursors.TestHelpers;

/// <summary>
/// The tokenizer of the auto closing test language: strings (<c>'</c>, <c>"</c>, <c>`</c>), line comments, block comments
/// and braces that nest.
/// </summary>
/// <remarks>Threading: stateless apart from its immutable states.</remarks>
internal sealed class AutoClosingLanguageTokenizationSupport(int encodedLanguageId) : ITokenizationSupport
{
    private abstract class State : IState
    {
        public IState Clone() => this;

        public abstract bool Equals(IState? other);
    }

    private sealed class BaseState(State? parent = null) : State
    {
        public State? Parent { get; } = parent;

        public override bool Equals(IState? other)
        {
            if (other is not BaseState otherBase)
            {
                return false;
            }

            if (Parent is null && otherBase.Parent is null)
            {
                return true;
            }

            if (Parent is null || otherBase.Parent is null)
            {
                return false;
            }

            return Parent.Equals(otherBase.Parent);
        }
    }

    private sealed class StringState(char quote, State parentState) : State
    {
        public char Quote { get; } = quote;

        public State ParentState { get; } = parentState;

        public override bool Equals(IState? other) =>
            other is StringState s && Quote == s.Quote && ParentState.Equals(s.ParentState);
    }

    private sealed class BlockCommentState(State parentState) : State
    {
        public State ParentState { get; } = parentState;

        // Monaco: `other instanceof StringState` (sic): the check is on the wrong class, so a block comment state is never equal to another block comment state.
        public override bool Equals(IState? other) => other is StringState s && ParentState.Equals(s.ParentState);
    }

    public IState GetInitialState() => new BaseState();

    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
    {
        var current = (State)state;
        var tokens = new List<(int Length, StandardTokenType Type)>();
        string remaining = line;

        void GenerateToken(int length, StandardTokenType type, State? newState = null)
        {
            if (tokens.Count > 0 && tokens[^1].Type == type)
            {
                // grow last tokens
                tokens[^1] = (tokens[^1].Length + length, type);
            }
            else
            {
                tokens.Add((length, type));
            }

            remaining = remaining.Substring(length);
            if (newState is not null)
            {
                current = newState;
            }
        }

        void Advance()
        {
            if (current is BaseState baseState)
            {
                int run = CountWhile(remaining, c => c is not ('\'' or '"' or '`' or '{' or '}' or '/'));
                if (run > 0)
                {
                    GenerateToken(run, StandardTokenType.Other);
                }
                else if (remaining[0] is '\'' or '"' or '`')
                {
                    GenerateToken(1, StandardTokenType.String, new StringState(remaining[0], baseState));
                }
                else if (remaining[0] == '{')
                {
                    GenerateToken(1, StandardTokenType.Other, new BaseState(baseState));
                }
                else if (remaining[0] == '}')
                {
                    GenerateToken(1, StandardTokenType.Other, baseState.Parent ?? new BaseState());
                }
                else if (remaining.StartsWith("//", StringComparison.Ordinal))
                {
                    GenerateToken(remaining.Length, StandardTokenType.Comment, baseState);
                }
                else if (remaining.StartsWith("/*", StringComparison.Ordinal))
                {
                    GenerateToken(2, StandardTokenType.Comment, new BlockCommentState(baseState));
                }
                else
                {
                    GenerateToken(1, StandardTokenType.Other, baseState);
                }
            }
            else if (current is StringState stringState)
            {
                int run = CountWhile(remaining, c => c is not ('\\' or '\'' or '"' or '`' or '$'));
                if (run > 0)
                {
                    GenerateToken(run, StandardTokenType.String);
                }
                else if (remaining[0] == '\\')
                {
                    // Monaco: generateToken(2, ...) on a line that ends with the backslash cannot happen in the tests.
                    GenerateToken(Math.Min(2, remaining.Length), StandardTokenType.String);
                }
                else if (remaining[0] == stringState.Quote)
                {
                    GenerateToken(1, StandardTokenType.String, stringState.ParentState);
                }
                else if (remaining.StartsWith("${", StringComparison.Ordinal))
                {
                    GenerateToken(2, StandardTokenType.Other, new BaseState(stringState));
                }
                else
                {
                    GenerateToken(1, StandardTokenType.Other, stringState);
                }
            }
            else if (current is BlockCommentState blockCommentState)
            {
                int run = CountWhile(remaining, c => c != '*');
                if (run > 0)
                {
                    GenerateToken(run, StandardTokenType.String);
                }
                else if (remaining.StartsWith("*/", StringComparison.Ordinal))
                {
                    GenerateToken(2, StandardTokenType.Comment, blockCommentState.ParentState);
                }
                else
                {
                    GenerateToken(1, StandardTokenType.Other, blockCommentState);
                }
            }
            else
            {
                throw new InvalidOperationException("unknown state");
            }
        }

        while (remaining.Length > 0)
        {
            Advance();
        }

        uint[] result = new uint[tokens.Count * 2];
        int startIndex = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            result[2 * i] = (uint)startIndex;
            result[(2 * i) + 1] = ((uint)encodedLanguageId << MetadataConsts.LanguageIdOffset)
                | ((uint)tokens[i].Type << MetadataConsts.TokenTypeOffset);
            startIndex += tokens[i].Length;
        }

        return new EncodedTokenizationResult(result, current);
    }

    private static int CountWhile(string text, Func<char, bool> predicate)
    {
        int count = 0;
        while (count < text.Length && predicate(text[count]))
        {
            count++;
        }

        return count;
    }
}
