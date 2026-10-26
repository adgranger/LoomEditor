// Ported from microsoft/vscode: src/vs/platform/markers/common/markers.ts (IMarker)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>
/// A marker stored by the <see cref="MarkerService"/>: a <see cref="MarkerData"/> flattened with its owner (Monaco <c>IMarker</c>).
/// Instances are stable: <see cref="MarkerService.Read"/> returns the same objects until their owner calls <see cref="MarkerService.Change"/>.
/// Equality is by reference on purpose.
/// </summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
public sealed class Marker
{
    internal Marker(
        string owner,
        MarkerSeverity severity,
        string message,
        int startLineNumber,
        int startColumn,
        int endLineNumber,
        int endColumn,
        string? code,
        string? source,
        int? modelVersionId,
        IReadOnlyList<MarkerTag>? tags)
    {
        Owner = owner;
        Severity = severity;
        Message = message;
        StartLineNumber = startLineNumber;
        StartColumn = startColumn;
        EndLineNumber = endLineNumber;
        EndColumn = endColumn;
        Code = code;
        Source = source;
        ModelVersionId = modelVersionId;
        Tags = tags;
    }

    /// <summary>The owner that set this marker.</summary>
    public string Owner { get; }

    /// <summary>How serious the problem is.</summary>
    public MarkerSeverity Severity { get; }

    /// <summary>The text shown to the user.</summary>
    public string Message { get; }

    /// <summary>First line, 1-based.</summary>
    public int StartLineNumber { get; }

    /// <summary>First column, 1-based.</summary>
    public int StartColumn { get; }

    /// <summary>Last line, 1-based.</summary>
    public int EndLineNumber { get; }

    /// <summary>Column after the last character, 1-based.</summary>
    public int EndColumn { get; }

    /// <summary>The problem's code, if any.</summary>
    public string? Code { get; }

    /// <summary>The tool that reported it, if any.</summary>
    public string? Source { get; }

    /// <summary>The model version the positions were computed against, when known.</summary>
    public int? ModelVersionId { get; }

    /// <summary>Presentation hints, if any.</summary>
    public IReadOnlyList<MarkerTag>? Tags { get; }
}
