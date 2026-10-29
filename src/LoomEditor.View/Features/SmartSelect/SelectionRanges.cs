// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/smartSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>The ranges of one cursor and the index of the current one (Monaco: <c>SelectionRanges</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class SelectionRanges(int index, IReadOnlyList<Range> ranges)
{
    public int Index { get; } = index;

    public IReadOnlyList<Range> Ranges { get; } = ranges;

    /// <summary>Moves to the next (or previous) range, skipping the ranges equal to the current one.</summary>
    public SelectionRanges Mov(bool forward)
    {
        int index = Index + (forward ? 1 : -1);
        if (index < 0 || index >= Ranges.Count)
        {
            return this;
        }

        var result = new SelectionRanges(index, Ranges);
        if (result.Ranges[index] == Ranges[Index])
        {
            // next range equals this range, retry with next-next
            return result.Mov(forward);
        }

        return result;
    }
}
