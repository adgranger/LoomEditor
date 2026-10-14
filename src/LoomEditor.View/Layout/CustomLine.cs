// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/lineHeights.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// A line that has a custom height, set by a decoration. Monaco: <c>CustomLine</c>.
/// </summary>
/// <remarks>Threading: not thread-safe; owned and mutated by <see cref="LineHeightsManager"/> on the UI thread.</remarks>
internal sealed class CustomLine
{
    public CustomLine(string decorationId, int index, int lineNumber, int specialHeight, int prefixSum)
    {
        DecorationId = decorationId;
        Index = index;
        LineNumber = lineNumber;
        // Monaco: Math.round. AwayFromZero matches it for the (never negative) heights.
        SpecialHeight = (int)Math.Round((double)specialHeight, MidpointRounding.AwayFromZero);
        PrefixSum = prefixSum;
        MaximumSpecialHeight = SpecialHeight;
        Deleted = false;
    }

    public int Index { get; set; }

    public int LineNumber { get; set; }

    public int SpecialHeight { get; set; }

    public int PrefixSum { get; set; }

    public int MaximumSpecialHeight { get; set; }

    public string DecorationId { get; set; }

    public bool Deleted { get; set; }
}
