// Ported from microsoft/vscode: src/vs/platform/markers/common/markers.ts (MarkerStatistics)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>The number of markers per severity; hints and any other severity count as unknown (Monaco <c>MarkerStats._add</c>).</summary>
/// <param name="Errors">Number of errors.</param>
/// <param name="Warnings">Number of warnings.</param>
/// <param name="Infos">Number of infos.</param>
/// <param name="Unknowns">Number of other markers.</param>
/// <remarks>Threading: immutable value, safe to share between threads.</remarks>
public readonly record struct MarkerStatistics(int Errors, int Warnings, int Infos, int Unknowns);
