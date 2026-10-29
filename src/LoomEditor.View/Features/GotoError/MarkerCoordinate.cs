// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts (MarkerCoordinate)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Markers;

namespace LoomEditor.View.Features.GotoError;

/// <summary>A problem and its place in the list.</summary>
/// <remarks>Threading: immutable record, safe to share between threads.</remarks>
/// <param name="Marker">The problem.</param>
/// <param name="Index">Its 1-based index in the sorted list.</param>
/// <param name="Total">The number of problems in the list.</param>
internal sealed record MarkerCoordinate(Marker Marker, int Index, int Total);
