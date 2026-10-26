// Ported from microsoft/vscode: src/vs/base/common/filters.ts (FuzzyScorer)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Filters;

/// <summary>A fuzzy scoring function, e.g. <see cref="Fuzzy.Score"/> (Monaco: <c>FuzzyScorer</c>).</summary>
internal delegate int[]? FuzzyScorer(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, FuzzyScoreOptions? options);
