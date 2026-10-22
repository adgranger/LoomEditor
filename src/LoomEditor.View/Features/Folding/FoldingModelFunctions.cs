// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.View.Features.Folding;

/// <summary>The free functions of Monaco's <c>foldingModel.ts</c>: collapse by levels and fold jumping.</summary>
/// <remarks>
/// <c>getNextFoldLine</c> stops at the last region instead of reading one region past the end (Monaco reads
/// undefined values there and can return line 0). Threading: UI thread only.
/// </remarks>
internal static class FoldingModelFunctions
{
    /// <summary>
    /// Collapses or expands the regions at the given locations.
    /// <paramref name="levels"/> is 1 to impact only the region at the location, <see cref="int.MaxValue"/> for all levels.
    /// </summary>
    public static void ToggleCollapseState(FoldingModel foldingModel, int levels, IReadOnlyList<int> lineNumbers)
    {
        var toToggle = new List<FoldingRegion>();
        foreach (int lineNumber in lineNumbers)
        {
            if (foldingModel.GetRegionAtLine(lineNumber) is { } region)
            {
                bool doCollapse = !region.IsCollapsed;
                toToggle.Add(region);
                if (levels > 1)
                {
                    toToggle.AddRange(foldingModel.GetRegionsInside(region, (r, level) => r.IsCollapsed != doCollapse && level < levels));
                }
            }
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Collapses or expands the regions at the given locations including all children (all regions when no line is given).</summary>
    public static void SetCollapseStateLevelsDown(FoldingModel foldingModel, bool doCollapse, int levels = int.MaxValue, IReadOnlyList<int>? lineNumbers = null)
    {
        var toToggle = new List<FoldingRegion>();
        if (lineNumbers is { Count: > 0 })
        {
            foreach (int lineNumber in lineNumbers)
            {
                if (foldingModel.GetRegionAtLine(lineNumber) is { } region)
                {
                    if (region.IsCollapsed != doCollapse)
                    {
                        toToggle.Add(region);
                    }

                    if (levels > 1)
                    {
                        toToggle.AddRange(foldingModel.GetRegionsInside(region, (r, level) => r.IsCollapsed != doCollapse && level < levels));
                    }
                }
            }
        }
        else
        {
            toToggle.AddRange(foldingModel.GetRegionsInside(null, (r, level) => r.IsCollapsed != doCollapse && level < levels));
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Collapses or expands the regions at the given locations including all parents.</summary>
    public static void SetCollapseStateLevelsUp(FoldingModel foldingModel, bool doCollapse, int levels, IReadOnlyList<int> lineNumbers)
    {
        var toToggle = new List<FoldingRegion>();
        foreach (int lineNumber in lineNumbers)
        {
            toToggle.AddRange(foldingModel.GetAllRegionsAtLine(lineNumber, (region, level) => region.IsCollapsed != doCollapse && level <= levels));
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Collapses or expands a region at the given locations; if the innermost one is already in that state, its first parent instead.</summary>
    public static void SetCollapseStateUp(FoldingModel foldingModel, bool doCollapse, IReadOnlyList<int> lineNumbers)
    {
        var toToggle = new List<FoldingRegion>();
        foreach (int lineNumber in lineNumbers)
        {
            List<FoldingRegion> regions = foldingModel.GetAllRegionsAtLine(lineNumber, (region, _) => region.IsCollapsed != doCollapse);
            if (regions.Count > 0)
            {
                toToggle.Add(regions[0]);
            }
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Folds or unfolds all regions that have a given level (1 is the top level), except if they contain one of the blocked lines.</summary>
    public static void SetCollapseStateAtLevel(FoldingModel foldingModel, int foldLevel, bool doCollapse, IReadOnlyList<int> blockedLineNumbers)
    {
        List<FoldingRegion> toToggle = foldingModel.GetRegionsInside(null, (region, level) =>
            level == foldLevel && region.IsCollapsed != doCollapse && !blockedLineNumbers.Any(region.ContainsLine));
        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Folds or unfolds all regions, except if they contain or are contained by a region of one of the blocked lines.</summary>
    public static void SetCollapseStateForRest(FoldingModel foldingModel, bool doCollapse, IReadOnlyList<int> blockedLineNumbers)
    {
        var filteredRegions = new List<FoldingRegion>();
        foreach (int lineNumber in blockedLineNumbers)
        {
            List<FoldingRegion> regions = foldingModel.GetAllRegionsAtLine(lineNumber);
            if (regions.Count > 0)
            {
                filteredRegions.Add(regions[0]);
            }
        }

        List<FoldingRegion> toToggle = foldingModel.GetRegionsInside(null, region =>
            filteredRegions.TrueForAll(filtered => !filtered.ContainedBy(region) && !region.ContainedBy(filtered)) && region.IsCollapsed != doCollapse);
        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Folds all regions whose first line matches <paramref name="regExp"/>.</summary>
    public static void SetCollapseStateForMatchingLines(FoldingModel foldingModel, Regex regExp, bool doCollapse)
    {
        FoldingRegions regions = foldingModel.Regions;
        var toToggle = new List<FoldingRegion>();
        for (int i = regions.Length - 1; i >= 0; i--)
        {
            if (doCollapse != regions.IsCollapsed(i) && regExp.IsMatch(foldingModel.TextModel.GetLineContent(regions.GetStartLineNumber(i))))
            {
                toToggle.Add(regions.ToRegion(i));
            }
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>Folds all regions of the given type.</summary>
    public static void SetCollapseStateForType(FoldingModel foldingModel, string type, bool doCollapse)
    {
        FoldingRegions regions = foldingModel.Regions;
        var toToggle = new List<FoldingRegion>();
        for (int i = regions.Length - 1; i >= 0; i--)
        {
            if (doCollapse != regions.IsCollapsed(i) && type == regions.GetType(i))
            {
                toToggle.Add(regions.ToRegion(i));
            }
        }

        foldingModel.ToggleCollapseState(toToggle);
    }

    /// <summary>The line to go to for the parent fold of <paramref name="lineNumber"/>, or <see langword="null"/>.</summary>
    public static int? GetParentFoldLine(int lineNumber, FoldingModel foldingModel)
    {
        int? startLineNumber = null;
        if (foldingModel.GetRegionAtLine(lineNumber) is { } foldingRegion)
        {
            startLineNumber = foldingRegion.StartLineNumber;

            // If the current line is not the start of the current fold, go to the top line of the current fold. If not, go to the parent fold.
            if (lineNumber == startLineNumber)
            {
                int parentFoldingIdx = foldingRegion.ParentIndex;
                startLineNumber = parentFoldingIdx != -1 ? foldingModel.Regions.GetStartLineNumber(parentFoldingIdx) : null;
            }
        }

        return startLineNumber;
    }

    /// <summary>The line to go to for the previous fold at the same level as <paramref name="lineNumber"/>, or <see langword="null"/>.</summary>
    public static int? GetPreviousFoldLine(int lineNumber, FoldingModel foldingModel)
    {
        FoldingRegion? foldingRegion = foldingModel.GetRegionAtLine(lineNumber);
        FoldingRegions regions = foldingModel.Regions;

        // If on the folding range start line, go to the previous sibling.
        if (foldingRegion is not null && foldingRegion.StartLineNumber == lineNumber)
        {
            // Find the min line number to stay within the parent.
            int expectedParentIndex = foldingRegion.ParentIndex;
            int minLineNumber = expectedParentIndex != -1 ? regions.GetStartLineNumber(expectedParentIndex) : 0;

            // Find the fold at the same level.
            while (foldingRegion.RegionIndex > 0)
            {
                foldingRegion = regions.ToRegion(foldingRegion.RegionIndex - 1);

                // Keep at the same level.
                if (foldingRegion.StartLineNumber <= minLineNumber)
                {
                    return null;
                }

                if (foldingRegion.ParentIndex == expectedParentIndex)
                {
                    return foldingRegion.StartLineNumber;
                }
            }

            return null;
        }

        // Go to the last fold that is before the current line.
        for (int i = regions.Length - 1; i >= 0; i--)
        {
            if (regions.GetStartLineNumber(i) < lineNumber)
            {
                return regions.GetStartLineNumber(i);
            }
        }

        return null;
    }

    /// <summary>The line to go to for the next fold at the same level as <paramref name="lineNumber"/>, or <see langword="null"/>.</summary>
    public static int? GetNextFoldLine(int lineNumber, FoldingModel foldingModel)
    {
        FoldingRegion? foldingRegion = foldingModel.GetRegionAtLine(lineNumber);
        FoldingRegions regions = foldingModel.Regions;

        // If on the folding range start line, go to the next sibling.
        if (foldingRegion is not null && foldingRegion.StartLineNumber == lineNumber)
        {
            // Find the max line number to stay within the parent.
            int expectedParentIndex = foldingRegion.ParentIndex;
            int maxLineNumber = expectedParentIndex != -1
                ? regions.GetEndLineNumber(expectedParentIndex)
                : regions.GetEndLineNumber(regions.Length - 1);

            // Find the fold at the same level.
            while (foldingRegion.RegionIndex + 1 < regions.Length)
            {
                foldingRegion = regions.ToRegion(foldingRegion.RegionIndex + 1);

                // Keep at the same level.
                if (foldingRegion.StartLineNumber >= maxLineNumber)
                {
                    return null;
                }

                if (foldingRegion.ParentIndex == expectedParentIndex)
                {
                    return foldingRegion.StartLineNumber;
                }
            }

            return null;
        }

        // Go to the first fold that is after the current line.
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions.GetStartLineNumber(i) > lineNumber)
            {
                return regions.GetStartLineNumber(i);
            }
        }

        return null;
    }
}
