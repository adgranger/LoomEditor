// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findState.ts (FindReplaceStateChangedEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>What changed in a <see cref="FindReplaceState"/> (Monaco: <c>FindReplaceStateChangedEvent</c>, without <c>filters</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class FindReplaceStateChangedEventArgs : EventArgs
{
    public bool MoveCursor { get; init; }

    public bool UpdateHistory { get; init; }

    public bool SearchString { get; init; }

    public bool ReplaceString { get; init; }

    public bool IsRevealed { get; init; }

    public bool IsReplaceRevealed { get; init; }

    public bool IsRegex { get; init; }

    public bool WholeWord { get; init; }

    public bool MatchCase { get; init; }

    public bool PreserveCase { get; init; }

    public bool SearchScope { get; init; }

    public bool MatchesPosition { get; init; }

    public bool MatchesCount { get; init; }

    public bool CurrentMatch { get; init; }

    public bool Loop { get; init; }

    public bool IsSearching { get; init; }
}
