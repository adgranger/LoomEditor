// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findModel.ts (MATCHES_LIMIT, RESEARCH_DELAY)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>Limits shared by the find model and the find state.</summary>
/// <remarks>Threading: constants; thread-safe.</remarks>
internal static class FindConstants
{
    /// <summary>The most matches the find model keeps (Monaco: <c>MATCHES_LIMIT</c>).</summary>
    public const int MatchesLimit = 19999;

    /// <summary>The delay before a new search after the text changed (Monaco: <c>RESEARCH_DELAY</c>).</summary>
    public static readonly TimeSpan ResearchDelay = TimeSpan.FromMilliseconds(240);
}
