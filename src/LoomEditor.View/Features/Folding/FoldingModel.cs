// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Folding;

/// <summary>
/// The folding regions of a text model, with one model decoration per region and their collapse state
/// (Monaco: <c>FoldingModel</c>).
/// </summary>
/// <remarks>
/// Cut point: <c>getMemento</c>, <c>applyMemento</c>, <c>_getLinesChecksum</c> and <c>CollapseMemento</c> (view-state
/// persistence) are not ported. Threading: UI thread only.
/// </remarks>
internal sealed class FoldingModel(TextModel textModel, IDecorationProvider decorationProvider) : IDisposable
{
    private string[] _editorDecorationIds = [];

    /// <summary>Monaco: <c>onDidChange</c>.</summary>
    public event EventHandler<FoldingModelChangeEvent>? Changed;

    public FoldingRegions Regions { get; private set; } = new([], []);

    public TextModel TextModel => textModel;

    public IDecorationProvider DecorationProvider => decorationProvider;

    public void ToggleCollapseState(IReadOnlyList<FoldingRegion> toggledRegions)
    {
        if (toggledRegions.Count == 0)
        {
            return;
        }

        // OrderBy is stable, like Array.prototype.sort.
        List<FoldingRegion> sorted = [.. toggledRegions.OrderBy(r => r.RegionIndex)];
        var processed = new HashSet<string>();
        decorationProvider.ChangeDecorations(accessor =>
        {
            int k = 0; // index from [0 ... Regions.Length]
            int dirtyRegionEndLine = -1; // end of the range where decorations need to be updated
            int lastHiddenLine = -1; // the end of the last hidden lines

            void UpdateDecorationsUntil(int index)
            {
                while (k < index)
                {
                    int endLineNumber = Regions.GetEndLineNumber(k);
                    bool isCollapsed = Regions.IsCollapsed(k);
                    if (endLineNumber <= dirtyRegionEndLine)
                    {
                        bool isManual = Regions.GetSource(k) != FoldSource.Provider;
                        accessor.ChangeDecorationOptions(_editorDecorationIds[k], decorationProvider.GetDecorationOption(isCollapsed, endLineNumber <= lastHiddenLine, isManual));
                    }

                    if (isCollapsed && endLineNumber > lastHiddenLine)
                    {
                        lastHiddenLine = endLineNumber;
                    }

                    k++;
                }
            }

            foreach (FoldingRegion region in sorted)
            {
                int index = region.RegionIndex;
                if (index < _editorDecorationIds.Length && processed.Add(_editorDecorationIds[index]))
                {
                    UpdateDecorationsUntil(index); // update all decorations up to current index using the old dirtyRegionEndLine

                    Regions.SetCollapsed(index, !Regions.IsCollapsed(index));
                    dirtyRegionEndLine = Math.Max(dirtyRegionEndLine, Regions.GetEndLineNumber(index));
                }
            }

            UpdateDecorationsUntil(Regions.Length);
            return 0;
        });
        Changed?.Invoke(this, new FoldingModelChangeEvent(this, sorted));
    }

    public void RemoveManualRanges(IReadOnlyList<Range> ranges)
    {
        var rangeIndexesToRemove = new HashSet<int>();
        bool removeAll = false;
        foreach (Range range in ranges)
        {
            if (range.IsEmpty)
            {
                int index = Regions.FindRange(range.StartLineNumber);
                while (index != -1 && Regions.GetSource(index) == FoldSource.Provider)
                {
                    index = Regions.GetParentIndex(index);
                }

                if (index == -1)
                {
                    removeAll = true;
                }
                else
                {
                    rangeIndexesToRemove.Add(index);
                }
            }
        }

        bool IntersectsSelection(FoldRange foldRange)
        {
            foreach (Range range in ranges)
            {
                if (!range.IsEmpty && !(range.StartLineNumber > foldRange.EndLineNumber || foldRange.StartLineNumber > range.EndLineNumber))
                {
                    return true;
                }
            }

            return false;
        }

        var newFoldingRanges = new List<FoldRange>();
        for (int i = 0; i < Regions.Length; i++)
        {
            FoldRange foldRange = Regions.ToFoldRange(i);
            if (foldRange.Source == FoldSource.Provider || (!removeAll && !rangeIndexesToRemove.Contains(i) && !IntersectsSelection(foldRange)))
            {
                newFoldingRanges.Add(foldRange);
            }
        }

        UpdatePost(FoldingRegions.FromFoldRanges(newFoldingRanges));
    }

    public void Update(FoldingRegions newRegions, ISelectedLines? selection = null)
    {
        List<FoldRange> foldedOrManualRanges = CurrentFoldedOrManualRanges(selection);
        List<FoldRange> newRanges = FoldingRegions.SanitizeAndMerge(newRegions, foldedOrManualRanges, textModel.LineCount, selection);
        UpdatePost(FoldingRegions.FromFoldRanges(newRanges));
    }

