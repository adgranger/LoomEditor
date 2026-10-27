// Ported from microsoft/vscode: src/vs/editor/common/diff/rangeMapping.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diagnostics;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

/// <summary>Maps a range in the original text model to a range in the modified text model (Monaco: <c>RangeMapping</c>).</summary>
/// <remarks>
/// <para>Not ported (they need Monaco's <c>TextEdit</c> / <c>TextReplacement</c>): <c>fromEdit</c>, <c>fromEditJoin</c>, <c>toTextEdit</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
internal sealed class RangeMapping(Range originalRange, Range modifiedRange)
{
    /// <summary>The original range.</summary>
    public Range OriginalRange { get; } = originalRange;

    /// <summary>The modified range.</summary>
    public Range ModifiedRange { get; } = modifiedRange;

    /// <summary><c>join</c> (static): joins a non-empty list of mappings into one.</summary>
    /// <exception cref="ArgumentException">The list is empty.</exception>
    public static RangeMapping Join(IReadOnlyList<RangeMapping> rangeMappings)
    {
        if (rangeMappings.Count == 0)
        {
            throw new ArgumentException("Cannot join an empty list of range mappings", nameof(rangeMappings));
        }

        RangeMapping result = rangeMappings[0];
        for (int i = 1; i < rangeMappings.Count; i++)
        {
            result = result.Join(rangeMappings[i]);
        }

        return result;
    }

    /// <summary><c>assertSorted</c>: Debug-only check that the mappings are sorted and do not overlap.</summary>
    public static void AssertSorted(IReadOnlyList<RangeMapping> rangeMappings)
    {
        for (int i = 1; i < rangeMappings.Count; i++)
        {
            RangeMapping previous = rangeMappings[i - 1];
            RangeMapping current = rangeMappings[i];
            Guard.Assert(
                previous.OriginalRange.End.IsBeforeOrEqual(current.OriginalRange.Start)
                && previous.ModifiedRange.End.IsBeforeOrEqual(current.ModifiedRange.Start),
                "Range mappings must be sorted");
        }
    }

    /// <summary><c>flip</c>.</summary>
    public RangeMapping Flip() => new(ModifiedRange, OriginalRange);

    /// <summary><c>join</c>: the smallest mapping that contains both.</summary>
    public RangeMapping Join(RangeMapping other) => new(OriginalRange.Plus(other.OriginalRange), ModifiedRange.Plus(other.ModifiedRange));

    /// <inheritdoc />
    public override string ToString() => $"{{{OriginalRange}->{ModifiedRange}}}";
}
