// Ported from microsoft/vscode: src/vs/editor/common/core/edits/lengthEdit.ts (LengthReplacement), edits/edit.ts (BaseReplacement)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Text;

/// <summary>
/// Replaces a range of items by <paramref name="NewLength"/> new items; only the lengths are kept (Monaco:
/// <c>LengthReplacement</c>). The generic <c>BaseReplacement</c> is not ported: this is the only replacement used.
/// </summary>
/// <param name="ReplaceRange">The range to be replaced (<c>replaceRange</c>).</param>
/// <param name="NewLength">The number of new items (<c>newLength</c>).</param>
/// <remarks>Threading: immutable. Equality compares the range and the new length (<c>equals</c>).</remarks>
internal sealed record LengthReplacement(OffsetRange ReplaceRange, int NewLength)
{
    /// <summary>Whether the replacement neither removes nor adds items (<c>isEmpty</c>).</summary>
    public bool IsEmpty => NewLength == 0 && ReplaceRange.IsEmpty;

    /// <summary><c>create</c>.</summary>
    public static LengthReplacement Create(int startOffset, int endOffsetExclusive, int newLength) =>
        new(new OffsetRange(startOffset, endOffsetExclusive), newLength);

    /// <summary><c>getLengthDelta</c>: how much the length of the data changes.</summary>
    public int GetLengthDelta() => NewLength - ReplaceRange.Length;

    /// <summary><c>tryJoinTouching</c>: precondition <c>ReplaceRange.EndExclusive == other.ReplaceRange.Start</c>.</summary>
    public LengthReplacement TryJoinTouching(LengthReplacement other) =>
        new(ReplaceRange.JoinRightTouching(other.ReplaceRange), NewLength + other.NewLength);

    /// <summary><c>slice</c> (static: a length replacement has nothing else to copy).</summary>
    public static LengthReplacement Slice(OffsetRange range, OffsetRange rangeInReplacement) => new(range, rangeInReplacement.Length);

    /// <summary><c>delta</c>: the same replacement, moved by <paramref name="offset"/>.</summary>
    public LengthReplacement Delta(int offset) => Slice(ReplaceRange.Delta(offset), new OffsetRange(0, NewLength));

    /// <summary><c>toString</c>.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"[{ReplaceRange.Start}, +{ReplaceRange.Length}) -> +{NewLength}}}");
}
