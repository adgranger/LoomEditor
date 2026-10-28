// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>A pair of offsets, one in each sequence (Monaco: <c>OffsetPair</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class OffsetPair(int offset1, int offset2) : IEquatable<OffsetPair>
{
    /// <summary><c>zero</c>.</summary>
    public static readonly OffsetPair Zero = new(0, 0);

    /// <summary><c>max</c> (Monaco: <c>Number.MAX_SAFE_INTEGER</c>, here <see cref="int.MaxValue"/>).</summary>
    public static readonly OffsetPair Max = new(int.MaxValue, int.MaxValue);

    /// <summary>The offset in the first sequence.</summary>
    public int Offset1 { get; } = offset1;

    /// <summary>The offset in the second sequence.</summary>
    public int Offset2 { get; } = offset2;

    /// <summary><c>delta</c>.</summary>
    public OffsetPair Delta(int offset) => offset == 0 ? this : new(Offset1 + offset, Offset2 + offset);

    /// <inheritdoc />
    public bool Equals(OffsetPair? other) => other is not null && Offset1 == other.Offset1 && Offset2 == other.Offset2;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as OffsetPair);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Offset1, Offset2);

    /// <inheritdoc />
    public override string ToString() => $"{Offset1} <-> {Offset2}";
}
