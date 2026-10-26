// Ported from microsoft/vscode: src/vs/base/common/filters.ts (FuzzyScore, FuzzyScoreOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

/// <summary>
/// Helpers for a fuzzy score, which is an <c>int[]</c> laid out as Monaco's <c>FuzzyScore</c>:
/// <c>[score, wordStart, matchPosN, ..., matchPos0]</c> (Monaco: <c>FuzzyScore</c> namespace).
/// </summary>
/// <remarks>Threading: stateless; <see cref="Default"/> is shared and must be treated as read-only.</remarks>
internal static class FuzzyScore
{
    /// <summary>No matches and value <c>-100</c>. Callers must not mutate it.</summary>
    public static readonly int[] Default = [-100, 0];

    /// <summary>Whether <paramref name="score"/> is null or the default score (Monaco: <c>FuzzyScore.isDefault</c>).</summary>
    public static bool IsDefault(int[]? score)
        => score is null || (score.Length == 2 && score[0] == -100 && score[1] == 0);
}

/// <summary>Options of <see cref="Fuzzy.Score"/> (Monaco: <c>FuzzyScoreOptions</c>).</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record FuzzyScoreOptions(bool FirstMatchCanBeWeak, bool BoostFullMatch)
{
    /// <summary>Monaco: <c>FuzzyScoreOptions.default</c>.</summary>
    public static readonly FuzzyScoreOptions Default = new(FirstMatchCanBeWeak: false, BoostFullMatch: true);
}
