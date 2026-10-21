// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/replacePattern.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Find;

/// <summary>A parsed replace string: <c>$0</c>, <c>$&amp;</c>, <c>$1</c>…<c>$99</c>, <c>\n</c>, <c>\t</c>, <c>\\</c>, <c>\u \U \l \L</c>.</summary>
/// <remarks>
/// Behavior difference: the case operators use invariant, length-preserving case conversion (see <see cref="SearchStrings"/>).
/// Threading: immutable; thread-safe.
/// </remarks>
internal sealed partial class ReplacePattern
{
    private readonly string? _staticValue;
    private readonly IReadOnlyList<ReplacePiece>? _pieces;

    /// <summary>Creates a pattern from pieces; no pieces means the empty static value (Monaco: the constructor).</summary>
    public ReplacePattern(IReadOnlyList<ReplacePiece>? pieces)
    {
        if (pieces is null || pieces.Count == 0)
        {
            _staticValue = "";
        }
        else if (pieces.Count == 1 && pieces[0].StaticValue is { } staticValue)
        {
            _staticValue = staticValue;
        }
        else
        {
            _pieces = pieces;
        }
    }

    /// <summary>A pattern that always yields <paramref name="value"/>.</summary>
    public static ReplacePattern FromStaticValue(string value) => new([ReplacePiece.CreateStaticValue(value)]);

    /// <summary>Whether the replace string has <c>$n</c> or case operators, i.e. depends on the match.</summary>
    public bool HasReplacementPatterns => _pieces is not null;

    /// <summary>The value of a pattern without replacement patterns, else <c>null</c> (for tests).</summary>
    internal string? StaticValue => _staticValue;

    /// <summary>The pieces of a pattern with replacement patterns, else <c>null</c> (for tests).</summary>
    internal IReadOnlyList<ReplacePiece>? Pieces => _pieces;

    /// <summary>The text that replaces a match.</summary>
    /// <param name="matches">The groups of the match (index 0 is the whole match; <c>null</c> entries did not take part), or <c>null</c>.</param>
    /// <param name="preserveCase">Whether to apply the case of the match to a static replacement.</param>
    public string BuildReplaceString(IReadOnlyList<string?>? matches, bool preserveCase = false)
    {
        if (_pieces is null)
        {
            return preserveCase ? SearchStrings.BuildReplaceStringWithCasePreserved(matches, _staticValue!) : _staticValue!;
        }

        var result = new StringBuilder();
        foreach (ReplacePiece piece in _pieces)
        {
            if (piece.StaticValue is { } staticValue)
            {
                result.Append(staticValue);
                continue;
            }

            // match index ReplacePiece
            string match = Substitute(piece.MatchIndex, matches);
            if (piece.CaseOps is { Count: > 0 } caseOps)
            {
                match = ApplyCaseOps(match, caseOps);
            }

            result.Append(match);
        }

        return result.ToString();
    }

    private static string ApplyCaseOps(string match, IReadOnlyList<char> caseOps)
    {
        var repl = new StringBuilder();
        var opIdx = 0;
        for (var idx = 0; idx < match.Length; idx++)
        {
            if (opIdx >= caseOps.Count)
            {
                repl.Append(match, idx, match.Length - idx);
                break;
            }

            switch (caseOps[opIdx])
            {
                case 'U':
                    repl.Append(char.ToUpperInvariant(match[idx]));
                    break;
                case 'u':
                    repl.Append(char.ToUpperInvariant(match[idx]));
                    opIdx++;
                    break;
                case 'L':
                    repl.Append(char.ToLowerInvariant(match[idx]));
                    break;
                case 'l':
                    repl.Append(char.ToLowerInvariant(match[idx]));
                    opIdx++;
                    break;
                default:
                    repl.Append(match[idx]);
                    break;
            }
        }

        return repl.ToString();
    }

    private static string Substitute(int matchIndex, IReadOnlyList<string?>? matches)
    {
        if (matches is null)
        {
            return "";
        }

        if (matchIndex == 0)
        {
            return matches[0] ?? "";
        }

        var remainder = "";
        while (matchIndex > 0)
        {
            if (matchIndex < matches.Count)
            {
                // A match can be undefined
                return (matches[matchIndex] ?? "") + remainder;
            }

            remainder = (matchIndex % 10) + remainder;
            matchIndex /= 10;
        }

        return "$" + remainder;
    }
}

/// <summary>One piece of a <see cref="ReplacePattern"/>: a static text, or a group of the match with case operators.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class ReplacePiece
{
    private ReplacePiece(string? staticValue, int matchIndex, IReadOnlyList<char>? caseOps)
    {
        StaticValue = staticValue;
        MatchIndex = matchIndex;
        CaseOps = caseOps is null || caseOps.Count == 0 ? null : [.. caseOps];
    }

    /// <summary>The text, or <c>null</c> for a group piece.</summary>
    public string? StaticValue { get; }

    /// <summary>The group index (−1 for a static piece).</summary>
    public int MatchIndex { get; }

    /// <summary>The case operators (<c>u U l L</c>) applied to the group, or <c>null</c>.</summary>
    public IReadOnlyList<char>? CaseOps { get; }

    public static ReplacePiece CreateStaticValue(string value) => new(value, -1, null);

    public static ReplacePiece CreateMatchIndex(int index) => new(null, index, null);

    public static ReplacePiece CreateCaseOps(int index, IReadOnlyList<char> caseOps) => new(null, index, caseOps);
}
