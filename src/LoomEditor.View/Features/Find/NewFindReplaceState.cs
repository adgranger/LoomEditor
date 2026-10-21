// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findState.ts (INewFindReplaceState)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>The changes to apply to a <see cref="FindReplaceState"/>; a <c>null</c> member is left unchanged.</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record NewFindReplaceState
{
    private readonly IReadOnlyList<Range>? _searchScope;

    public string? SearchString { get; init; }

    public string? ReplaceString { get; init; }

    public bool? IsRevealed { get; init; }

    public bool? IsReplaceRevealed { get; init; }

    public bool? IsRegex { get; init; }

    public FindOptionOverride? IsRegexOverride { get; init; }

    public bool? WholeWord { get; init; }

    public FindOptionOverride? WholeWordOverride { get; init; }

    public bool? MatchCase { get; init; }

    public FindOptionOverride? MatchCaseOverride { get; init; }

    public bool? PreserveCase { get; init; }

    public FindOptionOverride? PreserveCaseOverride { get; init; }

    /// <summary>
    /// The new search scope. Absent, <c>null</c> (clear the scope) and ranges are three different states:
    /// writing <c>SearchScope = null</c> in an initializer sets <see cref="HasSearchScope"/> too.
    /// </summary>
    public IReadOnlyList<Range>? SearchScope
    {
        get => _searchScope;
        init
        {
            _searchScope = value;
            HasSearchScope = true;
        }
    }

    /// <summary>Whether <see cref="SearchScope"/> was written (Monaco: <c>searchScope !== undefined</c>).</summary>
    public bool HasSearchScope { get; private init; }

    public bool? Loop { get; init; }

    public bool? IsSearching { get; init; }
}
