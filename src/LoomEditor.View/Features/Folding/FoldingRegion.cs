// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingRanges.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

/// <summary>A folding region seen through its <see cref="FoldingRegions"/> (Monaco: <c>FoldingRegion</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class FoldingRegion(FoldingRegions ranges, int index)
{
    public int StartLineNumber => ranges.GetStartLineNumber(index);

    public int EndLineNumber => ranges.GetEndLineNumber(index);

    public int RegionIndex => index;

    public int ParentIndex => ranges.GetParentIndex(index);

    public bool IsCollapsed => ranges.IsCollapsed(index);

    public bool ContainedBy(ILineRange range) => range.StartLineNumber <= StartLineNumber && range.EndLineNumber >= EndLineNumber;

    public bool ContainsLine(int lineNumber) => StartLineNumber <= lineNumber && lineNumber <= EndLineNumber;

    public bool HidesLine(int lineNumber) => StartLineNumber < lineNumber && lineNumber <= EndLineNumber;
}

