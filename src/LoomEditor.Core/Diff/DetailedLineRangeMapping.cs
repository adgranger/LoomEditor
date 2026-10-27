// Ported from microsoft/vscode: src/vs/editor/common/diff/rangeMapping.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>A <see cref="LineRangeMapping"/> that also holds the character-level changes inside it (Monaco: <c>DetailedLineRangeMapping</c>).</summary>
/// <remarks>
/// <para>Not ported (needs Monaco's <c>TextEdit</c>): the static <c>toTextEdit</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
internal sealed class DetailedLineRangeMapping(LineRange originalRange, LineRange modifiedRange, IReadOnlyList<RangeMapping>? innerChanges)
    : LineRangeMapping(originalRange, modifiedRange)
{
    /// <summary>
    /// <c>innerChanges</c>: <c>null</c> when the inner changes have not been computed; otherwise the character-level diff in this
    /// line range (never an empty list). Each inner range is contained in the line range, except for new-lines.
    /// </summary>
    public IReadOnlyList<RangeMapping>? InnerChanges { get; } = innerChanges;

    /// <summary><c>fromRangeMappings</c>.</summary>
    public static DetailedLineRangeMapping FromRangeMappings(IReadOnlyList<RangeMapping> rangeMappings)
    {
        LineRange originalRange = LineRange.Join(rangeMappings.Select(r => LineRange.FromRangeInclusive(r.OriginalRange)).ToArray());
        LineRange modifiedRange = LineRange.Join(rangeMappings.Select(r => LineRange.FromRangeInclusive(r.ModifiedRange)).ToArray());
        return new DetailedLineRangeMapping(originalRange, modifiedRange, rangeMappings);
    }

    /// <inheritdoc />
    public override DetailedLineRangeMapping Flip() =>
        new(Modified, Original, InnerChanges?.Select(c => c.Flip()).ToArray());

    /// <summary><c>withInnerChangesFromLineRanges</c>.</summary>
    public DetailedLineRangeMapping WithInnerChangesFromLineRanges() => new(Original, Modified, [ToRangeMapping()]);
}
