// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.View.DiffEditor;

internal sealed partial class DiffEditorViewModel
{
    // Same owner id as Monaco's plain model.deltaDecorations (0): the decorations have no class name, no editor draws them.
    private const int DiffUnchangedOwnerId = 0;

    private static readonly ModelDecorationOptions UnchangedDecoration =
        ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "unchanged" });

    private List<UnchangedRegion> _regions = [];
    private string[] _originalIds = [];
    private string[] _modifiedIds = [];

    /// <summary>The collapsible regions; empty when <see cref="HideUnchangedRegionsOptions.Enabled"/> is false.</summary>
    public IReadOnlyList<UnchangedRegion> UnchangedRegions => _options.HideUnchangedRegions.Enabled ? _regions : [];

    /// <summary>The list of regions was replaced (or hidden by an option change).</summary>
    public event EventHandler? UnchangedRegionsChanged;

    /// <summary>Monaco: <c>ensureModifiedLineIsVisible</c>.</summary>
    public void EnsureModifiedLineIsVisible(int line, RevealPreference preference)
    {
        if (Diff?.Mappings.Count == 0)
        {
            return;
        }

        foreach (var region in _regions)
        {
            if (region.GetHiddenModifiedRange().Contains(line))
            {
                region.ShowModifiedLine(line, preference);
                return;
            }
        }
    }

    /// <summary>Monaco: <c>ensureOriginalLineIsVisible</c>.</summary>
    public void EnsureOriginalLineIsVisible(int line, RevealPreference preference)
    {
        if (Diff?.Mappings.Count == 0)
        {
            return;
        }

        foreach (var region in _regions)
        {
            if (region.GetHiddenOriginalRange().Contains(line))
            {
                region.ShowOriginalLine(line, preference);
                return;
            }
        }
    }

    // The regions as the decorations (which followed the edits) now see them, one entry per region that still exists.
    private List<UnchangedRegion> RegionsFromDecorations()
    {
        var result = new List<UnchangedRegion>();
        for (var i = 0; i < _regions.Count; i++)
        {
            var origRange = _original.GetDecorationRange(_originalIds[i]);
            var modRange = _modified.GetDecorationRange(_modifiedIds[i]);
            if (origRange is null || modRange is null)
            {
                continue;
            }

            var r = _regions[i];
            var orig = LineRange.FromRangeInclusive(origRange.Value);
            var length = orig.Length;

            // The visible area can shrink by edits.
            var top = Math.Min(r.VisibleLineCountTop, length);
            var bottom = Math.Max(Math.Min(r.VisibleLineCountBottom, length - r.VisibleLineCountTop), 0);
            result.Add(new UnchangedRegion(orig.StartLineNumber, LineRange.FromRangeInclusive(modRange.Value).StartLineNumber, length, top, bottom));
        }

        return result;
    }

    // Monaco: autorun "collapse touching unchanged ranges".
    private void MergeTouchingRegions()
    {
        if (_regions.Count == 0 || _regions.Any(r => r.IsDragged is not null))
        {
            return;
        }

        var updated = RegionsFromDecorations();
        var merged = new List<UnchangedRegion>();
        var didChange = false;
        for (var i = 0; i < updated.Count;)
        {
            var j = i + 1;
            while (j < updated.Count && updated[j - 1].GetHiddenModifiedRange().EndLineNumberExclusive == updated[j].GetHiddenModifiedRange().StartLineNumber)
            {
                j++;
            }

            if (j - i > 1)
            {
                didChange = true;
                var sum = 0;
                for (var k = i; k < j; k++)
                {
                    sum += updated[k].LineCount;
                }

                merged.Add(new UnchangedRegion(
                    updated[i].OriginalLineNumber, updated[i].ModifiedLineNumber, sum, updated[i].VisibleLineCountTop, updated[j - 1].VisibleLineCountBottom));
            }
            else
            {
                merged.Add(updated[i]);
            }

            i = j;
        }

        if (didChange)
        {
            SetRegions(merged);
        }
    }

    // Monaco: updateUnchangedRegions.
    private void UpdateUnchangedRegions(DocumentDiff result)
    {
        var hide = _options.HideUnchangedRegions;
        var originalCount = _original.LineCount;
        var modifiedCount = _modified.LineCount;
        var fresh = UnchangedRegion.FromDiffs(result.Changes, originalCount, modifiedCount, hide.MinimumLineCount, hide.ContextLineCount);

        List<LineRangeMapping>? visible = null;
        if (_regions.Count > 0)
        {
            var previous = new List<UnchangedRegion>();
            foreach (var cur in RegionsFromDecorations())
            {
                // filterWithPrevious: drop regions overlapping their predecessor.
                if (previous.Count == 0
                    || (cur.ModifiedLineNumber >= previous[^1].ModifiedLineNumber + previous[^1].LineCount
                        && cur.OriginalLineNumber >= previous[^1].OriginalLineNumber + previous[^1].LineCount))
                {
                    previous.Add(cur);
                }
            }

            var hidden = LineRangeMapping.Clip(
                previous.Select(r => new LineRangeMapping(r.GetHiddenOriginalRange(), r.GetHiddenModifiedRange())),
                LineRange.OfLength(1, originalCount),
                LineRange.OfLength(1, modifiedCount));
            visible = LineRangeMapping.Inverse(hidden, originalCount, modifiedCount);
        }

        var regions = new List<UnchangedRegion>();
        foreach (var r in fresh)
        {
            if (visible is null)
            {
                regions.Add(r);
                continue;
            }

            var intersecting = visible.Where(f => f.Original.IntersectsStrict(r.OriginalUnchangedRange) && f.Modified.IntersectsStrict(r.ModifiedUnchangedRange)).ToList();
            regions.AddRange(r.SetVisibleRanges(intersecting));
        }

        SetRegions(regions);
    }

    private void SetRegions(List<UnchangedRegion> regions)
    {
        // A region with no line has no decoration (Monaco would throw on its null inclusive range): it is dropped.
        regions.RemoveAll(r => r.LineCount == 0);
        _originalIds = _original.DeltaDecorations(
            DiffUnchangedOwnerId, _originalIds, [.. regions.Select(r => new ModelDeltaDecoration(r.OriginalUnchangedRange.ToInclusiveRange()!.Value, UnchangedDecoration))]);
        _modifiedIds = _modified.DeltaDecorations(
            DiffUnchangedOwnerId, _modifiedIds, [.. regions.Select(r => new ModelDeltaDecoration(r.ModifiedUnchangedRange.ToInclusiveRange()!.Value, UnchangedDecoration))]);
        _regions = regions;
        UnchangedRegionsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveDecorations()
    {
        _original.DeltaDecorations(DiffUnchangedOwnerId, _originalIds, []);
        _modified.DeltaDecorations(DiffUnchangedOwnerId, _modifiedIds, []);
        _originalIds = [];
        _modifiedIds = [];
        _regions = [];
    }
}
