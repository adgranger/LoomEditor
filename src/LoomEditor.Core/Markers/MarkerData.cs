// Ported from microsoft/vscode: src/vs/platform/markers/common/markers.ts (IMarkerData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>
/// A problem as an owner reports it. Differences from Monaco's <c>IMarkerData</c>: no <c>relatedInformation</c> or <c>origin</c>, and
/// <c>code</c> is a plain string. Positions are 1-based; the service sanitizes them.
/// </summary>
/// <param name="Severity">How serious the problem is (not validated).</param>
/// <param name="Message">The text shown to the user; items with a null or empty message are dropped by the service.</param>
/// <param name="StartLineNumber">First line, 1-based.</param>
/// <param name="StartColumn">First column, 1-based.</param>
/// <param name="EndLineNumber">Last line, 1-based.</param>
/// <param name="EndColumn">Column after the last character, 1-based.</param>
/// <param name="Code">The problem's code (for example <c>CS0103</c>).</param>
/// <param name="Source">The tool that reported it (for example <c>csharp</c>).</param>
/// <param name="ModelVersionId">The model version the positions were computed against, when known.</param>
/// <param name="Tags">Presentation hints.</param>
/// <remarks>Threading: immutable record, safe to share between threads.</remarks>
public sealed record MarkerData(
    MarkerSeverity Severity,
    string Message,
    int StartLineNumber,
    int StartColumn,
    int EndLineNumber,
    int EndColumn,
    string? Code = null,
    string? Source = null,
    int? ModelVersionId = null,
    IReadOnlyList<MarkerTag>? Tags = null);
