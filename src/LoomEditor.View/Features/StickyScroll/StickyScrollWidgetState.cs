// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>What the sticky scroll widget shows. Monaco: <c>StickyScrollWidgetState</c>.</summary>
/// <remarks>
/// Threading: immutable by convention, safe from any thread. Equality compares the line lists element-wise
/// (the compiler-generated one would compare the list references).
/// </remarks>
internal sealed record StickyScrollWidgetState(
    IReadOnlyList<int> StartLineNumbers,
    IReadOnlyList<int> EndLineNumbers,
    int LastLineRelativePosition,
    int? ShowEndForLine = null)
{
    /// <summary>The state with no sticky line. Monaco: <c>StickyScrollWidgetState.Empty</c>.</summary>
    public static StickyScrollWidgetState Empty => new([], [], 0);

    /// <summary>Monaco: <c>equals</c>.</summary>
    public bool Equals(StickyScrollWidgetState? other) =>
        other is not null
        && LastLineRelativePosition == other.LastLineRelativePosition
        && ShowEndForLine == other.ShowEndForLine
        && StartLineNumbers.SequenceEqual(other.StartLineNumbers)
        && EndLineNumbers.SequenceEqual(other.EndLineNumbers);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(LastLineRelativePosition, ShowEndForLine, StartLineNumbers.Count, EndLineNumbers.Count);
}
