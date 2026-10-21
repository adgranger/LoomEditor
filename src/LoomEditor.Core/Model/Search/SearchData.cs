// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Search;

/// <summary>A parsed search request (Monaco: <c>SearchData</c>).</summary>
/// <param name="regex">The regex to search for.</param>
/// <param name="wordSeparators">The word separator classifier, or <c>null</c> when matches need not be whole words.</param>
/// <param name="simpleSearch">The plain string to search for, when a regex is not needed.</param>
/// <remarks>Threading: immutable; the regex is thread-safe.</remarks>
internal sealed class SearchData(Regex regex, WordCharacterClassifier? wordSeparators, string? simpleSearch)
{
    /// <summary>The regex to search for. Always defined.</summary>
    public Regex Regex { get; } = regex;

    /// <summary>The word separator classifier.</summary>
    public WordCharacterClassifier? WordSeparators { get; } = wordSeparators;

    /// <summary>The simple string to search for (if possible).</summary>
    public string? SimpleSearch { get; } = simpleSearch;

    /// <summary>Whether the regex is multiline (Monaco: <c>regex.multiline</c>).</summary>
    public bool IsMultiline => Regex.Options.HasFlag(RegexOptions.Multiline);
}