    public void UpdatePost(FoldingRegions newRegions)
    {
        var newEditorDecorations = new List<ModelDeltaDecoration>();
        int lastHiddenLine = -1;
        for (int index = 0, limit = newRegions.Length; index < limit; index++)
        {
            int startLineNumber = newRegions.GetStartLineNumber(index);
            int endLineNumber = newRegions.GetEndLineNumber(index);
            bool isCollapsed = newRegions.IsCollapsed(index);
            bool isManual = newRegions.GetSource(index) != FoldSource.Provider;
            var decorationRange = new Range(startLineNumber, textModel.GetLineMaxColumn(startLineNumber), endLineNumber, textModel.GetLineMaxColumn(endLineNumber) + 1);
            newEditorDecorations.Add(new ModelDeltaDecoration(decorationRange, decorationProvider.GetDecorationOption(isCollapsed, endLineNumber <= lastHiddenLine, isManual)));
            if (isCollapsed && endLineNumber > lastHiddenLine)
            {
                lastHiddenLine = endLineNumber;
            }
        }

        _editorDecorationIds = decorationProvider.ChangeDecorations(accessor => accessor.DeltaDecorations(_editorDecorationIds, newEditorDecorations));
        Regions = newRegions;
        Changed?.Invoke(this, new FoldingModelChangeEvent(this));
    }

    private List<FoldRange> CurrentFoldedOrManualRanges(ISelectedLines? selection)
    {
        var foldedRanges = new List<FoldRange>();
        for (int i = 0, limit = Regions.Length; i < limit; i++)
        {
            bool isCollapsed = Regions.IsCollapsed(i);
            FoldSource source = Regions.GetSource(i);
            if (isCollapsed || source != FoldSource.Provider)
            {
                FoldRange foldRange = Regions.ToFoldRange(i);
                if (textModel.GetDecorationRange(_editorDecorationIds[i]) is { } decRange)
                {
                    if (isCollapsed && selection is not null && selection.StartsInside(decRange.StartLineNumber + 1, decRange.EndLineNumber))
                    {
                        isCollapsed = false; // uncollapse is the range is blocked
                    }

                    foldedRanges.Add(new FoldRange
                    {
                        StartLineNumber = decRange.StartLineNumber,
                        EndLineNumber = decRange.EndLineNumber,
                        Type = foldRange.Type,
                        IsCollapsed = isCollapsed,
                        Source = source,
                    });
                }
            }
        }

        return foldedRanges;
    }

    public void Dispose()
    {
        decorationProvider.RemoveDecorations(_editorDecorationIds);
        Changed = null;
    }

    public List<FoldingRegion> GetAllRegionsAtLine(int lineNumber, Func<FoldingRegion, int, bool>? filter = null)
    {
        var result = new List<FoldingRegion>();
        int index = Regions.FindRange(lineNumber);
        int level = 1;
        while (index >= 0)
        {
            FoldingRegion current = Regions.ToRegion(index);
            if (filter is null || filter(current, level))
            {
                result.Add(current);
            }

            level++;
            index = current.ParentIndex;
        }

        return result;
    }

    public FoldingRegion? GetRegionAtLine(int lineNumber)
    {
        int index = Regions.FindRange(lineNumber);
        return index >= 0 ? Regions.ToRegion(index) : null;
    }

    /// <summary>The regions inside <paramref name="region"/> (all regions when <see langword="null"/>), in order.</summary>
    public List<FoldingRegion> GetRegionsInside(FoldingRegion? region, Func<FoldingRegion, bool>? filter = null)
    {
        var result = new List<FoldingRegion>();
        int index = region is null ? 0 : region.RegionIndex + 1;
        int endLineNumber = region?.EndLineNumber ?? int.MaxValue;
        for (int i = index, len = Regions.Length; i < len; i++)
        {
            FoldingRegion current = Regions.ToRegion(i);
            if (Regions.GetStartLineNumber(i) >= endLineNumber)
            {
                break;
            }

            if (filter is null || filter(current))
            {
                result.Add(current);
            }
        }

        return result;
    }

    /// <summary>Like <see cref="GetRegionsInside(FoldingRegion?, Func{FoldingRegion, bool}?)"/>; the filter also gets the nesting level.</summary>
    public List<FoldingRegion> GetRegionsInside(FoldingRegion? region, Func<FoldingRegion, int, bool> filter)
    {
        var result = new List<FoldingRegion>();
        int index = region is null ? 0 : region.RegionIndex + 1;
        int endLineNumber = region?.EndLineNumber ?? int.MaxValue;
        var levelStack = new List<FoldingRegion>();
        for (int i = index, len = Regions.Length; i < len; i++)
        {
            FoldingRegion current = Regions.ToRegion(i);
            if (Regions.GetStartLineNumber(i) >= endLineNumber)
            {
                break;
            }

            while (levelStack.Count > 0 && !current.ContainedBy(levelStack[^1]))
            {
                levelStack.RemoveAt(levelStack.Count - 1);
            }

            levelStack.Add(current);
            if (filter(current, levelStack.Count))
            {
                result.Add(current);
            }
        }

        return result;
    }
}
