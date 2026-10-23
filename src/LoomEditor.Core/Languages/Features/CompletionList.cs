// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionList)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>The result of a completion request.</summary>
/// <remarks>Threading: immutable (the provider must not mutate the list afterwards).</remarks>
/// <param name="suggestions">The items.</param>
/// <param name="incomplete">True when typing more characters requires a new request.</param>
public sealed class CompletionList(IReadOnlyList<CompletionItem> suggestions, bool incomplete = false)
{
    /// <summary>The items.</summary>
    public IReadOnlyList<CompletionItem> Suggestions { get; } = suggestions ?? throw new ArgumentNullException(nameof(suggestions));

    /// <summary>True when typing more characters requires a new request.</summary>
    public bool Incomplete { get; } = incomplete;
}
