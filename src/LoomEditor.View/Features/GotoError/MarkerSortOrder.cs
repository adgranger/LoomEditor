// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts (the problems.sortOrder setting)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.GotoError;

/// <summary>How a <see cref="MarkerList"/> sorts the problems (Monaco: the <c>problems.sortOrder</c> setting).</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
internal enum MarkerSortOrder
{
    /// <summary>The most severe first, then by position (the Monaco default).</summary>
    Severity,

    /// <summary>By position, then the most severe first.</summary>
    Position,
}
