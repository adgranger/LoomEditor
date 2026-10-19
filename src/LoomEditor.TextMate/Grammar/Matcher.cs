// Ported from microsoft/vscode-textmate: src/matcher.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>MatcherWithPriority</c>: a scope-selector matcher and its priority.</summary>
/// <typeparam name="T">The matcher input.</typeparam>
/// <param name="Matcher">The matcher (<c>Matcher&lt;T&gt;</c>).</param>
/// <param name="Priority"><c>-1</c> for <c>L:</c>, <c>1</c> for <c>R:</c>, <c>0</c> otherwise.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct MatcherWithPriority<T>(Func<T, bool> Matcher, int Priority);

/// <summary>
/// Scope selectors (<c>matcher.ts</c>): <c>createMatchers</c> parses a selector such as
/// <c>R:text.html - (comment.block, text.html source)</c> into matchers.
/// </summary>
/// <remarks>Threading: stateless; the matchers it returns are immutable.</remarks>
internal static partial class Matcher
{
    /// <summary><c>createMatchers</c>.</summary>
    /// <param name="selector">The scope selector.</param>
    /// <param name="matchesName">Whether the input matches a sequence of scope names.</param>
    public static List<MatcherWithPriority<T>> CreateMatchers<T>(string selector, Func<string[], T, bool> matchesName)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(matchesName);
        return new Parser<T>(selector, matchesName).Parse();
    }

    // isIdentifier: /[\w\.:]+/ with ASCII \w (not anchored: one such character is enough).
    private static bool IsIdentifier(string? token) => token is not null && IdentifierCharacter().IsMatch(token);

    [GeneratedRegex("[A-Za-z0-9_.:]", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierCharacter();

    // newTokenizer: /([LR]:|[\w\.:][\w\.:\-]*|[\,\|\-\(\)])/g with ASCII \w.
    [GeneratedRegex(@"[LR]:|[A-Za-z0-9_.:][A-Za-z0-9_.:\-]*|[,|\-()]", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    // The closures of createMatchers (parseOperand, parseConjunction, parseInnerExpression) share `token`.
    private sealed class Parser<T>
    {
        private readonly Func<string[], T, bool> _matchesName;
        private Match _match;
        private string? _token;

        public Parser(string selector, Func<string[], T, bool> matchesName)
        {
            _matchesName = matchesName;
            _match = TokenRegex().Match(selector);
        }

        public List<MatcherWithPriority<T>> Parse()
        {
            var results = new List<MatcherWithPriority<T>>();
            _token = Next();
            while (_token is not null)
            {
                int priority = 0;
                if (_token.Length == 2 && _token[1] == ':')
                {
                    switch (_token[0])
                    {
                        case 'R':
                            priority = 1;
                            break;
                        case 'L':
                            priority = -1;
                            break;
                        default:
                            Debug.WriteLine($"Unknown priority {_token} in scope selector");
                            break;
                    }

                    _token = Next();
                }

                Func<T, bool> matcher = ParseConjunction();
                results.Add(new MatcherWithPriority<T>(matcher, priority));
                if (_token != ",")
                {
                    break;
                }

                _token = Next();
            }

            return results;
        }

        // tokenizer.next()
        private string? Next()
        {
            if (!_match.Success)
            {
                return null;
            }

            string res = _match.Value;
            _match = _match.NextMatch();
            return res;
        }

        private Func<T, bool>? ParseOperand()
        {
            if (_token == "-")
            {
                _token = Next();
                Func<T, bool>? expressionToNegate = ParseOperand();
                return matcherInput => expressionToNegate is not null && !expressionToNegate(matcherInput);
            }

            if (_token == "(")
            {
                _token = Next();
                Func<T, bool> expressionInParents = ParseInnerExpression();
                if (_token == ")")
                {
                    _token = Next();
                }

                return expressionInParents;
            }

            if (IsIdentifier(_token))
            {
                var identifiers = new List<string>();
                do
                {
                    identifiers.Add(_token!);
                    _token = Next();
                }
                while (IsIdentifier(_token));

                string[] names = [.. identifiers];
                return matcherInput => _matchesName(names, matcherInput);
            }

            return null;
        }

        private Func<T, bool> ParseConjunction()
        {
            var matchers = new List<Func<T, bool>>();
            Func<T, bool>? matcher = ParseOperand();
            while (matcher is not null)
            {
                matchers.Add(matcher);
                matcher = ParseOperand();
            }

            return matcherInput => matchers.TrueForAll(m => m(matcherInput)); // and
        }

        private Func<T, bool> ParseInnerExpression()
        {
            var matchers = new List<Func<T, bool>>();
            Func<T, bool> matcher = ParseConjunction();
            while (true)
            {
                matchers.Add(matcher);
                if (_token is "|" or ",")
                {
                    do
                    {
                        _token = Next();
                    }
                    while (_token is "|" or ","); // ignore subsequent commas
                }
                else
                {
                    break;
                }

                matcher = ParseConjunction();
            }

            return matcherInput => matchers.Exists(m => m(matcherInput)); // or
        }
    }
}
